// @ts-nocheck
import { Icon } from "../../components/ui/Icon";
import { Price } from "../../components/ui/Price";
import { ProductImage } from "../../components/ui/ProductImage";
import { apiFetch } from "../../lib/api";
import { formatDateTime } from "../../lib/format";
import { formatNumber } from "../../lib/format";
import { jsx, jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function RecentDropsSection({ drops: c, onOpen: f, onDiscover: x }) {
  const d = useLocale(), [p, N] = useState({});
  return useEffect(() => {
    let v = true;
    const U = [...new Set(c.slice(0, 3).map((S) => S.productId))];
    return Promise.all(U.map(async (S) => {
      try {
        return [S, await apiFetch(`/v1/products/${S}`)];
      } catch {
        return null;
      }
    })).then((S) => {
      v && N(Object.fromEntries(S.filter((o) => o !== null)));
    }), () => {
      v = false;
    };
  }, [c]), c.length ? jsxs("section", { className: "recent-deals", "aria-labelledby": "recent-deals-title", children: [jsxs("div", { className: "section-heading", children: [jsxs("div", { children: [jsx("div", { className: "eyebrow", children: d.dropsEyebrow }), jsx("h2", { id: "recent-deals-title", children: d.dropsHeading })] }), x && jsxs("button", { className: "text-button", onClick: x, children: [d.dropsDiscoverMore, " ", jsx(Icon, { name: "arrow" })] })] }), jsx("div", { className: "recent-deal-grid", children: c.slice(0, 3).map((v, U) => {
    const S = p[v.productId]?.primaryImage;
    return jsxs("article", { className: `recent-deal ${U === 0 ? "recent-deal--featured" : ""}`, children: [jsx("div", { className: "recent-deal-photo", children: jsx(ProductImage, { source: S, productId: v.productId, alt: v.productName, layered: true }) }), jsxs("div", { className: "recent-deal-body", children: [jsx("span", { className: "candidate-label", children: d.dropsLower(formatNumber(v.dropPercent, { maximumFractionDigits: 1 })) }), jsx("button", { className: "product-link", onClick: () => f(v.productId), children: v.productName }), jsxs("div", { className: "recent-deal-prices", children: [jsx(Price, { amount: v.currentPrice, currencyCode: v.currencyCode }), jsx("del", { children: jsx(Price, { amount: v.previousPrice, currencyCode: v.currencyCode }) }), jsxs("span", { children: ["\u2193 ", jsx(Price, { amount: v.previousPrice - v.currentPrice, currencyCode: v.currencyCode })] })] }), jsxs("p", { children: [v.storeName, " \xB7 ", v.currencyCode, " \xB7 ", formatDateTime(v.recordedAt, { month: "short", day: "numeric" })] }), jsxs("button", { className: "text-button", onClick: () => f(v.productId), children: [d.dropsViewHistory, " ", jsx(Icon, { name: "arrow" })] })] })] }, v.listingId);
  }) })] }) : null;
}
