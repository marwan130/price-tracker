// @ts-nocheck
import { API_BASE_URL } from "../../lib/api";
import { cachedCrossRate, convertCurrency } from "../../lib/currency-client";
import { DealCardSkeleton, DealCardSkeletons } from "../../components/ui/skeletons";
import { DiscoveryOverview } from "./DiscoveryOverview";
import { Dropdown } from "../../components/ui/Dropdown";
import { EmptyState } from "../../components/ui/EmptyState";
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { Icon } from "../../components/ui/Icon";
import { DisplayCurrencyContext } from "../../lib/i18n-context";
import { LoadingIndicator } from "../../components/ui/LoadingIndicator";
import { Price } from "../../components/ui/Price";
import { ProductImage } from "../../components/ui/ProductImage";
import { apiFetch } from "../../lib/api";
import { asUuid } from "../../lib/api";
import { marketCurrencyOf } from "../../lib/markets";
import { normalizeProductUrl } from "../../lib/api";
import { useCallback, useContext, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function DiscoverPage({ query: c, categoryId: f, market: x, searchPulse: d, onSearch: p, onPreview: N, onCompare: v, compareIds: U, onOpen: S, onBack: back }) {
  const o = useLocale(), [A, Y] = useState([]), [w, le] = useState(false), [ae, se] = useState(false), [ie, O] = useState(0), z = useRef(0), $ = useRef(null), _ = useRef(null), visibleCandidateCount = useRef(0), loadMoreInFlight = useRef(false), sentinelLeftViewport = useRef(true);
  // Price inputs are typed in the currency chosen in settings; the market's
  // own currency only appears when no display currency is set.
  const displayCurrency = useContext(DisplayCurrencyContext);
  const pageRef = useRef(ie);
  pageRef.current = ie;
  useEffect(() => () => _.current?.close(), []);
  const [M, X] = useState(""), [G, Z] = useState("relevance"), re = useRef(G);
  re.current = G;
  const [xe, je] = useState(null), [te, be] = useState(""), [he, ne] = useState(void 0), [T, H] = useState(void 0), [I, ue] = useState(void 0), [g, k] = useState(void 0);
  useEffect(() => {
    const h = setTimeout(() => {
      ue(he), k(T);
    }, 350);
    return () => clearTimeout(h);
  }, [he, T]);
  // Slot counting needs the rendered column count of the results grid: the
  // grid is `auto-fill`, so the number of columns in the last row (and the
  // hole we must fill) changes with the viewport width. Skeleton slots round
  // the card count up to a full multiple of that column count — otherwise the
  // last row ends short and shows a patch of empty grid. Slots stay mounted
  // while a page is streaming OR the sentinel has more pages queued, so they
  // never vanish between two pages and reappear a beat later.
  // The grid mounts lazily with the first result, so the measurer hangs off a
  // callback ref and re-observes every time the node appears, not just once.
  const searchGridRef = useRef(null), gridRoRef = useRef(null), [gridColumns, sg] = useState(4);
  const measureGrid = useCallback(() => {
    const grid = searchGridRef.current;
    if (!grid) return;
    const count = getComputedStyle(grid).gridTemplateColumns.trim().split(/\s+/).filter(Boolean).length;
    if (count > 0) sg(count);
  }, []);
  const attachSearchGrid = useCallback((node) => {
    searchGridRef.current = node;
    gridRoRef.current?.disconnect();
    gridRoRef.current = null;
    if (!node) return;
    measureGrid();
    const ro = new ResizeObserver(measureGrid);
    ro.observe(node);
    gridRoRef.current = ro;
  }, [measureGrid]);
  useEffect(() => () => gridRoRef.current?.disconnect(), []);
  const [K, Q] = useState([]), [ee, me] = useState(true), [B, Te] = useState({}), loadMoreObserver = useRef(null), aeRef = useRef(false), J = useCallback(async (h, W, F, de = 0, Ne = false) => {
    if (!h.trim() || Ne && loadMoreInFlight.current) return;
    if (!Ne) sentinelLeftViewport.current = true;
    const Ce = ++z.current;
    if (W != null && F != null && W > F) {
      _.current?.close(), _.current = null, le(false), X(o.discoverPriceRangeError);
      return;
    }
    loadMoreInFlight.current = true, le(true), X(""), Ne || (Y([]), se(false), O(0), visibleCandidateCount.current = 0);
    const Be = new URLSearchParams({ Query: h.trim(), SortBy: re.current, Page: String(de), ...x ? { Market: x } : {}, ...f ? { CategoryId: f } : {} });
    // Bounds typed in the display currency must reach the server in the
    // market's own units: the backend compares each offer against the market
    // currency (EGP fallback when no market is chosen). Same snapshot-based
    // cross rates Price components display with (currency-client). When the
    // rate snapshot is unavailable, the bounds are dropped instead of sent in
    // the wrong currency — the server keeps everything, matching its policy
    // of not hiding offers whose conversion fails.
    const referenceCurrency = marketCurrencyOf(x) || "EGP";
    let priceMin = W, priceMax = F;
    if (displayCurrency && referenceCurrency && displayCurrency.toUpperCase() !== referenceCurrency
        && (W != null || F != null)) {
      try {
        const [cMin, cMax] = await Promise.all([
          W != null ? convertCurrency(W, displayCurrency, referenceCurrency) : null,
          F != null ? convertCurrency(F, displayCurrency, referenceCurrency) : null,
        ]);
        // Half-converted pairs mix units, so both bounds go or neither does.
        if (W != null && cMin?.amount == null || F != null && cMax?.amount == null)
          priceMin = priceMax = null;
        else {
          priceMin = cMin?.amount ?? null;
          priceMax = cMax?.amount ?? null;
        }
      } catch {
        priceMin = priceMax = null;
      }
    }
    const ht = `dealio-search:v6:${h.trim().toLowerCase()}:${x || "all"}:${f || "all"}:${W ?? ""}:${F ?? ""}:${re.current}:${de}:${displayCurrency || ""}`, Dt = sessionStorage.getItem(ht);
    Dt && Be.set("sessionId", Dt), _.current?.close();
    priceMin != null && Be.set("MinPrice", String(priceMin)), priceMax != null && Be.set("MaxPrice", String(priceMax));
    const at = new EventSource(`${API_BASE_URL}/v1/products/search/stream?${Be}`);
    _.current = at;
    const Ft = (lt) => {
      try {
        const xt = JSON.parse(lt.data);
        xt.sessionId && sessionStorage.setItem(ht, xt.sessionId);
      } catch {
      }
    };
    await new Promise((lt) => {
      let xt = false;
      const Ia = () => {
        xt || (xt = true, at.close(), _.current === at && (_.current = null), lt());
      };
      at.addEventListener("candidate", (nt) => {
        if (Ce === z.current) try {
          const Ue = JSON.parse(nt.data);
          Ft(nt);
          visibleCandidateCount.current++;
          const Pt = normalizeProductUrl(Ue.payload.productUrl);
          Y((Xe) => {
            const qt = Xe.findIndex((aa) => normalizeProductUrl(aa.productUrl) === Pt);
            if (qt < 0) return [...Xe, Ue.payload];
            const Rt = [...Xe];
            return Rt[qt] = { ...Xe[qt], ...Ue.payload, imageUrl: Ue.payload.imageUrl || Xe[qt].imageUrl, price: Ue.payload.price && Ue.payload.price > 0 ? Ue.payload.price : Xe[qt].price, currency: Ue.payload.currency || Xe[qt].currency, isVerified: Xe[qt].isVerified || Ue.payload.isVerified }, Rt;
          });
        } catch {
        }
      }), at.addEventListener("progress", Ft), at.addEventListener("sourceFailed", (nt) => {
        if (Ce === z.current) try {
          const Ue = JSON.parse(nt.data);
          if (Ft(nt), /candidate could not be verified from its product page/i.test(Ue.payload.reason)) return;
          X(o.discoverSomeFailed);
        } catch {
          X(o.discoverSomeFailed);
        }
      }), at.addEventListener("discoveryCompleted", (nt) => {
        if (Ce === z.current && visibleCandidateCount.current > 0) {
          Ft(nt);
        }
      }), at.addEventListener("completed", (nt) => {
        if (Ce === z.current) {
          try {
            const Ue = JSON.parse(nt.data);
            Ft(nt), se(Ue.payload?.hasMore === true);
          } catch {
          }
          sessionStorage.removeItem(ht), O(de), Ia();
        }
      }), at.addEventListener("failed", (nt) => {
        Ce === z.current && (Ft(nt), X(o.discoverTemporarilyUnavailable), se(false), sessionStorage.removeItem(ht), Ia());
      }), at.onerror = () => {
        Ce === z.current && !xt && at.readyState === EventSource.CLOSED && (X((nt) => nt || o.discoverTemporarilyUnavailable), Ia());
      }, at.addEventListener("progress", () => {
      });
    }), Ce === z.current && (loadMoreInFlight.current = false, le(false));
  }, [x, f, o, displayCurrency]);
  // Auto-load-more cried silently when the two streams raced: the sentinel
  // element mounts lazily with the results grid, while `completed` can set
  // hasMore either before or after it exists. A plain effect keyed on `ae`
  // skipped attaching whenever the ref was still null and never retried, so
  // the second page never loaded for the rest of the session. Both attach
  // points (element mount + every `ae` flip) read `ae` through a ref so the
  // gate uses the latest value; `syncSentinelObserver` disconnects any stale
  // observer first.
  aeRef.current = ae;
  const syncSentinelObserver = useCallback(() => {
    loadMoreObserver.current?.disconnect();
    loadMoreObserver.current = null;
    const h = $.current;
    if (!h || !aeRef.current || !c) return;
    const W = new IntersectionObserver((F) => {
      const de = F.some((Ne) => Ne.isIntersecting);
      if (!de) { sentinelLeftViewport.current = true; return; }
      if (!sentinelLeftViewport.current || loadMoreInFlight.current) return;
      sentinelLeftViewport.current = false;
      J(c, I, g, pageRef.current + 1, true);
    }, { rootMargin: "80px 0px" });
    loadMoreObserver.current = W, W.observe(h);
  }, [c, J, I, g]);
  useEffect(() => {
    syncSentinelObserver();
    return () => {
      loadMoreObserver.current?.disconnect();
      loadMoreObserver.current = null;
    };
  }, [syncSentinelObserver, ae]);
  useEffect(() => {
    c && J(c, I, g);
  }, [c, d, J, I, g]), useEffect(() => {
    let h = true;
    return apiFetch("/v1/discovery/overview").then((W) => {
      h && (je(W), be(""));
    }).catch(() => {
      h && be(o.overviewHighlightsUnavailable);
    }), apiFetch("/v1/price-history/drops/recent?size=12").then((W) => {
      if (!h) return;
      Q(W);
      const F = [...new Set(W.map((de) => de.productId))];
      Promise.all(F.map(async (de) => {
        try {
          const Ne = await apiFetch(`/v1/products/${de}`);
          return [de, Ne.primaryImage || ""];
        } catch {
          return [de, ""];
        }
      })).then((de) => {
        h && Te(Object.fromEntries(de));
      });
    }).catch(() => {
    }).finally(() => {
      h && me(false);
    }), () => {
      h = false;
    };
  }, [o]);
  const pe = useMemo(() => {
    let h = x ? A.filter((F) => F.marketCode === x) : A;
    if (he == null && T == null) return h;
    // Offers stream in their own store currency while the bounds are typed in
    // the display currency, so each priced offer is normalized with the same
    // snapshot cross rates (currency-client) instead of compared raw. Without
    // a display currency the typed numbers already mean the market's units,
    // so they compare directly. An offer whose rate pair is not loaded yet
    // stays — the server already filtered the range in market units, and this
    // pass must not hide more than that.
    const shown = (F) => {
      if (F.price == null || F.price <= 0) return false;
      if (!displayCurrency) return (he == null || F.price >= he) && (T == null || F.price <= T);
      const r = cachedCrossRate(F.currency || "", displayCurrency);
      // Rates not loaded yet: leave the offer visible rather than compare in
      // mismatched units — the server already decided the range.
      if (r == null) return true;
      const bound = F.price * r;
      return (he == null || bound >= he) && (T == null || bound <= T);
    };
    return h.filter(shown);
  }, [A, x, he, T, displayCurrency]), tt = useMemo(() => {
    const h = [...pe], W = (F, de, Ne = 1) => {
      const Ce = F.currency.localeCompare(de.currency);
      if (Ce) return Ce;
      const Be = F.price == null || F.price <= 0, ht = de.price == null || de.price <= 0;
      return Be !== ht ? Be ? 1 : -1 : Be ? 0 : Ne * (F.price - de.price);
    };
    switch (G) {
      case "price_asc":
      case "price":
        return h.sort((F, de) => W(F, de));
      case "price_desc":
        return h.sort((F, de) => W(F, de, -1));
      case "verified":
        return h.sort(W);
      case "store":
        return h.sort((F, de) => (F.storeName || "").localeCompare(de.storeName || ""));
      case "name":
        return h.sort((F, de) => (F.name || "").localeCompare(de.name || ""));
      default:
        return h;
    }
  }, [pe, G]), Ge = [{ id: "electronics", query: "Electronics", label: o.dealCategoryElectronics }, { id: "mobiles", query: "Mobiles & Tablets", label: o.dealCategoryMobiles }, { id: "computers", query: "Laptops & Computers", label: o.dealCategoryComputers }, { id: "home", query: "Home & Kitchen", label: o.dealCategoryHome }, { id: "gaming", query: "Gaming", label: o.dealCategoryGaming }, { id: "audio", query: "Audio", label: o.dealCategoryAudio }, { id: "fashion", query: "Fashion", label: o.dealCategoryFashion }, { id: "beauty", query: "Beauty & Health", label: o.dealCategoryBeauty }];
  return jsxs(Fragment, { children: [jsx("div", { className: "page-heading deals-page-heading", style: { marginBottom: 0 }, children: jsxs("div", { children: [c && jsx("button", { type: "button", className: "discover-back-button", onClick: back, children: [jsx(Icon, { name: document.documentElement.dir === "rtl" ? "chevron-right" : "chevron-left" }), o.discoverBack] }), jsx("div", { className: "eyebrow", children: c ? o.discoverSearchResultsEyebrow : o.discoverBestDealsEyebrow }), jsx("h1", { style: { margin: 0 }, children: c ? o.discoverSearchResultsHeading(c) : o.discoverBestDealsHeading }), ] }) }), !c && jsxs("div", { className: "deals-overview-container", children: [jsx("div", { className: "deals-category-bar", children: Ge.map((h) => jsxs("button", { className: "deals-cat-pill", onClick: () => p(h.query), children: [jsx("span", { children: h.label })] }, h.id)) }), jsxs("section", { className: `deals-section ${!K.length && !ee ? "deals-section--empty" : ""}`, children: [jsx("div", { className: "section-heading", children: jsx("div", { children: jsx("h2", { children: o.discoverTopPriceDrops }) }) }), K.length > 0 ? jsx("div", { className: "deal-cards-grid", children: K.map((h) => {
    const W = B[h.productId], F = h.previousPrice - h.currentPrice;
    return jsxs("article", { className: "deal-card", children: [jsxs("div", { className: "deal-card-photo-wrap", onErrorCapture: (de) => {
      de.target instanceof HTMLImageElement && (de.target.hidden = true);
    }, children: [jsxs("span", { className: "deal-badge-drop", children: ["-", h.dropPercent.toFixed(0), "% OFF"] }), W ? jsx(ProductImage, { source: W, productId: h.productId, alt: h.productName, layered: true }) : null] }), jsxs("div", { className: "deal-card-body", children: [jsxs("div", { className: "deal-card-meta", children: [jsx("span", { children: h.storeName }), " \xB7 ", jsx("span", { children: h.currencyCode })] }), jsx("button", { className: "deal-card-title-link", onClick: () => S(h.productId), children: h.productName }), jsxs("div", { className: "deal-card-pricing", children: [jsxs("div", { className: "deal-price-row", children: [jsx(Price, { className: "deal-current-price", amount: h.currentPrice, currencyCode: h.currencyCode }), jsx("del", { className: "deal-old-price", children: jsx(Price, { amount: h.previousPrice, currencyCode: h.currencyCode }) })] }), F > 0 && jsxs("div", { className: "deal-savings", children: [o.discoverSave, " ", jsx(Price, { amount: F, currencyCode: h.currencyCode })] })] }), jsxs("div", { className: "deal-card-actions", children: [jsxs("button", { className: "button button--tonal button--small", onClick: () => S(h.productId), children: [o.discoverViewDeal, " ", jsx(Icon, { name: "arrow" })] }), jsx("button", { className: `button ${U.includes(h.productId) ? "button--primary" : "button--quiet"} button--small`, onClick: () => v(h.productId), title: o.discoverCompare, children: U.includes(h.productId) ? o.discoverCompared : o.discoverCompare })] })] })] }, h.listingId);
  }) }) : ee ? jsx(DealCardSkeletons, { count: 6 }) : jsx(EmptyState, { className: "deals-empty-state", title: o.discoverNoRecentDrops, body: o.discoverNoRecentDropsBody })] }), xe && jsx(DiscoveryOverview, { overview: xe, onOpen: S }), te && jsx("p", { className: "verification-note", role: "status", children: te })] }), c && jsxs(Fragment, { children: [jsxs("div", { className: "toolbar discover-toolbar", children: [jsxs("label", { className: "sort-control", children: [jsx("span", { className: "sort-control-label", children: o.discoverSortLabel }), jsx(Dropdown, { className: "filter-dropdown", ariaLabel: o.discoverSortLabel, value: G, options: [{ value: "relevance", label: o.discoverSortRelevance }, { value: "price_asc", label: o.discoverSortPriceAsc }, { value: "price_desc", label: o.discoverSortPriceDesc }, { value: "store", label: o.discoverSortStore }, { value: "name", label: o.discoverSortName }], onChange: Z })] }), jsxs("div", { className: "discover-price-filter", role: "group", "aria-label": o.discoverPriceFilter, children: [jsxs("span", { className: "discover-price-label", children: [o.discoverPriceFilter, displayCurrency ? ` \xB7 ${displayCurrency}` : x ? ` \xB7 ${marketCurrencyOf(x)}` : ` \xB7 ${o.discoverStoreCurrencyLabel}`] }), jsxs("div", { className: "discover-price-row", children: [jsx("input", { type: "number", min: "0", step: "0.01", inputMode: "decimal", placeholder: "Min", value: he ?? "", onChange: (h) => ne(h.target.value === "" ? void 0 : Math.max(0, Number(h.target.value))), className: "price-number-input", "aria-label": o.discoverMinPrice }), jsx("span", { className: "price-sep", children: "\u2014" }), jsx("input", { type: "number", min: "0", step: "0.01", inputMode: "decimal", placeholder: "Max", value: T ?? "", onChange: (h) => H(h.target.value === "" ? void 0 : Math.max(0, Number(h.target.value))), className: "price-number-input", "aria-label": o.discoverMaxPrice }), (he != null || T != null) && jsx("button", { type: "button", className: "clear-price-btn", onClick: () => {
    ne(void 0), H(void 0);
  }, children: o.discoverClearPrice })] })] })] }), w && jsx(LoadingIndicator, { className: "search-loading-indicator--top", label: A.length ? o.discoverSearching : o.discoverSearchingStores }), w && A.length === 0 && jsx(DealCardSkeletons, { count: Math.ceil(12 / Math.max(1, gridColumns)) * gridColumns, gridRef: attachSearchGrid }), tt.length > 0 ? jsxs("div", { className: "deal-cards-grid search-cards-grid", ref: attachSearchGrid, children: [tt.map((h, W) => {
    const F = asUuid(h.productId) || asUuid(h.productUrl), de = h.country || h.marketCode || o.discoverStore;
    return jsxs("article", { className: "deal-card search-card", children: [jsxs("div", { className: "deal-card-photo-wrap", onErrorCapture: (Ne) => {
      Ne.target instanceof HTMLImageElement && (Ne.target.hidden = true);
    }, children: [jsx(ProductImage, { source: h.imageUrl, baseUrl: h.productUrl, productId: h.productId, alt: h.name, layered: true, loading: "lazy" })] }), jsxs("div", { className: "deal-card-body", children: [jsxs("div", { className: "deal-card-meta", children: [jsx("span", { children: h.storeName }), " \xB7 ", jsx("span", { children: de }), " \xB7 ", jsx("span", { className: "stock-state", children: h.inStock === true ? o.discoverInStock : h.inStock === false ? o.discoverOutOfStock : null })] }), jsx("button", { className: "deal-card-title-link", onClick: () => S(h), children: h.name }), jsx("div", { className: "deal-card-pricing", children: h.price != null && h.price > 0 ? jsxs("div", { className: "deal-price-row", children: [jsx(Price, { className: "deal-current-price", amount: h.price, currencyCode: h.currency })] }) : jsx("strong", { className: "deal-current-price", children: "—" }) }), jsxs("div", { className: "deal-card-actions", children: [jsx("button", { className: "button button--primary button--small", disabled: !(h.price > 0) || !h.currency, onClick: () => N(h), children: o.discoverTrackPrice }), F && jsx("button", { className: `button ${U.includes(F) ? "button--tonal" : "button--quiet"} button--small`, onClick: () => v(F), children: U.includes(F) ? o.discoverCompared : o.discoverCompare }), jsxs("a", { className: "quiet-link", href: h.productUrl, target: "_blank", rel: "noreferrer", children: [o.discoverStore, " ", jsx(Icon, { name: "external" })] })] })] })] }, `${F || normalizeProductUrl(h.productUrl) || W}`);
  }), (w || ae) && Array.from({ length: Math.ceil((tt.length + (tt.length < 12 ? 12 - tt.length : 3)) / gridColumns) * gridColumns - tt.length }).map((F, de) => jsx(DealCardSkeleton, { index: tt.length + de }, `skel-${tt.length + de}`)), ae && jsxs("div", { ref: (h) => { $.current = h; syncSentinelObserver(); }, className: "search-load-more", "aria-live": "polite", children: [jsx(LoadingIndicator, { className: "search-loading-indicator--load-more", label: o.discoverStillSearching })] })] }) : !w && (M ? jsx(EmptyState, { className: "discover-search-error", title: M, body: o.discoverSearchRetryMessage, action: jsx("button", { type: "button", className: "button button--quiet", onClick: () => J(c, I, g), children: o.discoverTryAgain }) }) : jsx(EmptyState, { title: o.discoverNoResults, body: o.discoverNoResultsBody }))] })] });
}
