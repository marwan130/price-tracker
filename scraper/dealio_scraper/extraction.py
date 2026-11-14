from __future__ import annotations

import asyncio
import ipaddress
import json
import re
import socket
from dataclasses import dataclass, replace
from decimal import Decimal, InvalidOperation
from typing import Any
from urllib.parse import urljoin, urlsplit, urlunsplit

from scrapling.fetchers import AsyncFetcher


@dataclass(frozen=True)
class Extraction:
    title: str | None
    price: str | None
    currency: str | None
    image_url: str | None
    availability: str | None
    extraction_method: str | None


async def _fetch_page(url: str, timeout_seconds: int):
    return await AsyncFetcher.get(
        url,
        timeout=timeout_seconds,
        retries=1,
        follow_redirects="safe",
        stealthy_headers=True,
    )


def _validate_url(url: str):
    parsed = urlsplit(url)
    if parsed.scheme != "https" or not parsed.hostname or parsed.username or parsed.password:
        raise ValueError("unsupported_url")
    try:
        literal_ip = ipaddress.ip_address(parsed.hostname)
    except ValueError:
        literal_ip = None
    if literal_ip is not None and not literal_ip.is_global:
        raise ValueError("non_public_host")
    return parsed, literal_ip


_STORE_SELECTORS: dict[str, tuple[tuple[str, ...], tuple[str, ...]]] = {
    "jumia": (
        ('[data-testid="price"]::text', '.-fs24::text'),
        ('h1::text', 'meta[property="og:title"]::attr(content)'),
    ),
    "noon": (
        ('[data-qa="product-price"]::text', '[data-testid="product-price"]::text', '.priceNow::text'),
        ('h1::text', 'meta[property="og:title"]::attr(content)'),
    ),
    "amazon": (
        (
            '#corePrice_feature_div .a-offscreen::text',
            '#price_inside_buybox::text',
            '#corePriceDisplay_desktop_feature_div .a-price .a-offscreen::text',
            '#corePriceDisplay_mobile_feature_div .a-price .a-offscreen::text',
            '#apex_desktop .a-price .a-offscreen::text',
            '.a-price .a-offscreen::text',
        ),
        ('#productTitle::text', 'meta[property="og:title"]::attr(content)'),
    ),
    "namshi": (
        ('[data-testid="price"]::text', '.product-price::text'),
        ('h1::text', 'meta[property="og:title"]::attr(content)'),
    ),
    "2b": (
        ('.price-including-tax .price::text', '.price-with-relation .price::text'),
        ('.page-title .base::text', 'h1::text'),
    ),
    "ubuy": (
        ('.product-price::text', '[class*="product-price"]::text'),
        ('h1[class*="product"]::text',),
    ),
}

_STORE_HOSTS = {
    "jumia": ("jumia.com.eg", "jumia.com.sa"),
    "noon": ("noon.com",),
    "amazon": ("amazon.eg", "amazon.sa", "amazon.ae", "amazon.com"),
    "namshi": ("namshi.com",),
    "2b": ("2b.com.eg",),
    "ubuy": ("ubuy.com", "ubuy.ae", "ubuy.com.eg", "ubuy.com.sa", "ubuy.com.kw"),
}


def _store_for_host(host: str | None) -> str | None:
    normalized = (host or "").casefold().removeprefix("www.")
    return next((store for store, domains in _STORE_HOSTS.items() if any(normalized == domain or normalized.endswith(f".{domain}") for domain in domains)), None)


def _first_selector_text(page, selectors: tuple[str, ...]) -> str | None:
    for selector in selectors:
        try:
            values = page.css(selector).getall()
        except (AttributeError, TypeError, ValueError):
            continue
        value = next((item for item in values if isinstance(item, str) and item.strip()), None)
        if value:
            return value.strip()
    return None


def _join_selector_text(page, selectors: tuple[str, ...]) -> str | None:
    for selector in selectors:
        try:
            values = page.css(selector).getall()
        except (AttributeError, TypeError, ValueError):
            continue
        joined = " ".join(
            item.strip() for item in values if isinstance(item, str) and item.strip()
        )
        if joined:
            return joined
    return None


_STORE_AVAILABILITY_TEXT_SELECTORS: dict[str, tuple[str, ...]] = {
    "amazon": ('#availability ::text', '#outOfStock ::text', '#buybox-error-message ::text'),
}


