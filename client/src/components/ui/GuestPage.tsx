// @ts-nocheck
import { EmptyState } from "./EmptyState";
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { useLocale } from "../../lib/i18n-context";

export function GuestPage({ title: c, eyebrow: f, body: x, onSignIn: d, pageClass: p, headerAction: N }) {
  const v = useLocale();
  return jsxs(Fragment, { children: [jsxs("div", { className: `page-heading ${p || ""}`.trim(), children: [jsxs("div", { className: p === "my-products-heading" ? "my-products-heading-copy" : "", children: [f && jsx("div", { className: "eyebrow", children: f }), jsx("h1", { children: c })] }), N] }), jsx(EmptyState, { title: v.guestPageAccessHeading, body: x, className: "guest-page-access", action: jsx("button", { className: "button button--blue", onClick: d, children: v.settingsTrackingSignIn }) })] });
}
