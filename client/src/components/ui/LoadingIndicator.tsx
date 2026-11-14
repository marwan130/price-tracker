// @ts-nocheck
import { jsx, jsxs } from "react/jsx-runtime";

export function LoadingIndicator({ className: c, label: f }) {
  return jsxs("div", { className: `search-loading-indicator ${c}`, role: "status", "aria-label": f, children: [jsxs("span", { className: "search-loading-dots", "aria-hidden": "true", children: [jsx("i", {}), jsx("i", {}), jsx("i", {})] }), jsx("span", { children: f })] });
}
