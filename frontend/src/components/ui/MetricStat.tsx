// @ts-nocheck
import { jsx, jsxs } from "react/jsx-runtime";

export function MetricStat({ label: c, value: f, note: x, tone: d = "" }) {
  return jsxs("article", { className: "metric", children: [jsx("div", { className: "metric-label", children: c }), jsx("div", { className: `metric-value ${d ? `metric-value--${d}` : ""}`, children: f }), jsx("div", { className: "metric-note", children: x })] });
}
