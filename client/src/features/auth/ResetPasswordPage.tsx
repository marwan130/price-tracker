// @ts-nocheck
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { apiFetch } from "../../lib/api";
import { useLocale } from "../../lib/i18n-context";
import { useState } from "react";

export function ResetPasswordPage({ onContinue: c }) {
  const f = useLocale(), [x, d] = useState(false), [p, N] = useState(""), [v, U] = useState("");
  async function S(o) {
    o.preventDefault();
    const A = new URLSearchParams(location.search).get("token");
    if (!A) {
      U(f.authPasswordResetConfirm);
      return;
    }
    d(true), U("");
    try {
      await apiFetch("/v1/auth/reset-password", { method: "POST", body: JSON.stringify({ token: A, newPassword: new FormData(o.currentTarget).get("password") }) }), N(f.authResetSaved);
    } catch (Y) {
      U(Y.message);
    } finally {
      d(false);
    }
  }
  return jsx("div", { className: "scrim", children: jsxs("section", { className: "dialog auth-dialog", role: "dialog", "aria-modal": "true", children: [jsx("div", { className: "eyebrow", children: f.authEyebrow }), jsx("h1", { children: f.authResetTitle }), p ? jsxs(Fragment, { children: [jsx("p", { children: p }), jsx("button", { className: "button button--primary button--full", onClick: c, children: f.authResetContinue })] }) : jsxs("form", { onSubmit: S, children: [jsx("label", { className: "field-label", htmlFor: "reset-password", children: f.authResetNewPassword }), jsx("input", { className: "field", id: "reset-password", name: "password", type: "password", minLength: 8, autoComplete: "new-password", required: true }), v && jsx("p", { className: "form-error", role: "alert", children: v }), jsx("button", { className: "button button--primary button--full", disabled: x, children: x ? f.authResetSaving : f.authResetContinue })] })] }) });
}
