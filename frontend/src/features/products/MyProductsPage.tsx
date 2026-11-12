// @ts-nocheck
import { DisplayCurrencyContext } from "../../lib/i18n-context";
import { Dropdown } from "../../components/ui/Dropdown";
import { EmptyState } from "../../components/ui/EmptyState";
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { GuestPage } from "../../components/ui/GuestPage";
import { Icon } from "../../components/ui/Icon";
import { LoadingIndicator } from "../../components/ui/LoadingIndicator";
import { MARKETS } from "../../lib/markets";
import { Price } from "../../components/ui/Price";
import { SkeletonTable } from "../../components/ui/skeletons";
import { TrackingCardSkeletons } from "../../components/ui/skeletons";
import { TrackingsTable } from "./TrackingsTable";
import { apiFetch } from "../../lib/api";
import { fieldToSortChoice } from "../../lib/sorting";
import { filterAndSortTrackings } from "../../lib/sorting";
import { localizeCategoryName } from "../../lib/i18n-context";
import { sortChoiceConfigs } from "../../lib/sorting";
import { useCallback, useContext, useEffect, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function MyProductsPage({ token: c, onOpen: f, onAddProduct: x, onPreview: d, onCompare: p, compareIds: N, notify: v, onSignIn: U, categories: categoryOptions = [], markets: marketOptions = MARKETS, locale = "en" }) {
  const S = useLocale(), o = useContext(DisplayCurrencyContext), [A, Y] = useState([]), [w, le] = useState(!!c), [ae, se] = useState(false), [ie, O] = useState(""), [z, $] = useState(""), [_, M] = useState("all"), [X, G] = useState("all"), [Z, re] = useState("all"), [xe, je] = useState("all"), [te, be] = useState(localStorage.getItem("pt-product-view") || "table"), [he, ne] = useState({ key: "productName", direction: "asc" }), [T, H] = useState([]), [I, ue] = useState(false), g = useCallback(() => {
    c && (le(true), apiFetch(`/v1/tracking?displayCurrency=${encodeURIComponent(o)}`).then((B) => {
      Y(B), O("");
    }).catch((B) => {
      O(B instanceof Error ? B.message : S.myProductsLoadError), v(B.message);
    }).finally(() => le(false)));
  }, [c, v, o]);
  useEffect(() => {
    g();
  }, [g]), useEffect(() => {
    if (!z.trim()) {
      se(false);
      return;
    }
    se(true);
    const B = window.setTimeout(() => se(false), 160);
    return () => window.clearTimeout(B);
  }, [z]);
  useEffect(() => {
    const B = () => g();
    return window.addEventListener("pt-tracking-changed", B), () => window.removeEventListener("pt-tracking-changed", B);
  }, [g]);
  const k = [...new Set([...A.map((B) => B.categoryName), ...categoryOptions.map((B) => B.name)].filter((B) => !!B))].sort((B, Te) => B.localeCompare(Te)), K = [...new Set([...A.map((B) => B.marketCountry), ...marketOptions.map((B) => B.name)].filter((B) => !!B))].sort((B, Te) => B.localeCompare(Te)), Q = filterAndSortTrackings(A, { status: _, query: z, category: X, market: Z, price: xe }, he);
  function ee(B) {
    ne((Te) => Te.key === B ? { key: B, direction: Te.direction === "asc" ? "desc" : "asc" } : { key: B, direction: B === "productName" ? "asc" : "desc" });
  }
  async function me(B) {
    if (!T.length || B === "remove" && !confirm(`Remove tracking for ${T.length} selected product(s)? Their price history will be retained.`)) return;
    ue(true);
    const J = (await Promise.allSettled(T.map((pe) => apiFetch(`/v1/tracking/${pe}${B === "remove" ? "" : `/${B}`}`, { method: B === "remove" ? "DELETE" : "PATCH" })))).filter((pe) => pe.status === "rejected").length;
    v(J ? `${J} selected action(s) failed.` : B === "remove" ? S.myProductsBulkRemovedMsg : S.myProductsBulkUpdatedMsg), H([]), await g(), ue(false);
  }
  return c ? jsxs(Fragment, { children: [jsxs("div", { className: "page-heading my-products-heading", children: [jsxs("div", { className: "my-products-heading-copy", children: [jsx("div", { className: "eyebrow", children: S.myProductsEyebrow }), jsx("h1", { children: S.myProductsHeading })] }), jsxs("button", { className: "button button--primary", onClick: x, children: [" ", jsx(Icon, { name: "plus" }), S.myProductsAddProduct] })] }), jsxs("div", { className: "toolbar my-products-toolbar", children: [jsxs("label", { className: "table-search", children: [jsx(Icon, { name: "search" }), jsx("input", { value: z, onChange: (B) => $(B.target.value), onKeyDown: (B) => {
    B.key === "Enter" && /^https?:\/\//i.test(z.trim()) && d(z.trim());
  }, placeholder: S.myProductsSearchPlaceholder, "aria-label": S.myProductsSearchLabel })] }), ae && jsx(LoadingIndicator, { className: "search-loading-indicator--inline", label: S.myProductsFiltering }), jsxs("label", { className: "sort-control", children: [jsx("span", { className: "sort-control-label", children: S.myProductsSortBy }), jsx(Dropdown, { className: "filter-dropdown", ariaLabel: S.myProductsSortBy, value: fieldToSortChoice[he.key], options: [{ value: "name", label: S.myProductsSortProductName }, { value: "lowest", label: S.myProductsSortLowestPrice }, { value: "target", label: S.myProductsSortTargetPrice }, { value: "change", label: S.myProductsSortPriceChange }, { value: "drop", label: S.myProductsSortBiggestDrop }, { value: "proximity", label: S.myProductsSortTargetProximity }, { value: "updated", label: S.myProductsSortLastUpdated }, { value: "status", label: S.myProductsSortTrackingStatus }], onChange: (B) => ne(sortChoiceConfigs[B]) })] }), jsxs("label", { className: "sort-control", children: [jsx("span", { className: "sort-control-label", children: S.myProductsStatusFilter }), jsx(Dropdown, { className: "filter-dropdown", ariaLabel: S.myProductsStatusFilter, value: _, options: [{ value: "all", label: S.myProductsAllStatuses }, { value: "active", label: S.statusTracking }, { value: "paused", label: S.statusPaused }], onChange: M })] }), jsxs("label", { className: "sort-control", children: [jsx("span", { className: "sort-control-label", children: S.myProductsCategoryFilter }), jsx(Dropdown, { className: "filter-dropdown", ariaLabel: S.myProductsCategoryFilter, value: X, options: [{ value: "all", label: S.myProductsAllCategories }, ...k.map((B) => ({ value: B, label: localizeCategoryName(B, S) }))], onChange: G })] }), jsxs("label", { className: "sort-control", children: [jsx("span", { className: "sort-control-label", children: S.myProductsMarketFilter }), jsx(Dropdown, { className: "filter-dropdown", ariaLabel: S.myProductsMarketFilter, value: Z, options: [{ value: "all", label: S.myProductsAllMarkets }, ...K.map((B) => ({ value: B, label: locale === "ar" ? marketOptions.find((Te) => Te.name === B)?.nameAr || B : B }))], onChange: re })] }), jsxs("label", { className: "sort-control", children: [jsx("span", { className: "sort-control-label", children: S.myProductsPriceFilter }), jsx(Dropdown, { className: "filter-dropdown", ariaLabel: S.myProductsPriceFilter, value: xe, options: [{ value: "all", label: S.myProductsAnyPrice }, { value: "reached", label: S.myProductsAtOrBelowTarget }, { value: "near", label: S.myProductsNearTarget }, { value: "above", label: S.myProductsAboveTarget }, { value: "missing", label: S.myProductsPriceUnavailable }], onChange: je })] }), jsxs("div", { className: "view-mode-switch", role: "group", "aria-label": S.myProductsViewLabel, children: [jsxs("button", { type: "button", className: te === "table" ? "is-selected" : "", "aria-pressed": te === "table", onClick: () => {
    be("table"), localStorage.setItem("pt-product-view", "table");
  }, children: [jsx(Icon, { name: "list" }), jsx("span", { children: S.myProductsTable })] }), jsxs("button", { type: "button", className: te === "cards" ? "is-selected" : "", "aria-pressed": te === "cards", onClick: () => {
    be("cards"), localStorage.setItem("pt-product-view", "cards");
  }, children: [jsx(Icon, { name: "grid" }), jsx("span", { children: S.myProductsCards })] })] })] }), T.length > 0 && jsxs("div", { className: "toolbar bulk-actions", "aria-live": "polite", children: [jsx("span", { children: S.myProductsSelected(T.length) }), jsx("button", { className: "button button--tonal button--small", disabled: I, onClick: () => {
    me("activate");
  }, children: S.myProductsResume }), jsx("button", { className: "button button--tonal button--small", disabled: I, onClick: () => {
    me("deactivate");
  }, children: S.myProductsPause }), jsx("button", { className: "button button--quiet button--small", disabled: I, onClick: () => {
    me("remove");
  }, children: S.myProductsRemoveSelected })] }), w ? te === "table" ? jsx(SkeletonTable, { rows: 5 }) : jsx(TrackingCardSkeletons, { count: 6 }) : ie ? jsx(EmptyState, { title: S.myProductsLoadError, body: ie, action: jsx("button", { className: "button button--quiet", onClick: () => {
    g();
  }, children: S.tryAgain }) }) : Q.length ? te === "table" ? jsx(TrackingsTable, { items: Q, onOpen: f, onRefresh: g, notify: v, selectedIds: T, onSelect: (B, Te) => H((J) => Te ? [.../* @__PURE__ */ new Set([...J, B])] : J.filter((pe) => pe !== B)), onSelectAll: (B) => H(B ? Q.map((Te) => Te.trackingId) : []), sortKey: he.key, sortDirection: he.direction, onSort: ee, onCompare: p, compareIds: N }) : jsx("div", { className: "tracking-cards", children: Q.map((B) => jsxs("article", { className: "tracking-card", children: [jsxs("label", { className: "check-filter", children: [jsx("input", { type: "checkbox", checked: T.includes(B.trackingId), onChange: (Te) => H((J) => Te.target.checked ? [.../* @__PURE__ */ new Set([...J, B.trackingId])] : J.filter((pe) => pe !== B.trackingId)) }), S.myProductsSelectLabel(B.productName)] }), jsxs("span", { className: `status ${B.isActive ? "status--active" : "status--paused"}`, children: [jsx("i", {}), B.isActive ? S.statusTracking : S.statusPaused] }), jsx("button", { className: "product-link", onClick: () => f(B.productId), children: B.productName }), jsxs("small", { children: [B.storeName || "\u2014", B.marketCountry ? ` \xB7 ${B.marketCountry}` : ""] }), jsx(Price, { amount: B.currentPrice, currencyCode: B.currentPriceCurrencyCode || B.currencyCode }), jsxs("span", { children: [S.myProductsSortTargetPrice + " ", jsx(Price, { amount: B.targetPrice, currencyCode: B.currencyCode })] }), jsxs("div", { className: "tracking-card-actions", children: [jsx("button", { className: "text-button", onClick: () => p(B.productId), children: N.includes(B.productId) ? S.discoverRemoveCompare : S.discoverAddCompare }), jsxs("button", { className: "text-button", onClick: () => f(B.productId), children: [S.myProductsViewProduct, " ", jsx(Icon, { name: "arrow" })] })] })] }, B.trackingId)) }) : jsx(EmptyState, { title: A.length ? S.myProductsNoMatch : S.myProductsEmpty, body: A.length ? S.myProductsNoMatchBody : S.myProductsEmptyBody })] }) : jsx(GuestPage, { title: S.myProductsHeading, eyebrow: S.myProductsEyebrow, pageClass: "my-products-heading", headerAction: jsxs("button", { className: "button button--primary", onClick: x, children: [jsx(Icon, { name: "plus" }), S.myProductsAddProduct] }), body: S.guestPageAccessBody, onSignIn: U });
}
