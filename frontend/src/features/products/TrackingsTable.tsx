// @ts-nocheck
import { DisplayCurrencyContext } from "../../lib/i18n-context";
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { Icon } from "../../components/ui/Icon";
import { Price } from "../../components/ui/Price";
import { apiFetch } from "../../lib/api";
import { useContext, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function TrackingsTable({ items: c, onOpen: f, onRefresh: x, notify: d, selectedIds: p = [], onSelect: N, onSelectAll: v, sortKey: U, sortDirection: S, onSort: o, onCompare: A, compareIds: Y = [] }) {
  const w = useLocale(), le = useContext(DisplayCurrencyContext), [ae, se] = useState(null);
  async function ie(z, $) {
    se(z.trackingId);
    try {
      $ === "remove" ? await apiFetch(`/v1/tracking/${z.trackingId}`, { method: "DELETE" }) : await apiFetch(`/v1/tracking/${z.trackingId}/${$}`, { method: "PATCH" }), d($ === "remove" ? w.trackingRemoved : $ === "activate" ? w.trackingResumed : w.trackingPaused), x?.();
    } catch (_) {
      d(_ instanceof Error ? _.message : w.actionFailed);
    } finally {
      se(null);
    }
  }
  const O = (z, $) => o ? jsxs("button", { className: "table-sort", type: "button", onClick: () => o($), "aria-label": w.sortBy(z), "aria-pressed": U === $, children: [z, U === $ ? jsx("span", { "aria-hidden": "true", children: S === "asc" ? " \u2191" : " \u2193" }) : null] }) : z;
  return jsx("div", { className: "table-wrap", children: jsxs("table", { children: [jsx("thead", { children: jsxs("tr", { children: [N && jsx("th", { children: jsx("input", { type: "checkbox", "aria-label": w.myProductsSelectAll, checked: c.length > 0 && c.every((z) => p.includes(z.trackingId)), onChange: (z) => v?.(z.target.checked) }) }), jsx("th", { children: O(w.colProduct, "productName") }), jsx("th", { children: w.colStore }), jsx("th", { children: O(w.colCurrentPrice, "currentPrice") }), jsx("th", { children: O(w.colTargetPrice, "targetPrice") }), jsx("th", { children: O(w.colDifference, "difference") }), jsx("th", { children: O(w.colStatus, "isActive") }), jsx("th", { children: jsx("span", { className: "sr-only", children: w.colActions }) })] }) }), jsx("tbody", { children: c.map((z) => {
    const $ = z.currentPriceInDisplayCurrency == null || z.targetPriceInDisplayCurrency == null ? null : z.currentPriceInDisplayCurrency - z.targetPriceInDisplayCurrency, _ = Y.includes(z.productId);
    return jsxs("tr", { children: [N && jsx("td", { children: jsx("input", { type: "checkbox", "aria-label": w.myProductsSelectLabel(z.productName), checked: p.includes(z.trackingId), onChange: (M) => N(z.trackingId, M.target.checked) }) }), jsx("td", { children: jsx("button", { className: "product-link", onClick: () => f(z.productId), children: z.productName }) }), jsx("td", { children: z.storeName || "\u2014" }), jsx("td", { className: "price-cell", children: jsx(Price, { amount: z.currentPriceInDisplayCurrency, currencyCode: le }) }), jsx("td", { children: jsx(Price, { amount: z.targetPriceInDisplayCurrency, currencyCode: le }) }), jsx("td", { children: $ == null ? "\u2014" : jsx("span", { className: $ <= 0 ? "delta-down" : "delta-neutral", children: $ <= 0 ? w.atTarget : jsxs(Fragment, { children: ["+", jsx(Price, { amount: $, currencyCode: le })] }) }) }), jsx("td", { children: jsxs("span", { className: `status ${z.isActive ? "status--active" : "status--paused"}`, children: [jsx("i", {}), z.isActive ? w.statusTracking : w.statusPaused] }) }), jsxs("td", { className: "row-actions", children: [A && jsx("button", { className: "text-button", type: "button", onClick: () => A(z.productId), children: _ ? w.discoverRemoveCompare : w.discoverAddCompare }), jsx("button", { className: "icon-button icon-button--small", disabled: ae === z.trackingId, "aria-label": z.isActive ? w.pauseTracking : w.resumeTracking, onClick: () => ie(z, z.isActive ? "deactivate" : "activate"), children: jsx(Icon, { name: z.isActive ? "pause" : "play" }) }), jsx("button", { className: "icon-button icon-button--small danger-action", disabled: ae === z.trackingId, "aria-label": w.removeTracking, onClick: () => {
      confirm(w.removeTrackingConfirm) && ie(z, "remove");
    }, children: jsx(Icon, { name: "trash" }) })] })] }, z.trackingId);
  }) })] }) });
}
