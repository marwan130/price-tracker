from __future__ import annotations

import asyncio
import json
import logging
import os
import random
import time
from datetime import UTC, datetime, timedelta
from uuid import UUID, uuid4

import aio_pika
from aio_pika import DeliveryMode, ExchangeType, Message
from aio_pika.abc import AbstractIncomingMessage

from price_tracker_scraper.extraction import as_payload, extract_product, price_in_range

logging.basicConfig(level=os.getenv("LOG_LEVEL", "INFO"))
logger = logging.getLogger("price_tracker_scraper")

WORK_EXCHANGE = "price-tracker.work.v1"
RESULT_EXCHANGE = "price-tracker.results.v1"
QUEUE = "scrape.jobs.v1"
ROUTING_KEY = "scrape.job.requested.v1"
RETRY_EXCHANGE = "price-tracker.retry.v1"
RETRY_DELAYS_MS = (5_000, 30_000, 120_000)
RETRY_JITTER_MS = tuple(delay // 5 for delay in RETRY_DELAYS_MS)


def _now() -> str:
    return datetime.now(UTC).isoformat()


def _failure_code(exc: Exception) -> str:
    name = type(exc).__name__.casefold()
    if str(exc) in {"unsupported_url", "non_public_host"}:
        return "invalid_or_non_public_url"
    if str(exc) == "price_out_of_range":
        return "price_out_of_range"
    if "timeout" in name:
        return "fetch_timeout"
    if "http_status:" in str(exc):
        return str(exc)
    return "fetch_or_extract_failed"


def _is_retryable_failure(failure_code: str) -> bool:
    if failure_code in {"invalid_or_non_public_url", "http_status:404", "price_out_of_range"}:
        return False
    if failure_code.startswith("http_status:"):
        try:
            status = int(failure_code.partition(":")[2])
        except ValueError:
            return False
        return status in {408, 425, 429} or status >= 500
    return True


def _header_retry_count(headers: object) -> int:
    """Read `x-retry-count` from an AMQP message's headers.

    AMQP header values type as `FieldValue` (a bare union that includes
    non-numeric shapes like datetime), so coerce defensively: numeric
    headers pass through, anything else round-trips through str(), and a
    malformed value counts as zero retries (never crashes a redelivery).
    """
    if not isinstance(headers, dict):
        return 0
    raw = headers.get("x-retry-count", 0)
    if isinstance(raw, int) and not isinstance(raw, bool):
        return max(raw, 0)
    try:
        return int(str(raw))
    except ValueError:
        return 0


async def _handle(message: AbstractIncomingMessage, exchange, retry_exchange, dead_exchange) -> None:
    try:
        envelope = json.loads(message.body)
        if envelope.get("schemaVersion") != 1:
            raise ValueError("unsupported_schema_version")
        payload = envelope["payload"]
        job_id = UUID(str(payload["jobId"]))
        worker_started = datetime.now(UTC)
        started_monotonic = time.monotonic()
        url = payload["url"]
        if not isinstance(url, str) or not url.startswith("https://"):
            raise ValueError("url_must_use_https")
        correlation_id = str(envelope.get("correlationId") or uuid4())
        try:
            extraction = await extract_product(url)
            if not price_in_range(extraction.price, payload.get("minPrice"), payload.get("maxPrice")):
                raise ValueError("price_out_of_range")
            result = {"jobId": str(job_id), "listingId": payload.get("listingId"), "searchSessionId": payload.get("searchSessionId"), "outcome": "completed", "extraction": as_payload(extraction)}
            routing_key = "scrape.result.completed.v1"
        except Exception as exc:  # noqa: BLE001 - any failure must become a structured failure result (classified below), never crash the worker loop
            failure_code = _failure_code(exc)
            retryable = _is_retryable_failure(failure_code)
            retry_count = _header_retry_count(message.headers)
            if retryable and retry_count < len(RETRY_DELAYS_MS):
                delay_ms = RETRY_DELAYS_MS[retry_count]
                headers = dict(message.headers or {})
                headers["x-retry-count"] = retry_count + 1
                await retry_exchange.publish(
                    Message(
                        message.body,
                        headers=headers,
                        delivery_mode=DeliveryMode.PERSISTENT,
                        content_type=message.content_type or "application/json",
                        correlation_id=message.correlation_id,
                        message_id=message.message_id,
                        expiration=timedelta(milliseconds=delay_ms + random.randint(0, RETRY_JITTER_MS[retry_count])),
                    ),
                    f"scrape.retry.{delay_ms}ms.v1",
                    mandatory=True,
                )
                await message.ack()
                logger.info("Retrying scrape job %s after %sms (%s)", job_id, delay_ms, failure_code)
                return

            logger.warning("Scrape job %s failed (%s)", job_id, failure_code)
            result = {
                "jobId": str(job_id),
                "listingId": payload.get("listingId"),
                "searchSessionId": payload.get("searchSessionId"),
                "outcome": "failed",
                "failure": {"code": failure_code, "retryable": retryable},
            }
            routing_key = "scrape.result.failed.v1"

        result["workerStartedAtUtc"] = worker_started.isoformat()
        result["durationMs"] = max(0, int((time.monotonic() - started_monotonic) * 1000))

        response = {
            "schemaVersion": 1,
            "messageId": str(uuid4()),
            "correlationId": correlation_id,
            "causationId": str(envelope.get("messageId") or ""),
            "occurredAtUtc": _now(),
            "payload": result,
        }
        await exchange.publish(
            Message(
                json.dumps(response, separators=(",", ":")).encode(),
                delivery_mode=DeliveryMode.PERSISTENT,
                content_type="application/json",
                correlation_id=correlation_id,
                message_id=response["messageId"],
            ),
            routing_key,
            mandatory=True,
        )
        await message.ack()
    except (ValueError, KeyError, TypeError, json.JSONDecodeError) as exc:
        logger.error("Rejecting malformed scrape message: %s", exc)
        await message.reject(requeue=False)
    except Exception:
        logger.exception("Scrape message processing failed")
        retry_count = _header_retry_count(message.headers)
        if retry_count >= len(RETRY_DELAYS_MS):
            destination = dead_exchange
            route = "scrape.job.processing-failed.v1"
        else:
            destination = retry_exchange
            route = f"scrape.retry.{RETRY_DELAYS_MS[retry_count]}ms.v1"
        headers = dict(message.headers or {})
        headers["x-retry-count"] = retry_count + 1
        await destination.publish(
            Message(
                message.body,
                headers=headers,
                delivery_mode=DeliveryMode.PERSISTENT,
                content_type=message.content_type or "application/json",
                correlation_id=message.correlation_id,
                message_id=message.message_id,
            ),
            route,
            mandatory=True,
        )
        await message.ack()


async def run() -> None:
    host = os.getenv("RABBITMQ_HOST", "rabbitmq")
    port = int(os.getenv("RABBITMQ_PORT", "5672"))
    user = os.environ["RABBITMQ_USER"]
    password = os.environ["RABBITMQ_PASSWORD"]
    connection = await aio_pika.connect_robust(
        host=host,
        port=port,
        login=user,
        password=password,
        client_properties={"connection_name": "price-tracker-scrapling-worker"},
    )
    async with connection:
        channel = await connection.channel(publisher_confirms=True)
        await channel.set_qos(prefetch_count=2)
        work_exchange = await channel.declare_exchange(WORK_EXCHANGE, ExchangeType.TOPIC, durable=True)
        result_exchange = await channel.declare_exchange(RESULT_EXCHANGE, ExchangeType.TOPIC, durable=True)
        retry_exchange = await channel.declare_exchange(RETRY_EXCHANGE, ExchangeType.DIRECT, durable=True)
        dead_exchange = await channel.declare_exchange("price-tracker.dead.v1", ExchangeType.TOPIC, durable=True)
        dead_queue = await channel.declare_queue("scrape.jobs.dead.v1", durable=True, arguments={"x-max-length": 10_000, "x-overflow": "reject-publish"})
        await dead_queue.bind(dead_exchange, "scrape.job.requested.v1")
        await dead_queue.bind(dead_exchange, "scrape.job.processing-failed.v1")
        for delay_ms, jitter_ms in zip(RETRY_DELAYS_MS, RETRY_JITTER_MS, strict=True):
            retry_route = f"scrape.retry.{delay_ms}ms.v1"
            retry_queue = await channel.declare_queue(
                f"scrape.jobs.retry.{delay_ms}ms.v1",
                durable=True,
                arguments={
                    "x-message-ttl": delay_ms + jitter_ms,
                    "x-dead-letter-exchange": WORK_EXCHANGE,
                    "x-dead-letter-routing-key": ROUTING_KEY,
                    "x-max-length": 10_000,
                    "x-overflow": "reject-publish",
                },
            )
            await retry_queue.bind(retry_exchange, retry_route)
        queue = await channel.declare_queue(
            QUEUE,
            durable=True,
            arguments={"x-dead-letter-exchange": "price-tracker.dead.v1", "x-dead-letter-routing-key": "scrape.job.requested.v1", "x-max-length": 10_000, "x-overflow": "reject-publish"},
        )
        await queue.bind(work_exchange, ROUTING_KEY)
        logger.info("Scrapling consumer ready on %s", QUEUE)
        await queue.consume(
            lambda message: _handle(message, result_exchange, retry_exchange, dead_exchange), no_ack=False
        )
        await asyncio.Future()


if __name__ == "__main__":
    asyncio.run(run())
