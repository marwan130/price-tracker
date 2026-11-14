// @ts-nocheck
import { DisplayCurrencyContext, useLocale } from "../../lib/i18n-context";
import { EmptyState } from "../../components/ui/EmptyState";
import { Icon } from "../../components/ui/Icon";
import { LoadingIndicator } from "../../components/ui/LoadingIndicator";
import { MetricStat } from "../../components/ui/MetricStat";
import { Price } from "../../components/ui/Price";
import { ProductImage } from "../../components/ui/ProductImage";
import { RecentDropsSection } from "../discover/RecentDropsSection";
import { SkeletonTable } from "../../components/ui/skeletons";
import { TrackingsTable } from "../products/TrackingsTable";
import { apiFetch } from "../../lib/api";
import { formatDisplayValue, formatShortDateOrDash } from "../../lib/format";
import { useCallback, useContext, useEffect, useRef, useState, Fragment } from "react";

const FEED_PAGE_SIZE = 8;
const SEED_KEY = "dealio-feed-seed";
const PAGES_KEY = "dealio-feed-pages";
const SEEN_KEY = "dealio-feed-seen";
const ITEMS_KEY = "dealio-feed-items";
const DONE_KEY = "dealio-feed-done";

function sessionSeed() {
  let seed = null;
  try { seed = sessionStorage.getItem(SEED_KEY); } catch { seed = null; }
  if (!seed) {
    seed = typeof crypto !== "undefined" && crypto.randomUUID
      ? crypto.randomUUID()
      : `s-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
    try { sessionStorage.setItem(SEED_KEY, seed); } catch { /* storage can be unavailable */ }
  }
  return seed;
}

function newSessionSeed() {
  const seed = typeof crypto !== "undefined" && crypto.randomUUID
    ? crypto.randomUUID()
    : `s-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
  try { sessionStorage.setItem(SEED_KEY, seed); } catch { /* storage can be unavailable */ }
  return seed;
}

function readSeen() {
  try {
    const raw = sessionStorage.getItem(SEEN_KEY);
    const list = raw ? JSON.parse(raw) : [];
    return Array.isArray(list) ? list.filter((id) => typeof id === "string") : [];
  } catch {
    return [];
  }
}

function writeSeen(ids) {
  try {
    sessionStorage.setItem(SEEN_KEY, JSON.stringify(ids.slice(-500)));
  } catch { /* ignore */ }
}

function readFeedItems() {
  try {
    const rows = JSON.parse(sessionStorage.getItem(ITEMS_KEY) || "[]");
    return Array.isArray(rows) ? rows.filter((item) => item?.productId).slice(-500) : [];
  } catch {
    return [];
  }
}

function writeFeedItems(items) {
  try { sessionStorage.setItem(ITEMS_KEY, JSON.stringify(items.slice(-500))); } catch { /* storage can be unavailable */ }
}

function readFeedPage() {
  try { return Math.max(0, Number(sessionStorage.getItem(PAGES_KEY)) || 0); } catch { return 0; }
}

function readFeedDone() {
  try { return sessionStorage.getItem(DONE_KEY) === "1"; } catch { return false; }
}

// A session-stable random shuffle of the catalog. Same seed -> the same
// product order on every page fetch, so the feed never reshuffles while
// browsing and successive pages are duplicate-free slices of one ordering.
function fetchFeedPage({ seed, page, signal }) {
  const params = new URLSearchParams({
    sortBy: "random",
    randomSeed: seed,
    Page: String(page),
    Size: String(FEED_PAGE_SIZE),
  });
  return apiFetch(`/v1/products?${params}`, signal ? { signal } : undefined);
}

export function DashboardPage({ token, openProduct, onDiscover, onSignIn, notify }) {
  const v = useLocale();
  const displayCurrency = useContext(DisplayCurrencyContext);

  const [alerts, setAlerts] = useState([]);
  const [unread, setUnread] = useState(0);
  const [trackBusy, setTrackBusy] = useState(false);
  const [trackError, setTrackError] = useState("");
  const [drops, setDrops] = useState([]);

  // --- discovery feed state -------------------------------------------------
  const [feedItems, setFeedItems] = useState(readFeedItems);
  const [feedState, setFeedState] = useState(() => readFeedItems().length ? readFeedDone() ? "done" : "ready" : "loading");
  const [feedError, setFeedError] = useState("");
  const feedPageRef = useRef(readFeedPage());
  const seedRef = useRef(sessionSeed());
  const feedBusyRef = useRef(false);
  const feedControllerRef = useRef(null);
  const sentinelRef = useRef(null);
  const seenRef = useRef(new Set([...readSeen(), ...readFeedItems().map((item) => item.productId)]));
  const feedDoneRef = useRef(readFeedDone());

  useEffect(() => {
    let alive = true;
    apiFetch("/v1/price-history/drops/recent?size=3").then((list) => {
      if (alive) setDrops(list);
    }).catch(() => {
      if (alive) setDrops([]);
    });
    return () => { alive = false; };
  }, []);

  useEffect(() => {
    if (!token) {
      setAlerts([]);
      setUnread(0);
      setTrackError("");
      setTrackBusy(false);
      return;
    }
    let alive = true;
    setTrackBusy(true);
    setTrackError("");
    Promise.all([
      apiFetch(`/v1/tracking?displayCurrency=${encodeURIComponent(displayCurrency)}`),
      apiFetch("/v1/notifications/unread-count"),
    ]).then(([trackings, count]) => {
      if (alive) { setAlerts(trackings); setUnread(count); setTrackError(""); }
    }).catch((err) => {
      if (!alive) return;
      setTrackError(err instanceof Error ? err.message : v.dashboardTrackingLoadError);
      notify(err.message);
    }).finally(() => {
      if (alive) setTrackBusy(false);
    });
    return () => { alive = false; };
  }, [token, notify, displayCurrency]);

  const atTarget = alerts.filter((t) => t.currentPriceInTargetCurrency != null && t.currentPriceInTargetCurrency <= t.targetPrice).length;
  const nearTarget = alerts.filter((t) => t.currentPriceInTargetCurrency != null && t.currentPriceInTargetCurrency > t.targetPrice && t.currentPriceInTargetCurrency <= t.targetPrice * 1.1).length;

  const loadFeedPage = useCallback(async (page = feedPageRef.current) => {
    if (feedBusyRef.current || feedDoneRef.current) return;
    feedBusyRef.current = true;
    const controller = new AbortController();
    feedControllerRef.current = controller;
    setFeedError("");
    setFeedState((current) => current === "ready" || current === "idle" ? "busy" : "loading");
    try {
      const result = await fetchFeedPage({ seed: seedRef.current, page, signal: controller.signal });
      const content = Array.isArray(result?.content) ? result.content : [];
      const fresh = content.filter((item) => {
        if (!item?.productId || seenRef.current.has(item.productId)) return false;
        seenRef.current.add(item.productId);
        return true;
      });
      writeSeen([...seenRef.current]);
      setFeedItems((items) => {
        const next = [...items, ...fresh];
        writeFeedItems(next);
        return next;
      });
      feedPageRef.current = page + 1;
      feedDoneRef.current = !!result?.last;
      try {
        sessionStorage.setItem(PAGES_KEY, String(page + 1));
        sessionStorage.setItem(DONE_KEY, result?.last ? "1" : "0");
      } catch { /* storage can be unavailable */ }
      setFeedState(!content.length && page === 0 ? "empty" : result?.last ? "done" : "idle");
    } catch (err) {
      if (controller.signal.aborted) return;
      setFeedError(err instanceof Error ? err.message : v.dashFeedError);
      setFeedState("error");
    } finally {
      if (feedControllerRef.current === controller) {
        feedBusyRef.current = false;
        feedControllerRef.current = null;
      }
    }
  }, [v.dashFeedError]);

  useEffect(() => {
    if (!feedDoneRef.current) loadFeedPage();
    return () => {
      feedControllerRef.current?.abort();
      feedBusyRef.current = false;
    };
  }, [loadFeedPage]);

  // progressive loading: a sentinel below the grid pulls the next page
  useEffect(() => {
    const node = sentinelRef.current;
    if (!node) return;
    if (typeof IntersectionObserver === "undefined") return;
    const observer = new IntersectionObserver((entries) => {
      if (!entries.some((e) => e.isIntersecting)) return;
      if (feedState !== "idle" && feedState !== "ready") return;
      loadFeedPage();
    }, { rootMargin: "480px 0px" });
    observer.observe(node);
    return () => observer.disconnect();
  }, [feedItems, feedState, loadFeedPage]);

  function restartFeed() {
    setFeedError("");
    feedControllerRef.current?.abort();
    feedBusyRef.current = false;
    feedDoneRef.current = false;
    seedRef.current = newSessionSeed();
    feedPageRef.current = 0;
    seenRef.current = new Set();
    writeSeen([]);
    setFeedItems([]);
    writeFeedItems([]);
    try { sessionStorage.setItem(PAGES_KEY, "0"); sessionStorage.setItem(DONE_KEY, "0"); } catch { /* storage can be unavailable */ }
    loadFeedPage(0);
  }

  return (
    <Fragment>
      <div className="page-heading dashboard-feed-title">
        <div className="dashboard-feed-title-copy">
          <div className="eyebrow">{v.dashFeedEyebrow}</div>
          <h1>{v.dashFeedHeading}</h1>
        </div>
        <button className="button button--tonal" onClick={onDiscover}>
          <Icon name="search" />
          {v.dashboardExplore}
        </button>
      </div>

      <section className="dash-feed" aria-labelledby="dash-feed-heading">
        <h2 id="dash-feed-heading" className="sr-only">{v.dashFeedHeading}</h2>

        {feedState === "loading" ? (
          <div className="feed-grid" aria-hidden="true">
            <div className="feed-skeleton feed-skeleton--feature" />
            <div className="feed-skeleton" />
            <div className="feed-skeleton" />
            <div className="feed-skeleton" />
            <div className="feed-skeleton feed-skeleton--wide" />
          </div>
        ) : feedState === "error" && !feedItems.length ? (
          <EmptyState
            title={v.dashFeedEmpty}
            body={feedError || v.dashFeedError}
            action={<button className="button button--quiet" onClick={restartFeed}>{v.dashboardTryAgain}</button>}
          />
        ) : feedState === "empty" && !feedItems.length ? (
          <EmptyState title={v.dashFeedEmpty} body={v.dashFeedEmptyBody} />
        ) : (
          <Fragment>
            <div className="feed-grid">
              {feedItems.map((item, index) => (
                <FeedTile key={item.productId} item={item} index={index} single={feedItems.length === 1} onOpen={openProduct} />
              ))}
            </div>
            {feedState === "busy" ? (
              <div className="feed-status">
                <LoadingIndicator className="search-loading-indicator--inline" label={v.dashFeedLoading} />
              </div>
            ) : null}
            {feedState === "error" ? (
              <div className="feed-status feed-status--error" role="status">
                <p>{feedError || v.dashFeedError}</p>
                <button className="button button--quiet" onClick={() => loadFeedPage()}>{v.dashboardTryAgain}</button>
              </div>
            ) : null}
            {feedState === "done" ? (
              <div className="feed-status feed-status--end" role="status">
                <span>{v.dashFeedEnd}</span>
                <button className="button button--tonal" onClick={restartFeed}>{v.dashFeedReshuffle}</button>
              </div>
            ) : null}
            <div className="feed-sentinel" ref={sentinelRef} aria-hidden="true" />
          </Fragment>
        )}
      </section>

      <RecentDropsSection drops={drops} onOpen={openProduct} onDiscover={onDiscover} />

      {token ? (
        <Fragment>
          <div className="section-heading">
            <div>
              <h2>{v.dashboardTrackingHeading}</h2>
            </div>
            <button
              className="text-button"
              onClick={() => {
                history.pushState({}, "", "/products");
                window.dispatchEvent(new PopStateEvent("popstate"));
              }}
            >
              {v.dashboardViewAll} <Icon name="arrow" />
            </button>
          </div>

          <div className="stat-grid">
            <MetricStat label={v.dashboardTrackedMetric} value={trackBusy || trackError ? "—" : alerts.length.toString()} note={v.dashboardTrackedNote} />
            <MetricStat label={v.dashboardAtTargetMetric} value={trackBusy || trackError ? "—" : atTarget.toString()} note={v.dashboardAtTargetNote} tone="green" />
            <MetricStat label={v.dashboardNearTargetMetric} value={trackBusy || trackError ? "—" : nearTarget.toString()} note={v.dashboardNearTargetNote} tone="amber" />
            <MetricStat label={v.dashboardUnreadMetric} value={trackBusy || trackError ? "—" : unread.toString()} note={v.dashboardUnreadNote} />
          </div>

          <div className="section-heading section-heading--spaced">
            <div>
              <h2>{v.dashboardRecentHeading}</h2>
            </div>
          </div>
          {trackBusy && !alerts.length ? <SkeletonTable rows={4} /> : alerts.length ? (
            <TrackingsTable
              items={alerts.slice(0, 5)}
              onOpen={openProduct}
              notify={notify}
              onRefresh={() => {
                apiFetch(`/v1/tracking?displayCurrency=${encodeURIComponent(displayCurrency)}`).then(setAlerts).catch((err) => notify(err.message));
              }}
            />
          ) : (
            <EmptyState
              title={trackError ? v.dashboardTrackingLoadError : v.dashboardNothingTracked}
              body={trackError || v.dashboardNothingTrackedBody}
              action={trackError ? <button className="button button--quiet" onClick={() => window.location.reload()}>{v.dashboardTryAgain}</button> : undefined}
            />
          )}
        </Fragment>
      ) : (
        <section className="guest-panel">
          <div>
            <div className="eyebrow">{v.dashboardGuestEyebrow}</div>
            <h2>{v.dashboardGuestHeading}</h2>
            <p>{v.dashboardGuestBody}</p>
          </div>
          <button className="button button--primary" onClick={onSignIn}>
            {v.dashboardGuestCta} <Icon name="arrow" />
          </button>
        </section>
      )}
    </Fragment>
  );
}

const FEED_COLOR_COUNT = 4;

function tileColor(item) {
  let hash = 0;
  const key = item.productId || item.name || "";
  for (let i = 0; i < key.length; i += 1) hash = (hash * 31 + key.charCodeAt(i)) | 0;
  return Math.abs(hash) % FEED_COLOR_COUNT;
}

// Mosaic variant: photos get standard/wide frames, the very first option
// shown can be promoted to the hero tile, and missing photos collapse into
// flat horizontal strips (nothing is invented in their place).
function tileVariant(item, index) {
  if (item.primaryImage) {
    if (index % 4 === 3) return "wide";
    return "standard";
  }
  return "flat";
}

function FeedTile({ item, index, single, onOpen }) {
  const v = useLocale();
  const currency = item.currencyCode || item.currency || "";
  const featured = index === 0 && !!item.primaryImage && item.lowestPrice != null;

  const variant = featured ? "feature" : tileVariant(item, index);
  const color = tileColor(item) % FEED_COLOR_COUNT;

  // Price movement — the signed percentage the backend computed against the
  // previous observation of the same listing in the same currency.
  const movement = buildMovement(item, v, formatShortDateOrDash);

  const media = (
    <div className="feed-media">
      <span className="feed-blob" aria-hidden="true" />
      {item.primaryImage ? (
        <ProductImage source={item.primaryImage} productId={item.productId} alt="" loading={index >= 4 ? "lazy" : "eager"} layered />
      ) : variant === "flat" ? null : (
        <span className="feed-media-fallback" aria-hidden="true">{(item.brand || item.name || "?").trim().charAt(0).toUpperCase()}</span>
      )}
    </div>
  );

  return (
    <article className={`feed-tile feed-tile--${variant} feed-tile--c${color}${featured ? " feed-tile--featured" : ""}${single ? " feed-tile--solo" : ""}`}>
      {(item.category || item.brand) ? (
        <span className="feed-chip" aria-hidden="true">{(item.category || item.brand).split(" ").slice(0, 3).join(" ")}</span>
      ) : null}
      {variant === "flat" ? null : media}
      <div className="feed-body">
        <h3 className="feed-name">{item.name}</h3>
        <div className="feed-pricing">
          <Price amount={item.lowestPrice} currencyCode={currency} />
          {item.previousPrice != null ? (
            <span className="feed-was">
              <span className="feed-was-label">{v.dashFeedWas}</span>
              <span className="feed-was-value">{formatDisplayValue(item.previousPrice, currency)}</span>
            </span>
          ) : null}
        </div>
        {movement ? (
          <span
            className={`feed-movement feed-movement--${movement.dir}`}
            title={movement.aria}
            aria-label={movement.aria}
            role="text"
          >
            <span aria-hidden="true" className="feed-movement-arrow">{movement.arrow}</span>
            {movement.short}
            {movement.date ? <span className="feed-movement-date"> · {movement.date}</span> : null}
          </span>
        ) : null}
        <div className="feed-meta">
          {item.storeCount > 0 ? <span className="feed-meta-bit">{v.dashFeedStores(item.storeCount)}</span> : null}
          {item.brand ? <span className="feed-meta-bit feed-meta-bit--brand">{item.brand}</span> : null}
        </div>
        <button type="button" className="feed-hit" onClick={() => item.productId && onOpen(item)} aria-label={v.dashFeedOpen(item.name)} />
      </div>
    </article>
  );
}

function buildMovement(item, v, formatDate) {
  const pct = Number(item.priceChangePercent);
  if (!Number.isFinite(pct) || pct === 0) return null;
  const dir = pct < 0 ? "down" : "up";
  const rounded = new Intl.NumberFormat(undefined, { maximumFractionDigits: 1 }).format(Math.abs(pct));
  const date = item.previousRecordedAt ? formatDate(item.previousRecordedAt) : "";
  const arrow = dir === "down" ? "↓" : "↑";
  const aria = dir === "down"
    ? v.dashFeedMovementDown(rounded, date)
    : v.dashFeedMovementUp(rounded, date);
  return { dir, arrow, short: `${rounded}%`, date, aria };
}
