// @ts-nocheck
import { API_BASE_URL } from "../../lib/api";
import { Icon } from "../../components/ui/Icon";
import { LoadingIndicator } from "../../components/ui/LoadingIndicator";
import { Price } from "../../components/ui/Price";
import { ProductImage } from "../../components/ui/ProductImage";
import { jsx, jsxs } from "react/jsx-runtime";
import { normalizeProductUrl } from "../../lib/api";
import { useEffect, useRef, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function GlobalSearchDialog({ market: c, categoryId: f, onClose: x, onSearch: d, onSelect: p, addMode = false, busy = false }) {
  const N = useLocale(), [v, U] = useState(""), [S, o] = useState([]), [A, Y] = useState(false), [w, le] = useState(-1), ae = useRef(null);
  useEffect(() => {
    const O = v.trim();
    if (ae.current?.close(), ae.current = null, o([]), le(-1), O.length < 2 || /^https?:\/\//i.test(O)) {
      Y(false);
      return;
    }
    let z = true, $ = null;
    const _ = `dealio-global-search:v2:${O.toLowerCase()}:${c || "all"}:${f || "all"}`, M = window.setTimeout(() => {
      Y(true);
      const X = new URLSearchParams({ Query: O, ...c ? { Market: c } : {}, ...f ? { CategoryId: f } : {} }), G = sessionStorage.getItem(_);
      G && X.set("sessionId", G), $ = new EventSource(`${API_BASE_URL}/v1/products/search/stream?${X}`), ae.current = $;
      const Z = (xe) => {
        try {
          const je = JSON.parse(xe.data);
          je.sessionId && sessionStorage.setItem(_, je.sessionId);
        } catch {
        }
      }, re = () => {
        z && ($?.close(), ae.current === $ && (ae.current = null), Y(false));
      };
      $.addEventListener("progress", Z), $.addEventListener("candidate", (xe) => {
        if (z) try {
          const je = JSON.parse(xe.data);
          Z(xe);
          const te = je.payload;
          o((be) => {
            const he = normalizeProductUrl(te.productUrl), ne = be.findIndex((H) => normalizeProductUrl(H.productUrl) === he);
            if (ne < 0) return [...be, te];
            const T = [...be];
            return T[ne] = { ...be[ne], ...te, imageUrl: te.imageUrl || be[ne].imageUrl, price: te.price && te.price > 0 ? te.price : be[ne].price, currency: te.currency || be[ne].currency, isVerified: be[ne].isVerified || te.isVerified }, T;
          });
        } catch {
        }
      }), $.addEventListener("completed", (xe) => {
        Z(xe), sessionStorage.removeItem(_), re();
      }), $.addEventListener("failed", (xe) => {
        Z(xe), sessionStorage.removeItem(_), re();
      }), $.onerror = () => {
        $?.readyState === EventSource.CLOSED && re();
      };
    }, 80);
    return () => {
      z = false, window.clearTimeout(M), $?.close(), ae.current === $ && (ae.current = null);
    };
  }, [v, c, f]);
  const se = (O) => {
    O.preventDefault(), v.trim() && d(v.trim());
  }, ie = async (O) => {
    if (busy) return;
    const selected = await p(O);
    if (selected !== false) x();
  };
  return jsx("div", { className: "scrim scrim--search", role: "presentation", onMouseDown: (O) => {
    O.target === O.currentTarget && x();
  }, children: jsxs("form", { className: "search-overlay", onSubmit: se, role: "dialog", "aria-modal": "true", "aria-label": N.globalSearchLabel, children: [jsxs("div", { className: "search-entry", children: [jsx(Icon, { name: "search" }), jsx("input", { autoFocus: true, value: v, onChange: (O) => {
    U(O.target.value), le(-1);
  }, onKeyDown: (O) => {
    O.key === "ArrowDown" && S.length ? (O.preventDefault(), le((z) => (z + 1) % S.length)) : O.key === "ArrowUp" && S.length ? (O.preventDefault(), le((z) => z <= 0 ? S.length - 1 : z - 1)) : O.key === "Enter" && w >= 0 && S[w] && (O.preventDefault(), ie(S[w]));
  }, placeholder: N.globalSearchPlaceholder, "aria-label": N.globalSearchLabel, role: "combobox", "aria-autocomplete": "list", "aria-expanded": S.length > 0, "aria-controls": "global-search-suggestions", "aria-activedescendant": w >= 0 ? `global-search-suggestion-${w}` : void 0 }), jsx("kbd", { "aria-hidden": "true", children: "ESC" })] }), A && jsx(LoadingIndicator, { className: "search-loading-indicator--inline", label: N.discoverSearching }), busy && jsx(LoadingIndicator, { className: "search-loading-indicator--inline", label: N.productDetailsLoading }), S.length > 0 && jsx("ul", { className: `search-suggestions ${addMode ? "search-suggestions--collection" : ""}`, id: "global-search-suggestions", role: "listbox", children: S.map((O, z) => jsx("li", { children: jsxs("button", { id: `global-search-suggestion-${z}`, type: "button", role: "option", disabled: busy, "aria-selected": w === z, onMouseEnter: () => le(z), onClick: () => ie(O), children: [jsx(ProductImage, { source: O.imageUrl, baseUrl: O.productUrl, alt: "" }), jsxs("span", { children: [jsx("strong", { children: O.name }), jsxs("small", { children: [O.storeName] })] }), jsx("b", { children: jsx(Price, { amount: O.price, currencyCode: O.currency }) }), addMode && jsx("span", { className: "search-result-add", children: N.myProductsAddProduct })] }) }, `${O.productUrl}-${z}`)) }), jsx("button", { className: "sr-only", type: "submit", children: N.globalSearchLabel })] }) });
}