def _extract_store_availability(page, store: str | None, price: str | None) -> str | None:
    raw = _first_selector_text(page, (
        'meta[property="product:availability"]::attr(content)',
        'meta[property="og:availability"]::attr(content)',
        '[itemprop="availability"]::attr(content)',
        '[itemprop="availability"]::attr(href)',
    ))
    if raw:
        return _normalize_availability(raw)
    if price is not None:
        return None
    return _normalize_availability(_join_selector_text(
        page, _STORE_AVAILABILITY_TEXT_SELECTORS.get(store or "", ())
    ))

_AVAILABILITY_ALIASES = {
    "instock": "InStock",
    "available": "InStock",
    "limitedavailability": "LimitedAvailability",
    "preorder": "PreOrder",
    "outofstock": "OutOfStock",
    "soldout": "OutOfStock",
    "oos": "OutOfStock",
    "unavailable": "OutOfStock",
    "currentlyunavailable": "OutOfStock",
    "notavailable": "OutOfStock",
    "notinstock": "OutOfStock",
    "discontinued": "OutOfStock",
}

_OUT_OF_STOCK_TEXT_MARKERS = ("out of stock", "currently unavailable", "sold out", "نفدت الكمية", "غير متوفر")
_IN_STOCK_TEXT_MARKERS = ("in stock", "متوفر")


def _normalize_availability(raw: str | None) -> str | None:
    if not raw or not raw.strip():
        return None
    value = raw.strip()
    if "://" in value:
        value = value.rsplit("/", 1)[-1].strip()
    keyword = re.sub(r"[^a-z]", "", value.casefold())
    if keyword in _AVAILABILITY_ALIASES:
        return _AVAILABILITY_ALIASES[keyword]
    lowered = value.casefold()
    if any(marker in lowered for marker in _OUT_OF_STOCK_TEXT_MARKERS):
        return "OutOfStock"
    if any(marker in lowered for marker in _IN_STOCK_TEXT_MARKERS):
        return "InStock"
    return None


def _extract_store_page(page, url: str | None) -> Extraction:
    store = _store_for_host(urlsplit(url).hostname if url else None)
    if store is None:
        price_selectors: tuple[str, ...] = ()
        title_selectors: tuple[str, ...] = ()
    else:
        price_selectors, title_selectors = _STORE_SELECTORS[store]
    raw_price = _first_selector_text(page, (
        'meta[property="product:price:amount"]::attr(content)',
        'meta[itemprop="price"]::attr(content)',
        '[itemprop="price"]::attr(content)',
        *price_selectors,
    ))
    title = _first_selector_text(page, (
        'meta[property="og:title"]::attr(content)',
        'meta[name="twitter:title"]::attr(content)',
        *title_selectors,
    ))
    raw_currency = _first_selector_text(page, (
        'meta[property="product:price:currency"]::attr(content)',
        'meta[itemprop="priceCurrency"]::attr(content)',
        '[itemprop="priceCurrency"]::attr(content)',
    ))
    if not raw_currency:
        currency_match = re.search(r"(?<![A-Z])(?:EGP|SAR|AED|BHD|JOD|KWD|OMR|QAR|TRY|USD|EUR|GBP)(?![A-Z])", raw_price or "", re.IGNORECASE)
        raw_currency = currency_match.group(0) if currency_match else None
    # Remove localized currency text before parsing the amount.
    price_text = re.sub(r"(?<![A-Z])(?:EGP|SAR|AED|BHD|JOD|KWD|OMR|QAR|TRY|USD|EUR|GBP)(?![A-Z])", "", raw_price or "", flags=re.IGNORECASE)
    number = re.search(r"(?<![\w])\d[\d,.]*(?:[.,]\d{1,2})?", price_text)
    price = _valid_price(number.group(0)) if number else None
    currency = raw_currency.strip().upper() if raw_currency and len(raw_currency.strip()) == 3 and raw_currency.strip().isalpha() else None
    availability = _extract_store_availability(page, store, price)
    return Extraction(
        title,
        price,
        currency,
        None,
        availability,
        f"{store or 'generic'}-selectors" if any((title, price, currency, availability)) else None,
    )


