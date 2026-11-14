// @ts-nocheck
import { DisplayCurrencyContext } from "../../lib/i18n-context";
import { Icon } from "../../components/ui/Icon";
import { LoadingIndicator } from "../../components/ui/LoadingIndicator";
import { Price } from "../../components/ui/Price";
import { ProductImage } from "../../components/ui/ProductImage";
import { apiFetch } from "../../lib/api";
import { convertCurrency } from "../../lib/currency-client";
import { jsx, jsxs } from "react/jsx-runtime";
import { useContext, useEffect, useRef, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function TrackPriceDialog({ item: c, token: f, busy: x, suspended: d, onClose: p, onSignIn: N, notify: v }) {
  const U = useLocale(), S = useContext(DisplayCurrencyContext), [o, A] = useState(""), [le, ae] = useState(false), [se, ie] = useState(true), [O, z] = useState(true), $ = useRef(false);
  useEffect(() => {
    let ne = true;
    return $.current = false, c.price == null || c.price <= 0 || !/^[A-Za-z]{3}$/.test(c.currency) ? (A(""), () => {
      ne = false;
    }) : c.currency.toUpperCase() === S ? (A(String(Math.max(1, Math.round(c.price)))), () => {
      ne = false;
    }) : (A(""), convertCurrency(c.price, c.currency, S).then((T) => {
      ne && !$.current && A(T ? String(Math.max(1, Math.round(T.amount))) : "");
    }).catch(() => {
      ne && !$.current && A("");
    }), () => {
      ne = false;
    });
  }, [c.price, c.currency, S]);
  const _ = 9999999999, M = /^[A-Za-z]{3}$/.test(c.currency), X = U.previewPriceLabel2(S), G = useRef(void 0), Z = useRef(void 0), re = useRef(false), xe = (ne) => {
    $.current = true, A((T) => {
      const H = Number(T), ue = (Number.isFinite(H) && H > 0 ? H : ne > 0 ? 0 : 2) + ne;
      return String(Math.max(1, Math.min(_, ue)));
    });
  }, je = (ne) => (T) => {
    T.preventDefault(), re.current = true, xe(ne), G.current = window.setTimeout(() => {
      Z.current = window.setInterval(() => xe(ne), 70);
    }, 280);
  }, te = () => {
    G.current !== void 0 && window.clearTimeout(G.current), Z.current !== void 0 && window.clearInterval(Z.current), G.current = void 0, Z.current = void 0, window.setTimeout(() => {
      re.current = false;
    }, 0);
  }, be = (ne) => {
    if (re.current) {
      re.current = false;
      return;
    }
    xe(ne);
  };
  async function he(ne) {
    if (ne.preventDefault(), !f) {
      p();
      N();
      return;
    }
    const T = Number(o);
    if (!Number.isFinite(T) || T <= 0 || T > _) {
      v(U.previewInvalidPrice);
      return;
    }
    if (!c.submittedUrl && !c.existingProductId) {
      v(U.previewNoUrl);
      return;
    }
    ae(true);
    try {
      const H = c.existingProductId ? await apiFetch(`/v1/products/${c.existingProductId}`) : await apiFetch("/v1/products/import-url", { method: "POST", body: JSON.stringify({ url: c.submittedUrl }) }), ue = (await apiFetch(`/v1/listings/by-product/${H.productId}`)).filter((ee) => ee.isActive && /^[A-Za-z]{3}$/.test(ee.currencyCode || "")), g = c.submittedUrl ? new URL(c.submittedUrl).hostname.toLowerCase() : "", k = (ee) => ee.storeName.toLowerCase() === c.storeName.toLowerCase() || !!g && ee.productUrl.toLowerCase().includes(g), K = M ? ue.find((ee) => ee.currencyCode?.toUpperCase() === c.currency.toUpperCase() && k(ee)) || ue.find((ee) => ee.currencyCode?.toUpperCase() === c.currency.toUpperCase()) : ue.find(k) || (ue.length === 1 ? ue[0] : void 0);
      if (!K) throw new Error(U.previewNoOffer);
      const Q = K.currencyCode?.toUpperCase();
      if (!Q || !/^[A-Za-z]{3}$/.test(Q)) {
        v(U.previewInvalidCurrency);
        return;
      }
      await apiFetch("/v1/tracking", { method: "POST", body: JSON.stringify({ productId: H.productId, listingId: K.listingId, targetPrice: T, currencyCode: S, notifyEmail: se, notifyFurtherDecreaseEmail: O }) }), v(U.previewTrackingStarted), p();
    } catch {
      v(U.requestFailed);
    } finally {
      ae(false);
    }
  }
  return jsx("div", { className: "scrim", role: "presentation", "aria-hidden": d || void 0, onMouseDown: (ne) => {
    ne.target === ne.currentTarget && p();
  }, children: jsxs("section", { className: "dialog", role: "dialog", "aria-modal": d ? void 0 : true, "aria-labelledby": "preview-title", children: [jsxs("div", { className: "dialog-head", children: [jsxs("div", { children: [jsx("div", { className: "eyebrow", children: U.previewEyebrow }), jsx("h2", { id: "preview-title", children: U.previewHeading })] }), jsx("button", { className: "icon-button", onClick: p, "aria-label": U.close, children: jsx(Icon, { name: "close" }) })] }), jsxs("div", { className: "preview-product", children: [jsx("div", { className: "preview-photo", children: jsx(ProductImage, { source: c.imageUrl, baseUrl: c.productUrl, alt: "", loading: "eager" }) }), jsxs("div", { children: [jsx("h3", { children: c.name }), jsxs("p", { children: [c.storeName, M ? ` \xB7 ${U.previewListedIn(c.currency)}` : ""] }), c.price != null && c.price > 0 && jsx(Price, { amount: c.price, currencyCode: c.currency })] })] }), x && jsx(LoadingIndicator, { className: "search-loading-indicator--inline", label: v.productDetailsLoading }), jsxs("form", { onSubmit: he, children: [jsx("label", { className: "field-label", htmlFor: "target-price", children: X }), jsxs("div", { className: "price-stepper", role: "group", "aria-label": X, children: [jsx("button", { className: "price-stepper-button price-stepper-button--minus", type: "button", "aria-label": U.previewDecreaseTarget, disabled: Number(o) <= 1 || x, onPointerDown: je(-1), onPointerUp: te, onPointerLeave: te, onPointerCancel: te, onClick: () => be(-1), children: "\u2212" }), jsx("input", { className: "price-stepper-input", id: "target-price", type: "text", inputMode: "decimal", value: o, "aria-label": X, disabled: x, onChange: (ne) => {
    $.current = true;
    const T = ne.target.value.replace(/,/g, ".").replace(/[^\d.]/g, "").replace(/(\..*)\./g, "$1");
    A(T.slice(0, 13));
  }, onBlur: () => {
    const ne = Number(o);
    Number.isFinite(ne) && ne > 0 ? A(String(Math.min(_, Math.max(1, ne)))) : A("");
  } }), jsx("button", { className: "price-stepper-button price-stepper-button--plus", type: "button", "aria-label": U.previewIncreaseTarget, disabled: Number(o) >= _ || x, onPointerDown: je(1), onPointerUp: te, onPointerLeave: te, onPointerCancel: te, onClick: () => be(1), children: "+" })] }), jsxs("fieldset", { className: "email-preferences", children: [jsx("legend", { children: U.previewEmailPreferences }), jsxs("label", { children: [jsx("input", { type: "checkbox", checked: se, onChange: (ne) => ie(ne.target.checked) }), " ", U.previewEmailTarget] }), jsxs("label", { children: [jsx("input", { type: "checkbox", checked: O, onChange: (ne) => z(ne.target.checked) }), " ", U.previewEmailFurther] })] }), jsx("p", { className: "dialog-help", children: U.previewHelp }), jsxs("div", { className: "dialog-actions", children: [jsx("button", { className: "button button--quiet", type: "button", onClick: p, children: U.previewCancel }), jsx("button", { className: "button button--primary", type: "submit", disabled: le || x, children: x ? U.loading : le ? U.previewSaving : f ? U.previewConfirm : U.previewSignInToTrack })] })] })] }) });
}
