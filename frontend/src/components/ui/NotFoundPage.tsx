// @ts-nocheck
import { Icon } from "./Icon";
import { jsx, jsxs } from "react/jsx-runtime";
import { useLocale } from "../../lib/i18n-context";

export function NotFoundPage({ onGo: c }) {
  const f = useLocale();
  return jsx("div", { className: "not-found-wrap", role: "main", "aria-label": f.notFoundAriaLabel, children: jsxs("div", { className: "not-found-card", children: [jsxs("div", { className: "not-found-badge-wrap", children: [jsx("span", { className: "not-found-badge", children: "404" }), jsx("span", { className: "not-found-stamp", children: f.notFoundStamp })] }), jsx("div", { className: "eyebrow", style: { color: "var(--brand)", marginBottom: 6 }, children: f.notFoundEyebrow }), jsx("h1", { className: "not-found-title", children: f.notFoundHeading }), jsx("p", { className: "not-found-desc", children: f.notFoundDescription }), jsxs("div", { className: "not-found-deal-strip", children: [jsxs("span", { children: [f.notFoundObservation, ": ", jsx("strong", { children: "404" })] }), jsxs("span", { children: [f.notFoundStatus, ": ", jsx("strong", { style: { color: "var(--brand)" }, children: f.notFoundStatusValue })] }), jsxs("span", { children: [f.notFoundRecommendation, ": ", jsx("strong", { style: { color: "var(--green)" }, children: f.navDiscover })] })] }), jsxs("div", { className: "not-found-actions", children: [jsxs("button", { className: "button button--primary", onClick: () => c("discover"), children: [jsx(Icon, { name: "search" }), " ", f.notFoundExploreDeals] }), jsxs("button", { className: "button button--tonal", onClick: () => c("dashboard"), children: [jsx(Icon, { name: "home" }), " ", f.notFoundDashboard] })] })] }) });
}
