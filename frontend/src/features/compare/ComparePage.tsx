// @ts-nocheck
import { useEffect, useState } from "react";
import { CompareHistoryChart } from "./CompareHistoryChart";
import { EmptyState } from "../../components/ui/EmptyState";
import { Icon } from "../../components/ui/Icon";
import { Price } from "../../components/ui/Price";
import { ProductImage } from "../../components/ui/ProductImage";
import { SkeletonTable } from "../../components/ui/skeletons";
import { apiFetch } from "../../lib/api";
import { useLocale } from "../../lib/i18n-context";

export function ComparePage({ token, ids, onRemove, onOpen, notify }) {
  const copy = useLocale();
  const [products, setProducts] = useState([]);
  const [offers, setOffers] = useState({});
  const [trackings, setTrackings] = useState({});
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    let active = true;
    if (!ids.length) {
      setProducts([]);
      setOffers({});
      setTrackings({});
      return;
    }

    setLoading(true);
    Promise.all(ids.map(async (id) => {
      const [product, listings] = await Promise.all([
        apiFetch(`/v1/products/${id}`).catch(() => null),
        apiFetch(`/v1/listings/by-product/${id}`).catch(() => []),
      ]);
      return { id, product, listings };
    })).then(async (rows) => {
      if (!active) return;
      setProducts(rows.flatMap((row) => row.product ? [row.product] : []));
      setOffers(Object.fromEntries(rows.map((row) => [row.id, row.listings])));
      if (rows.some((row) => !row.product)) notify(copy.compareError);
      if (!token) return setTrackings({});
      try {
        const rows = await apiFetch("/v1/tracking");
        if (active) setTrackings(rows.filter((row) => ids.includes(row.productId)).reduce((grouped, row) => {
          (grouped[row.productId] ||= []).push(row);
          return grouped;
        }, {}));
      } catch {
        if (active) setTrackings({});
      }
    }).finally(() => active && setLoading(false));

    return () => { active = false; };
  }, [ids, token, notify, copy.compareError]);

  const currentOffers = (productId) => (offers[productId] || [])
    .filter((offer) => offer.isActive && !offer.isExcluded && offer.currentPrice != null);
  const bestOffer = (productId) => [...currentOffers(productId)]
    .sort((a, b) => a.currentPrice - b.currentPrice)[0];
  const comparisonRows = [
    [copy.compareLatestPrice, (product) => <Price amount={product.lowestPrice} currencyCode={product.currencyCode || product.currency} />],
    [copy.compareTargetPrice, (product) => {
      const target = (trackings[product.productId] || [])[0];
      return target ? <><Price amount={target.targetPrice} currencyCode={target.currencyCode} /> · {target.isActive ? copy.statusTracking : copy.statusPaused}</> : copy.compareNotTracked;
    }],
    [copy.compareOffers, (product) => copy.compareOfferCount(currentOffers(product.productId).length)],
    [copy.compareBestStore, (product) => bestOffer(product.productId)?.storeName || "—"],
    [copy.compareMarket, (product) => bestOffer(product.productId)?.marketCountry || "—"],
    [copy.compareBrand, (product) => product.brand || copy.compareUnavailable],
    [copy.compareCategory, (product) => product.category || copy.compareUnavailable],
  ];

  return (
    <>
      <div className="page-heading">
        <div><div className="eyebrow">{copy.compareEyebrow}</div><h1>{copy.compareHeading}</h1></div>
        <span className="compare-limit">{ids.length} of 3 {copy.navCompare}</span>
      </div>
      {loading ? (
        <div className="compare-skeleton-wrap">
          <div className="compare-product-panels">
            {Array.from({ length: 2 }, (_, index) => <div className="compare-product-panel skeleton-card" key={index}>
              <div className="skeleton-box" style={{ width: "100%", height: 140, borderRadius: 6 }} />
              <div className="compare-product-copy"><div className="skeleton-box skeleton-text skeleton-title" /><div className="skeleton-box skeleton-text skeleton-price" /></div>
            </div>)}
          </div>
          <SkeletonTable rows={5} />
        </div>
      ) : products.length ? (
        <>
          <div className="compare-product-panels">
            {products.map((product) => {
              const best = bestOffer(product.productId);
              return <article className="compare-product-panel" key={product.productId}>
                <div className="compare-product-image"><ProductImage source={product.primaryImage} baseUrl={product.productUrl || ""} productId={product.productId} alt={product.name} /></div>
                <div className="compare-product-copy">
                  <button className="product-link" onClick={() => onOpen(product.productId)}>{product.name}</button>
                  <p>{[product.brand, product.category].filter(Boolean).join(" · ") || copy.compareUnavailable}</p>
                  <Price className="compare-product-price" amount={product.lowestPrice} currencyCode={product.currencyCode || product.currency} />
                  <small>{copy.compareOfferCount(currentOffers(product.productId).length)}{best ? ` · ${best.storeName}` : ""}</small>
                  <div className="compare-product-actions">
                    <button className="text-button" onClick={() => onOpen(product.productId)}>{copy.compareViewProduct} <Icon name="arrow" /></button>
                    <button className="remove-compare" aria-label={`${copy.compareRemove} ${product.name}`} onClick={() => onRemove(product.productId)}><Icon name="close" /></button>
                  </div>
                </div>
              </article>;
            })}
          </div>
          <section className="compare-history-section">
            <div className="section-heading"><div><h2>{copy.priceHistory}</h2><p>{copy.compareHistoryIntro}</p></div></div>
            <div className="compare-history-grid">
              {products.map((product) => {
                const currency = product.currencyCode || product.currency || "USD";
                const rows = currentOffers(product.productId).filter((offer) => offer.currencyCode === currency);
                return <article className="compare-history-card" key={product.productId}>
                  <h3>{product.name}</h3>
                  {rows.length ? <CompareHistoryChart listings={rows} currencyCode={currency} /> : <p className="dialog-help">{copy.compareNoHistoryForCurrency(currency)}</p>}
                </article>;
              })}
            </div>
          </section>
          <div className="compare-table-wrap">
            <table className="compare-table">
              <caption className="sr-only">{copy.compareHeading}</caption>
              <thead><tr><th scope="col">{copy.compareProduct}</th>{products.map((product) => <th scope="col" key={product.productId}>
                <button className="product-link" onClick={() => onOpen(product.productId)}>{product.name}</button>
              </th>)}</tr></thead>
              <tbody>{comparisonRows.map(([label, value]) => <tr key={label}>
                <th scope="row">{label}</th>{products.map((product) => <td key={product.productId}>{value(product)}</td>)}
              </tr>)}</tbody>
              <tfoot><tr><th scope="row">{copy.compareOffers}</th>{products.map((product) => <td key={product.productId}>
                <button className="text-button" onClick={() => onOpen(product.productId)}>{copy.compareViewProduct} <Icon name="arrow" /></button>
              </td>)}</tr></tfoot>
            </table>
          </div>
        </>
      ) : <EmptyState title={copy.compareEmpty} body={copy.compareEmptyBody} action={null} />}
    </>
  );
}