def _extract_page(page, url: str | None = None) -> Extraction:
    if page.status < 200 or page.status >= 300:
        raise RuntimeError(f"http_status:{page.status}")

    products = _json_ld_products(page)
    if not products:
        extraction = _extract_store_page(page, url)
        if extraction.image_url is None:
            image_url = _extract_page_image(page, url)
            if image_url:
                extraction = replace(extraction, image_url=image_url)
        return extraction

    extractions = [
        _extract_product(product, offer, url)
        for product in products
        for offer in _offers(product)
    ]
    extraction = max(extractions, key=_extraction_completeness)
    store_extraction = _extract_store_page(page, url)
    store_availability = (
        store_extraction.availability
        if extraction.availability is None and extraction.price is None
        else None
    )
    used_store_fallback = any((
        extraction.price is None and store_extraction.price is not None,
        extraction.currency is None and store_extraction.currency is not None,
        extraction.title is None and store_extraction.title is not None,
        store_availability is not None,
        extraction.image_url is None and store_extraction.image_url is not None,
    ))
    extraction = replace(
        extraction,
        title=extraction.title or store_extraction.title,
        price=extraction.price or store_extraction.price,
        currency=extraction.currency or store_extraction.currency,
        availability=extraction.availability or store_availability,
        image_url=extraction.image_url or store_extraction.image_url,
        extraction_method=(
            f"{extraction.extraction_method}+{store_extraction.extraction_method}"
            if used_store_fallback and extraction.extraction_method and store_extraction.extraction_method
            else extraction.extraction_method or store_extraction.extraction_method
        ),
    )
    if extraction.image_url is None:
        image_url = _extract_page_image(page, url)
        if image_url:
            extraction = replace(extraction, image_url=image_url)
    return extraction


def _extract_page_image(page, page_url: str | None) -> str | None:
    raw_image = _first_selector_text(page, (
        'meta[property="og:image"]::attr(content)',
        'meta[property="og:image:secure_url"]::attr(content)',
        'meta[name="twitter:image"]::attr(content)',
        'link[rel="image_src"]::attr(href)',
        '[itemprop="image"]::attr(content)',
        '[itemprop="image"]::attr(src)',
        '#landingImage::attr(data-old-hires)',
        '#landingImage::attr(data-a-dynamic-image)',
        '#landingImage::attr(src)',
        '#imgBlkFront::attr(src)',
        'img[data-testid*="product"]::attr(src)',
    ))
    if not raw_image:
        return None
    if raw_image.lstrip().startswith("{"):
        raw_image = _largest_dynamic_image(raw_image)
        if not raw_image:
            return None
    image_url = urljoin(page_url or "", raw_image)
    parsed = urlsplit(image_url)
    if parsed.scheme == "http" and parsed.hostname:
        image_url = urlunsplit(("https", parsed.netloc, parsed.path, parsed.query, parsed.fragment))
        parsed = urlsplit(image_url)
    if parsed.scheme != "https" or not parsed.hostname or _is_junk_image(image_url):
        return None
    return image_url


def _largest_dynamic_image(raw: str | None) -> str | None:
    if not raw:
        return None
    try:
        entries = json.loads(raw)
    except (json.JSONDecodeError, TypeError):
        return None
    if not isinstance(entries, dict):
        return None
    best_url, best_area = None, -1
    for candidate_url, size in entries.items():
        if not isinstance(candidate_url, str) or not candidate_url.startswith("https://"):
            continue
        area = 0
        if isinstance(size, list) and len(size) == 2 and all(isinstance(n, (int, float)) for n in size):
            area = size[0] * size[1]
        if area > best_area:
            best_url, best_area = candidate_url, area
    return best_url


def _amazon_image_score(url: str) -> int:
    lowered = url.lower()
    if "/images/i/" not in lowered:
        return 0
    if "_sy300_sx300_" in lowered or "_sx300_sy300_" in lowered:
        return 3
    if "_us40" in lowered or "_us50" in lowered:
        return 1
    return 2


def _best_product_image(value: Any) -> str | None:
    if isinstance(value, str) and value.strip():
        return value.strip()
    if isinstance(value, list):
        candidates = [item.strip() for item in value if isinstance(item, str) and item.strip()]
        if not candidates:
            return None
        return max(candidates, key=_amazon_image_score)
    return None


_JUNK_IMAGE_MARKERS = (
    "fls-", "uedata", "nav-sprite", "/gno/", "sprite", "opengraph/common",
    "beacon", "clear.gif", "blank.gif", "transparent.gif", "1x1",
    "data:image", "logo", "placeholder", "favicon", "sponsored-ads", "icons/",
    "banner", "social", "share", "facebook", "instagram", "whatsapp",
    "payment-icons", "rating-stars", "google-",
    "payment", "paypal", "visa", "mastercard", "mada-card", "amex", "unionpay",
    "tabby", "tamara", "valu-", "fawry", "instapay", "paymob", "weaccept",
    "trustpilot", "star-rating", "wishlist", "app-store", "google-play",
    "cookie", "newsletter", "breadcrumb", "checkout-icon",
    "/images/G/",
    "sash", "avatars", "personalization", "aplus-media", "grey-pixel",
    "transparent-pixel", "loading-4x", "iconfarm", ".svg", "homepage",
)


