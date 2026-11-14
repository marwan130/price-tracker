// @ts-nocheck
import { APP_NAME } from "../../lib/brand";
import { Dropdown } from "../../components/ui/Dropdown";
import { EmptyState } from "../../components/ui/EmptyState";
import { Fragment } from "react";
import { Icon } from "../../components/ui/Icon";
import { Price } from "../../components/ui/Price";
import { ProductImage } from "../../components/ui/ProductImage";
import { apiFetch, asUuid } from "../../lib/api";
import { formatDateTime, formatDisplayValue, formatShortDateOrDash } from "../../lib/format";
import { useEffect, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

const SERIES_COLORS = ["#ff6257", "#4d91d8", "#55a878", "#f28c38"];
const HISTORY_PAGE_SIZE = 200;
const DAY_MS = 864e5;
const STALE_AFTER_DAYS = 14;

// Offer trust from the actual backend status model: HasVerifiedPrice tells
// whether the price was verified on the last scrape, LastScrapedAt how
// recently, and a listing with no stored observation has nothing to show.
function offerTrust(listing) {
  if (!listing.isActive) return "inactive";
  if (listing.currentPrice == null) return "noprice";
  const checkedAt = listing.lastScrapedAt ? new Date(listing.lastScrapedAt).getTime() : NaN;
  if (!Number.isFinite(checkedAt) || Date.now() - checkedAt > STALE_AFTER_DAYS * DAY_MS) return "stale";
  return listing.hasVerifiedPrice ? "verified" : "unverified";
}

// Hero movement derived from already-fetched observations only. Each store is
// compared strictly against itself (same store, same currency, 24h–30d gap),
// so no price trains against a rival store, and the
// label is anchored to the store that currently holds the floor.
function heroMovement(observations) {
  if (!observations?.length) return null;
  const byCurrency = new Map();
  for (const obs of observations) {
    const key = obs.currencyCode || "";
    if (!byCurrency.has(key)) byCurrency.set(key, []);
    byCurrency.get(key).push(obs);
  }
  const currencyGroup = [...byCurrency.values()].sort((a, b) => b.length - a.length)[0];
  if (!currencyGroup || currencyGroup.length < 2) return null;

  const byStore = new Map();
  for (const obs of currencyGroup) {
    if (!byStore.has(obs.storeName)) byStore.set(obs.storeName, []);
    byStore.get(obs.storeName).push(obs);
  }
  const movements = [];
  for (const [storeName, list] of byStore) {
    list.sort((a, b) => new Date(a.recordedAt) - new Date(b.recordedAt));
    const latest = list[list.length - 1];
    const previous = list.slice(0, -1).reverse().find((obs) => {
      const gap = new Date(latest.recordedAt) - new Date(obs.recordedAt);
      return gap > DAY_MS && gap <= 30 * DAY_MS;
    });
    if (!previous || previous.price <= 0) continue;
    const percent = Math.round(Math.abs((latest.price - previous.price) / previous.price * 100));
    if (percent === 0) continue;
    movements.push({
      storeName: storeName || "",
      dir: latest.price < previous.price ? "down" : "up",
      percent,
      since: formatShortDateOrDash(previous.recordedAt),
    });
  }
  if (!movements.length) return null;
  // Prefer the decrease at the store now holding the lowest price; fall back
  // to the first detected movement otherwise.
  const floorStoreName = [...byStore.entries()]
    .map(([store, list]) => list[list.length - 1] && { store, latest: list[list.length - 1] })
    .filter((entry) => entry.latest)
    .sort((a, b) => a.latest.price - b.latest.price)[0]?.store;
  const downAtFloor = movements.find((m) => m.dir === "down" && m.storeName === floorStoreName)
    || movements.find((m) => m.dir === "down")
    || movements.sort((a, b) => a.storeName.localeCompare(b.storeName))[0];
  if (downAtFloor.dir === "down" && floorStoreName) downAtFloor.storeName = floorStoreName;
  return downAtFloor;
}

export function ProductDetailsPage({ id, candidate, token, notify, onCompare, compareIds, onBack, onTrack }) {
  const v = useLocale();

  const [product, setProduct] = useState(null);
  useEffect(() => {
    product?.name && (document.title = `${product.name} · ${APP_NAME}`);
  }, [product?.name]);

  const [offers, setOffers] = useState([]);
  const [offersLoaded, setOffersLoaded] = useState(false);
  const [trackings, setTrackings] = useState([]);
  const [targetBusyId, setTargetBusyId] = useState(null);
  const [histories, setHistories] = useState({});
  const [historyError, setHistoryError] = useState("");
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historySampled, setHistorySampled] = useState(false);
  const [historyReloadTick, setHistoryReloadTick] = useState(0);
  const [pageError, setPageError] = useState("");
  const [range, setRange] = useState("30");
  const [hiddenStores, setHiddenStores] = useState([]);

  async function saveTarget(tracking, event) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const targetPrice = Number(data.get("targetPrice"));
    if (!Number.isFinite(targetPrice) || targetPrice <= 0) {
      notify(v.previewInvalidPrice);
      return;
    }
    setTargetBusyId(tracking.trackingId);
    try {
      const saved = await apiFetch(`/v1/tracking/${tracking.trackingId}`, {
        method: "PUT",
        body: JSON.stringify({
          targetPrice,
          notifyEmail: data.has("notifyEmail"),
          notifyFurtherDecreaseEmail: data.has("notifyFurtherDecreaseEmail"),
        }),
      });
      setTrackings((items) => items.map((t) => t.trackingId === tracking.trackingId ? { ...t, ...saved } : t));
      notify(v.trackingPreferencesSaved);
    } catch (err) {
      notify(err instanceof Error ? err.message : v.actionFailed);
    } finally {
      setTargetBusyId(null);
    }
  }

  async function toggleOfferExclusion(listing) {
    try {
      await apiFetch(`/v1/listings/${listing.listingId}/${listing.isExcluded ? "include" : "exclude"}`, { method: "PATCH" });
      setOffers((items) => items.map((t) => t.listingId === listing.listingId ? { ...t, isExcluded: !listing.isExcluded } : t));
      notify(listing.isExcluded ? v.productOfferIncluded(listing.storeName) : v.productOfferExcluded(listing.storeName));
    } catch (err) {
      notify(err.message);
    }
  }

  useEffect(() => {
    let alive = true;
    setPageError("");
    setTargetBusyId(null);
    setProduct(null);
    setOffers([]);
    setOffersLoaded(false);
    setTrackings([]);
    if (!asUuid(id)) {
      // "candidate" is a search-result offer with no catalog id yet; the page
      // renders from the card data instead of /v1/products/:id.
      if (id === "candidate" && candidate?.productUrl) {
        setProduct({
          productId: null,
          name: candidate.name || "",
          description: candidate.description || "",
          brand: "",
          category: "",
          storeName: candidate.storeName || "",
          productUrl: candidate.productUrl,
          primaryImage: candidate.imageUrl || "",
          lowestPrice: candidate.price,
          currency: candidate.currency || "",
          isVerified: candidate.isVerified === true,
        });
        setOffersLoaded(true);
        return () => { alive = false; };
      }
      setPageError(v.productDetailsUnavailable);
      return () => { alive = false; };
    }
    (async () => {
      try {
        const loaded = await apiFetch(`/v1/products/${id}`);
        if (!alive) return;
        setProduct(loaded);
        const [listings, trackingRows] = await Promise.all([
          apiFetch(`/v1/listings/by-product/${id}`).catch(() => []),
          token ? apiFetch("/v1/tracking").catch(() => []) : Promise.resolve([]),
        ]);
        if (!alive) return;
        setTrackings(Array.isArray(trackingRows) ? trackingRows.filter((t) => t.productId === id) : []);
        setOffers((Array.isArray(listings) ? listings : []).sort((a, b) => (a.currencyCode || "").localeCompare(b.currencyCode || "") || (a.currentPrice ?? Infinity) - (b.currentPrice ?? Infinity)));
        setOffersLoaded(true);
      } catch (err) {
        alive && setPageError(err instanceof Error ? err.message : v.productDetailsUnavailable);
      }
    })();
    return () => { alive = false; };
  }, [id, token, candidate]);

  // Per-offer price history. Very long histories are sampled (start /
  // middle / end pages) so the shape of the whole range stays visible
  // without hammering the API.
  useEffect(() => {
    if (!offers.length) {
      setHistories({});
      setHistoryError("");
      setHistorySampled(false);
      setHistoryLoading(false);
      return;
    }
    let alive = true;
    let failures = 0;
    let sampled = false;
    setHistories({});
    setHistoryError("");
    setHistorySampled(false);
    setHistoryLoading(true);
    const rangeQuery = (page) => {
      const params = new URLSearchParams({ Page: String(page), Size: String(HISTORY_PAGE_SIZE) });
      if (range !== "all") params.set("From", new Date(Date.now() - Number(range) * DAY_MS).toISOString());
      return params;
    };
    Promise.all(offers.map(async (listing) => {
      try {
        const first = await apiFetch(`/v1/price-history/by-listing/${listing.listingId}?${rangeQuery(0)}`);
        const pageCount = Math.ceil(first.totalElements / HISTORY_PAGE_SIZE);
        if (pageCount <= 1) return [listing.listingId, first.content];
        sampled = sampled || pageCount > 3;
        const extraPages = [...new Set([0, Math.floor((pageCount - 1) / 2), pageCount - 1])].filter((page) => page > 0);
        const rest = await Promise.all(extraPages.map(async (page) => {
          try {
            return (await apiFetch(`/v1/price-history/by-listing/${listing.listingId}?${rangeQuery(page)}`)).content;
          } catch {
            failures += 1;
            return [];
          }
        }));
        const merged = [first.content, ...rest].flat();
        return [listing.listingId, [...new Map(merged.map((obs) => [obs.id, obs])).values()].sort((a, b) => new Date(a.recordedAt) - new Date(b.recordedAt))];
      } catch {
        failures += 1;
        return [listing.listingId, []];
      }
    })).then((results) => {
      if (!alive) return;
      setHistories(Object.fromEntries(results));
      setHistorySampled(sampled);
      failures && setHistoryError(v.productHistoryLoadFailed);
    }).finally(() => {
      alive && setHistoryLoading(false);
    });
    return () => { alive = false; };
  }, [offers, range, historyReloadTick, v]);

  const visibleOffers = offers;
  const verifiedOffers = visibleOffers.filter((listing) => !listing.isExcluded && offerTrust(listing) === "verified" && listing.currentPrice > 0);
  const verifiedCurrencies = new Set(verifiedOffers.map((listing) => listing.currencyCode));
  const currentOffer = verifiedCurrencies.size === 1 ? verifiedOffers.sort((a, b) => a.currentPrice - b.currentPrice)[0] : null;
  const heroPrice = asUuid(id) ? offersLoaded ? currentOffer?.currentPrice ?? null : null : product?.lowestPrice;
  const heroCurrency = currentOffer?.currencyCode || product?.currencyCode || product?.currency;
  const heroPriceLabel = currentOffer
    ? v.productPriceSource(currentOffer.storeName)
    : asUuid(id) ? v.productPriceNotConfirmed
    : product?.isVerified ? v.productPriceSource(product.storeName) : v.productOfferUnverified;
  const trackableOffer = currentOffer || visibleOffers.find((listing) => listing.isActive && !listing.isExcluded && listing.productUrl);
  const since = range === "all" ? 0 : Date.now() - Number(range) * DAY_MS;
  const observations = visibleOffers.flatMap((listing) =>
    (histories[listing.listingId] || [])
      .filter((obs) => new Date(obs.recordedAt).getTime() >= since)
      .map((obs) => ({ ...obs, storeName: listing.storeName })))
    .sort((a, b) => new Date(a.recordedAt) - new Date(b.recordedAt));
  const visible = observations.filter((obs) => !hiddenStores.includes(obs.storeName));
  const storeNames = [...new Set(observations.map((obs) => obs.storeName))];
  const chartCurrencies = [...new Set(visible.map((obs) => obs.currencyCode))];
  const minPrice = visible.length ? Math.min(...visible.map((o) => o.price)) : 0;
  const maxPrice = visible.length ? Math.max(...visible.map((o) => o.price)) : 0;
  const minTime = visible.length ? +new Date(visible[0].recordedAt) : 0;
  const maxTime = visible.length ? +new Date(visible[visible.length - 1].recordedAt) : 0;
  const xPct = (obs) => maxTime === minTime ? 50 : ((+new Date(obs.recordedAt) - minTime) / (maxTime - minTime)) * 94 + 3;
  const yPct = (obs) => maxPrice === minPrice ? 50 : 92 - ((obs.price - minPrice) / (maxPrice - minPrice)) * 84;
  const seriesPath = (storeName) => {
    const rows = visible.filter((obs) => obs.storeName === storeName);
    const gaps = rows.slice(1)
      .map((obs, index) => +new Date(obs.recordedAt) - +new Date(rows[index].recordedAt))
      .filter((gap) => gap > 0)
      .sort((a, b) => a - b);
    const typicalGap = gaps.length ? gaps[Math.floor((gaps.length - 1) / 2)] : 0;
    return rows
      .map((obs, index) => {
        const leap = index > 0 && typicalGap > 0
          && +new Date(obs.recordedAt) - +new Date(rows[index - 1].recordedAt) > typicalGap * 2.5;
        return `${index === 0 || leap ? "M" : "L"} ${xPct(obs)} ${yPct(obs)}`;
      })
      .join(" ");
  };
  const bestListingId = visibleOffers.find((listing) => listing.currentPrice != null && listing.isActive && !listing.isExcluded && offerTrust(listing) === "verified")?.listingId ?? null;
  const movement = product ? heroMovement(observations) : null;

  if (pageError) {
    return (
      <EmptyState
        title={v.productDetailsUnavailable}
        body={pageError}
        action={<button className="button button--tonal" onClick={onBack}>{v.compareBackToProducts}</button>}
      />
    );
  }
  if (!product) {
    return <div className="loading-surface">{v.productDetailsLoading}</div>;
  }

  return (
    <div className="product-detail-page">
      <button className="back-link product-detail-back" onClick={onBack}><Icon name="chevron-left" />{v.compareBackToProducts}</button>
      {/* product-hero class feeds product-details-enhancements.ts (sticky bar + activity). */}
      <header className="product-detail-heading product-hero">
        <div className="product-detail-identity"><span className="eyebrow">{v.breadcrumbProductDetails}</span><h1>{product.name}</h1>
          <div className="product-detail-meta">{[product.brand, product.category, product.storeName].filter(Boolean).map((item, index) => <span key={item + "-" + index}>{item}</span>)}</div>
        </div>
        {asUuid(id) ? <button className="button button--tonal product-detail-compare" onClick={() => onCompare(id)}>{compareIds.includes(id) ? v.discoverRemoveCompare : v.discoverAddCompare}</button> : null}
      </header>
      <section className="product-detail-overview">
        <div className="product-detail-image">{product.primaryImage ? <ProductImage source={product.primaryImage} baseUrl={product.productUrl} productId={product.productId} alt={product.name} layered /> : <div className="product-detail-image-fallback" role="img" aria-label={v.productImageMissing}><span aria-hidden="true">{(product.brand || product.name || "?").trim().charAt(0).toUpperCase()}</span></div>}</div>
        <div className="product-detail-buy">
          <div className="product-detail-price-label">{v.compareLatestPrice}</div><div className="product-detail-price"><Price amount={heroPrice} currencyCode={heroCurrency} /></div><p className="product-detail-price-source">{heroPriceLabel}</p>
          {movement ? <p className={"product-detail-movement product-detail-movement--" + movement.dir}><strong>{movement.dir === "down" ? "↓" : "↑"} {movement.percent}%</strong><span>{movement.dir === "down" ? v.productMovementDown(movement.percent, movement.since) : v.productMovementUp(movement.percent, movement.since)}</span>{movement.storeName ? <span>{movement.storeName}</span> : null}</p> : null}
          <div className="product-detail-actions">
            {(!asUuid(id) || offers.some((listing) => listing.isActive)) ? <button className="button button--primary" disabled={asUuid(id) ? !trackableOffer : !product.productUrl} onClick={() => { if (!asUuid(id)) { candidate && onTrack({ ...candidate }); return; } trackableOffer && onTrack({ name: product.name, productUrl: trackableOffer.productUrl, imageUrl: product.primaryImage, storeName: trackableOffer.storeName, currency: trackableOffer.currencyCode, price: trackableOffer.currentPrice, isVerified: trackableOffer.hasVerifiedPrice, existingProductId: id }); }}>{v.discoverTrackPrice}</button> : null}
            <a className="quiet-link" href={asUuid(id) ? "#offers" : product.productUrl} target={asUuid(id) ? undefined : "_blank"} rel="noreferrer">{v.compareViewProduct} <Icon name="external" /></a>
          </div>
          <div className="product-detail-targets"><strong>{v.colTargetPrice}</strong>{trackings.length ? trackings.map((tracking) => <span key={tracking.trackingId} className={"status " + (tracking.isActive ? "status--active" : "status--paused")}><i /><Price amount={tracking.targetPrice} currencyCode={tracking.currencyCode} />{" · "}{tracking.isActive ? v.statusTracking : v.statusPaused}</span>) : <span>{token ? v.colTargetPrice : v.signIn}</span>}</div>
        </div>
        <aside className="product-detail-facts"><div><span>{v.productMarketCurrency}</span><strong>{heroCurrency || v.compareUnavailable}</strong></div><div><span>{v.storeOffersHeading}</span><strong>{v.productOffersCount(visibleOffers.length)}</strong></div>{currentOffer ? <div><span>{v.colStore}</span><strong>{currentOffer.storeName}</strong></div> : null}</aside>
      </section>
      {trackings.length > 0 ? (
        <section className="content-section tracking-preference-section">
          <div className="section-heading">
            <div>
              <h2>{v.trackingPreferencesHeading}</h2>
              
            </div>
          </div>
          <div className="tracking-preference-grid">
            {trackings.map((tracking) => (
              <form
                key={tracking.trackingId}
                className="tracking-preference-card"
                onSubmit={(event) => saveTarget(tracking, event)}
              >
                <div className="tracking-preference-title">
                  <strong>{tracking.storeName || tracking.marketCountry || product.name}</strong>
                  <span className={`status ${tracking.isActive ? "status--active" : "status--paused"}`}>
                    <i />
                    {tracking.isActive ? v.statusTracking : v.statusPaused}
                  </span>
                </div>
                <label className="field-label" htmlFor={`target-${tracking.trackingId}`}>
                  {v.colTargetPrice} · {tracking.currencyCode}
                </label>
                <input
                  className="field"
                  id={`target-${tracking.trackingId}`}
                  name="targetPrice"
                  type="number"
                  inputMode="decimal"
                  min="0.01"
                  step="any"
                  defaultValue={tracking.targetPrice}
                  required
                />
                <fieldset className="email-preferences">
                  <legend>{v.previewEmailPreferences}</legend>
                  <label>
                    <input type="checkbox" name="notifyEmail" defaultChecked={tracking.notifyEmail} />
                    {v.trackingEmailTarget}
                  </label>
                  <label>
                    <input type="checkbox" name="notifyFurtherDecreaseEmail" defaultChecked={tracking.notifyFurtherDecreaseEmail ?? false} />
                    {v.trackingEmailFurther}
                  </label>
                </fieldset>
                <button className="button button--primary" type="submit" disabled={targetBusyId === tracking.trackingId}>
                  {v.trackingSavePreferences}
                </button>
              </form>
            ))}
          </div>
        </section>
      ) : null}

      <section className="content-section product-detail-section" id="offers">
        <div className="section-heading">
          <div>
            <h2>{v.storeOffersHeading}</h2>
            
          </div>
          <div className="product-offer-controls">
            <span className="results-count">{v.productOffersCount(visibleOffers.length)}</span>
          </div>
        </div>
        {visibleOffers.length ? (
          <div className="table-wrap">
            <table className="offers-table">
              <thead>
                <tr>
                  <th>{v.colStore}</th>
                  <th>{v.colCurrentPrice}</th>
                  <th>{v.productMarketCurrency}</th>
                  <th>{v.productLastChecked}</th>
                  <th>{v.dealQualityHeading}</th>
                  <th>{v.colStatus}</th>
                  <th>{v.productMonitoring}</th>
                </tr>
              </thead>
              <tbody>
                {visibleOffers.map((listing) => {
                  const trust = offerTrust(listing);
                  return (
                    <tr key={listing.listingId} className={listing.listingId === bestListingId ? "offer-row--best" : ""}>
                      <td>
                        <a className="product-link" href={listing.productUrl} target="_blank" rel="noreferrer">
                          {listing.storeName}
                          <Icon name="external" />
                        </a>
                        {listing.listingId === bestListingId ? <span className="offer-best-flag">{v.productOfferBest}</span> : null}
                      </td>
                      <td className="price-cell">
                        <div className="offer-price">
                          <Price amount={listing.currentPrice} currencyCode={listing.currencyCode} />
                          <span
                            className={`offer-flag offer-flag--${trust}`}
                            title={trust === "verified" ? v.productOfferVerifiedHint : trust === "stale" ? v.productOfferStaleHint : trust === "unverified" ? v.productOfferUnverifiedHint : v.productOfferNoPriceHint}
                          >
                            {trust === "verified" ? v.productOfferVerified
                              : trust === "stale" ? v.productOfferStale
                              : trust === "unverified" ? v.productOfferUnverified
                              : trust === "inactive" ? v.productInactiveListing
                              : v.productOfferNoPrice}
                          </span>
                        </div>
                      </td>
                      <td>{listing.currencyCode || v.compareUnavailable}</td>
                      <td>{formatShortDateOrDash(listing.lastScrapedAt)}</td>
                      <td>
                        <span
                          className={`deal-quality${listing.dealQuality?.score != null ? " deal-quality--scored" : ""}`}
                          title={v.dealQualityTooltip}
                          aria-label={listing.dealQuality?.score != null ? `${v.dealQualityScoreLabel(listing.dealQuality.score)}. ${v.dealQualityTooltip}` : v.dealQualityInsufficient}
                        >
                          {listing.dealQuality?.score != null ? v.dealQualityScoreLabel(listing.dealQuality.score) : v.dealQualityInsufficient}
                        </span>
                      </td>
                      <td>
                        <span className={`status ${listing.isActive ? "status--active" : "status--paused"}`}>
                          <i />
                          {listing.isActive ? v.productActiveListing : v.productInactiveListing}
                        </span>
                      </td>
                      <td>
                        {token && trackings.length > 0 ? (
                          <button className="text-button" disabled={!listing.isActive} onClick={() => toggleOfferExclusion(listing)}>
                            {listing.isExcluded ? v.productIncludeOffer : v.productExcludeOffer}
                          </button>
                        ) : listing.isExcluded ? v.productExcluded : v.productIncluded}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState title={v.productNoStoredOffers} body={v.productNoStoredOffersBody} />
        )}
      </section>

      <section className="content-section product-detail-section product-history-section">
        <div className="section-heading product-history-heading">
          <div><span className="eyebrow">{v.breadcrumbProductDetails}</span><h2>{v.priceHistory}</h2></div>
          <Dropdown
            className="filter-dropdown range-select"
            ariaLabel={v.priceHistory}
            value={range}
            options={[
              { value: "7", label: v.productHistoryPeriod7 },
              { value: "30", label: v.compareHistoryPeriod30 },
              { value: "90", label: v.compareHistoryPeriod90 },
              { value: "365", label: v.compareHistoryPeriod365 },
              { value: "all", label: v.compareHistoryPeriodAll },
            ]}
            onChange={(next) => setRange(next)}
          />
        </div>

        {storeNames.length > 1 ? (
          <div className="history-store-controls" role="group" aria-label={v.productHistoryByStore}>
            {storeNames.map((storeName, index) => (
              <label key={storeName}>
                <input
                  type="checkbox"
                  checked={!hiddenStores.includes(storeName)}
                  onChange={() => setHiddenStores((current) => current.includes(storeName)
                    ? current.filter((s) => s !== storeName)
                    : [...current, storeName])}
                />
                <i className={`legend-dot legend-dot--${index % 4}`} />
                {storeName}
              </label>
            ))}
          </div>
        ) : null}

        {historyLoading ? (
          <div className="skeleton-box" style={{ height: "220px", borderRadius: "8px", margin: "14px 0" }} />
        ) : historyError && !visible.length ? (
          <EmptyState
            title={v.compareHistoryHeading}
            body={historyError}
            action={<button className="button button--quiet" onClick={() => setHistoryReloadTick((tick) => tick + 1)}>{v.tryAgain}</button>}
          />
        ) : visible.length ? (
          <Fragment>
            {historyError ? <p className="verification-note" role="status">{v.productHistoryPartial}</p> : null}
            {historySampled ? <p className="verification-note" role="status">{v.productHistorySampled}</p> : null}
            {chartCurrencies.length === 1 ? (
              <Fragment>
                <div className="history-chart">
                  <div className="chart-scale" aria-hidden="true">
                    <span>{formatDisplayValue(maxPrice, chartCurrencies[0])}</span>
                    <span>{formatDisplayValue((minPrice + maxPrice) / 2, chartCurrencies[0])}</span>
                    <span>{formatDisplayValue(minPrice, chartCurrencies[0])}</span>
                  </div>
                  <div
                    className="chart-plot"
                    role="group"
                    aria-label={v.productHistoryChartLabel(formatShortDateOrDash(visible[0].recordedAt), formatShortDateOrDash(visible[visible.length - 1].recordedAt), formatDisplayValue(minPrice, chartCurrencies[0]), formatDisplayValue(maxPrice, chartCurrencies[0]))}
                  >
                    {storeNames.map((storeName, index) => (
                      <svg key={storeName} className="chart-series" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">
                        <path d={seriesPath(storeName)} fill="none" stroke={SERIES_COLORS[index % SERIES_COLORS.length]} strokeWidth={index === 0 ? 2.4 : 1.6} strokeLinecap="round" strokeLinejoin="round" />
                      </svg>
                    ))}
                    {visible.map((obs, index) => (
                      <button
                        key={`${obs.id}-${index}`}
                        type="button"
                        className="chart-point"
                        style={{ left: `${xPct(obs)}%`, top: `${yPct(obs)}%`, backgroundColor: SERIES_COLORS[Math.max(0, storeNames.indexOf(obs.storeName)) % SERIES_COLORS.length] }}
                        title={`${formatDisplayValue(obs.price, obs.currencyCode)} · ${formatShortDateOrDash(obs.recordedAt)} · ${obs.storeName}`}
                        aria-label={`${formatShortDateOrDash(obs.recordedAt)} · ${formatDisplayValue(obs.price, obs.currencyCode)} · ${obs.storeName}`}
                      />
                    ))}
                  <div className="chart-date-labels">
                    <span>{formatDateTime(visible[0].recordedAt, { month: "short", day: "numeric" })}</span>
                    <span>{formatDateTime(visible[visible.length - 1].recordedAt, { month: "short", day: "numeric" })}</span>
                  </div>
                  </div>
                </div>
                <div className="history-legend">
                  {storeNames.map((storeName, index) => (
                    <span key={storeName}>
                      <i className={`legend-dot legend-dot--${index % 4}`} />
                      {storeName}
                    </span>
                  ))}
                </div>
              </Fragment>
            ) : (
              <div className="history-observation-list">
                {visible.map((obs) => (
                  <p key={`${obs.id}-${obs.recordedAt}`}>
                    {formatDisplayValue(obs.price, obs.currencyCode)} · {formatShortDateOrDash(obs.recordedAt)} · {obs.storeName}
                  </p>
                ))}
              </div>
            )}
          </Fragment>
        ) : (
          <EmptyState title={v.productHistoryEmpty} body={v.productHistoryEmptyBody} />
        )}
      </section>

    </div>
  );
}
