// @ts-nocheck
import { Dropdown } from "../../components/ui/Dropdown";
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { apiFetch } from "../../lib/api";
import { formatDisplayValue } from "../../lib/format";
import { formatShortDateOrDash } from "../../lib/format";
import { useEffect, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function CompareHistoryChart({ listings: c, currencyCode: f }) {
  const x = useLocale(), [d, p] = useState({}), [N, v] = useState([]), [U, S] = useState("30"), [o, A] = useState(false), [Y, w] = useState(false);
  useEffect(() => {
    let Z = true;
    if (!c.length) {
      p({}), w(false);
      return;
    }
    A(true), w(false);
    const re = U === "all" ? void 0 : new Date(Date.now() - Number(U) * 864e5).toISOString();
    let xe = false;
    return Promise.all(c.map(async (je) => {
      const te = (be) => {
        const he = new URLSearchParams({ Page: String(be), Size: "200" });
        return re && he.set("From", re), he;
      };
      try {
        const be = await apiFetch(`/v1/price-history/by-listing/${je.listingId}?${te(0)}`), he = Math.ceil(be.totalElements / 200), ne = he > 1 ? [.../* @__PURE__ */ new Set([0, Math.floor((he - 1) / 2), he - 1])] : [0], T = await Promise.all(ne.filter((I) => I > 0).map((I) => apiFetch(`/v1/price-history/by-listing/${je.listingId}?${te(I)}`).then((ue) => ue.content).catch(() => [])));
        he > 3 && (xe = true);
        const H = [be.content, ...T].flat();
        return [je.listingId, [...new Map(H.map((I) => [I.id, I])).values()].filter((I) => I.currencyCode === f).sort((I, ue) => +new Date(I.recordedAt) - +new Date(ue.recordedAt))];
      } catch {
        return [je.listingId, []];
      }
    })).then((je) => {
      Z && (p(Object.fromEntries(je)), w(xe));
    }).finally(() => {
      Z && A(false);
    }), () => {
      Z = false;
    };
  }, [c, U, f]);
  const le = c.filter((Z) => !N.includes(Z.listingId)), ae = le.flatMap((Z) => (d[Z.listingId] || []).map((re) => ({ ...re, listingId: Z.listingId, storeName: Z.storeName }))).sort((Z, re) => +new Date(Z.recordedAt) - +new Date(re.recordedAt)), se = ae.map((Z) => Z.price), ie = se.length ? Math.min(...se) : 0, O = se.length ? Math.max(...se) : 0, z = ae.length ? +new Date(ae[0].recordedAt) : 0, $ = ae.length ? +new Date(ae[ae.length - 1].recordedAt) : 0, _ = (Z) => $ === z ? 50 : (+new Date(Z.recordedAt) - z) / ($ - z) * 94 + 3, M = (Z) => O === ie ? 50 : 92 - (Z.price - ie) / (O - ie) * 84, X = ["#ff6257", "#4d91d8", "#55a878", "#f28c38"], G = (Z) => {
    const re = ae.filter((te) => te.listingId === Z), xe = re.slice(1).map((te, be) => +new Date(te.recordedAt) - +new Date(re[be].recordedAt)).filter((te) => te > 0).sort((te, be) => te - be), je = xe.length ? xe[Math.floor((xe.length - 1) / 2)] : 0;
    return re.map((te, be) => {
      const he = be > 0 && je > 0 && +new Date(te.recordedAt) - +new Date(re[be - 1].recordedAt) > je * 2.5;
      return `${be === 0 || he ? "M" : "L"} ${_(te)} ${M(te)}`;
    }).join(" ");
  };
  return jsxs("div", { className: "compare-history", children: [jsxs("div", { className: "compare-history-head", children: [jsxs("strong", { children: [x.compareHistoryHeading, " \xB7 ", f] }), jsxs("label", { children: [x.compareHistoryPeriod, jsx(Dropdown, { className: "filter-dropdown range-select", ariaLabel: x.compareHistoryPeriod, value: U, options: [{ value: "30", label: x.compareHistoryPeriod30 }, { value: "90", label: x.compareHistoryPeriod90 }, { value: "365", label: x.compareHistoryPeriod365 }, { value: "all", label: x.compareHistoryPeriodAll }], onChange: S })] })] }), c.length > 1 && jsx("div", { className: "history-store-controls", role: "group", "aria-label": x.compareHistoryStores, children: c.map((Z, re) => jsxs("label", { children: [jsx("input", { type: "checkbox", checked: !N.includes(Z.listingId), onChange: () => v((xe) => xe.includes(Z.listingId) ? xe.filter((je) => je !== Z.listingId) : [...xe, Z.listingId]) }), jsx("i", { className: `legend-dot legend-dot--${re % 4}` }), Z.storeName] }, Z.listingId)) }), Y && jsx("p", { className: "dialog-help", children: x.compareHistorySampled }), o ? jsx("div", { className: "skeleton-box", style: { height: 120 } }) : ae.length ? jsxs(Fragment, { children: [jsx("div", { className: "compare-history-plot", role: "img", "aria-label": x.compareHistoryRange(formatShortDateOrDash(ae[0].recordedAt), formatShortDateOrDash(ae[ae.length - 1].recordedAt), f), children: jsx("svg", { viewBox: "0 0 100 100", preserveAspectRatio: "none", "aria-hidden": "true", children: le.map((Z, re) => jsx("path", { d: G(Z.listingId), fill: "none", stroke: X[re % X.length], strokeWidth: "2", strokeLinecap: "round", strokeLinejoin: "round" }, Z.listingId)) }) }), jsxs("div", { className: "compare-history-scale", children: [jsx("span", { children: formatDisplayValue(O, f) }), jsx("span", { children: formatDisplayValue(ie, f) })] })] }) : jsx("p", { className: "dialog-help", children: x.compareHistoryUnavailable(f) })] });
}