def _is_junk_image(image_url: str) -> bool:
    lowered = image_url.lower()
    return any(marker in lowered for marker in _JUNK_IMAGE_MARKERS)


def _extract_product(product: dict[str, Any], offer: dict[str, Any], page_url: str | None = None) -> Extraction:
    raw_image = product.get("image")
    image_url = _best_product_image(raw_image)
    if image_url and page_url:
        image_url = urljoin(page_url, image_url)
    if image_url and _is_junk_image(image_url):
        image_url = None
    price = _valid_price(offer.get("price", offer.get("lowPrice")))
    raw_currency = offer.get("priceCurrency")
    currency = (
        raw_currency.strip().upper()
        if isinstance(raw_currency, str)
        and len(raw_currency.strip()) == 3
        and raw_currency.strip().isalpha()
        else None
    )

    availability = _normalize_availability(offer.get("availability") if isinstance(offer.get("availability"), str) else None)

    title = _first_text(product.get("name"))
    method = "json-ld" if any((title, price, currency, image_url, availability)) else None
    return Extraction(title, price, currency, image_url, availability, method)


def _extraction_completeness(extraction: Extraction) -> tuple[int, ...]:
    return (
        int(extraction.price is not None),
        int(extraction.currency is not None),
        int(extraction.title is not None),
        int(extraction.availability is not None),
        int(extraction.image_url is not None),
    )


def _products(value: Any):
    if isinstance(value, list):
        for item in value:
            yield from _products(item)
    elif isinstance(value, dict):
        kind = value.get("@type")
        kinds = kind if isinstance(kind, list) else [kind]
        if any(isinstance(k, str) and k.casefold() in ("product", "productgroup") for k in kinds):
            yield value
            variants = value.get("hasVariant")
            if isinstance(variants, (list, dict)):
                yield from _products(variants)
        graph = value.get("@graph")
        if graph is not None:
            yield from _products(graph)


def _json_ld_products(page) -> list[dict[str, Any]]:
    found: list[dict[str, Any]] = []
    for raw in page.css('script[type="application/ld+json"]::text').getall():
        try:
            data = json.loads(raw)
        except (json.JSONDecodeError, TypeError):
            continue
        found.extend(_products(data))
    return found


def _first_text(value: Any) -> str | None:
    if isinstance(value, str) and value.strip():
        return value.strip()
    if isinstance(value, list):
        return next((item.strip() for item in value if isinstance(item, str) and item.strip()), None)
    if isinstance(value, dict):
        return _first_text(value.get("url")) or _first_text(value.get("contentUrl"))
    return None


def _offers(product: dict[str, Any]) -> list[dict[str, Any]]:
    offers = product.get("offers")
    if isinstance(offers, list):
        return [offer for offer in offers if isinstance(offer, dict)] or [{}]
    return [offers] if isinstance(offers, dict) else [{}]


def _valid_price(value: Any) -> str | None:
    if value is None or isinstance(value, bool):
        return None
    try:
        amount = Decimal(str(value).replace(",", "").strip())
    except (InvalidOperation, ValueError):
        return None
    if not amount.is_finite() or amount <= 0:
        return None
    return format(amount, "f")


async def extract_product(url: str, timeout_seconds: int = 25) -> Extraction:
    """Fetch one targeted URL and extract explicit Product JSON-LD fields.

    The worker reports extraction facts only. The API remains responsible for
    market, currency, product identity and price validation decisions.
    """
    parsed, literal_ip = _validate_url(url)
    if literal_ip is None:
        addresses = await asyncio.get_running_loop().getaddrinfo(
            parsed.hostname, parsed.port or 443, type=socket.SOCK_STREAM
        )
        if not addresses or any(not ipaddress.ip_address(item[4][0]).is_global for item in addresses):
            raise ValueError("non_public_host")

    page = await _fetch_page(url, timeout_seconds)
    return _extract_page(page, url)


def as_payload(extraction: Extraction) -> dict[str, Any]:
    return {
        "title": extraction.title,
        "price": extraction.price,
        "currency": extraction.currency,
        "imageUrl": extraction.image_url,
        "availability": extraction.availability,
        "extractionMethod": extraction.extraction_method,
    }


def price_in_range(price: str | None, min_price: Any = None, max_price: Any = None) -> bool:
    if min_price is None and max_price is None:
        return True
    if price is None:
        return False
    try:
        value = Decimal(str(price))
        lower = None if min_price is None else Decimal(str(min_price))
        upper = None if max_price is None else Decimal(str(max_price))
    except (InvalidOperation, ValueError, TypeError):
        return False
    return (lower is None or value >= lower) and (upper is None or value <= upper)
