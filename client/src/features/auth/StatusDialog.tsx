// @ts-nocheck
import { jsx, jsxs } from "react/jsx-runtime";
import { useLocale } from "../../lib/i18n-context";

export function StatusDialog({ title: c, body: f, error: x, onContinue: d }) {
  const p = useLocale();
  return jsx("div", { className: "scrim", children: jsxs("section", { className: "dialog auth-dialog", role: "status", children: [jsx("div", { className: "eyebrow", children: p.authEyebrow }), jsx("h1", { children: c }), jsx("p", { children: x || f }), jsx("button", { className: "button button--primary button--full", onClick: d, children: p.authContinue })] }) });
}
