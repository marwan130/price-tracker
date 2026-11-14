// @ts-nocheck
import { jsx, jsxs } from "react/jsx-runtime";

export function EmptyState({ title: c, body: f, action: x, className: d = "" }) {
  return jsxs("div", { className: `empty-state ${d}`.trim(), children: [jsx("span", { className: "empty-ring", children: jsx("span", {}) }), jsx("h3", { children: c }), jsx("p", { children: f }), x] });
}
