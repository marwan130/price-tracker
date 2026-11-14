// @ts-nocheck
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { Icon } from "../../components/ui/Icon";
import { apiFetch } from "../../lib/api";
import { useCallback, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function AuthDialog({ onClose: c, onSuccess: f, notify: x }) {
  const d = useLocale(), [p, N] = useState("login"), [v, U] = useState(false), [S, o] = useState(""), [A, Y] = useState(""), w = void 0;
  useCallback((se, ie, O) => f(se, ie, O), [f]), useCallback((se) => {
    o(se), x(se);
  }, [x]);
  async function le(se) {
    se.preventDefault(), U(true), o("");
    const ie = new FormData(se.currentTarget);
    try {
      if (p === "forgot") {
        await apiFetch("/v1/auth/forgot-password", { method: "POST", body: JSON.stringify({ email: ie.get("email") }) }), Y(d.authForgotSent);
        return;
      }
      const O = p === "login" ? { email: ie.get("email"), password: ie.get("password") } : { name: ie.get("name"), email: ie.get("email"), password: ie.get("password") }, z = await apiFetch(`/v1/auth/${p === "login" ? "login" : "register"}`, { method: "POST", body: JSON.stringify(O) });
      f(z.accessToken, z.name || String(ie.get("name") || z.email || "Account"), z.emailVerified === true);
    } catch (O) {
      const z = O instanceof Error ? O.message : d.authWorking;
      o(z), x(z);
    } finally {
      U(false);
    }
  }
  const ae = () => {
    o(""), Y(""), N("login");
  };
  return jsx("div", { className: "scrim", onMouseDown: (se) => {
    se.target === se.currentTarget && c();
  }, children: jsxs("section", { className: "dialog auth-dialog", role: "dialog", "aria-modal": "true", "aria-labelledby": "auth-title", children: [jsxs("div", { className: "dialog-head", children: [jsxs("div", { children: [jsx("div", { className: "eyebrow", children: d.authEyebrow }), jsx("h2", { id: "auth-title", children: p === "login" ? d.authLoginTitle : p === "register" ? d.authRegisterTitle : d.authResetTitle })] }), jsx("button", { className: "icon-button", "aria-label": d.close, onClick: c, children: jsx(Icon, { name: "close" }) })] }), w, A ? jsxs(Fragment, { children: [jsx("p", { className: "auth-success-message", role: "status", children: A }), jsx("button", { className: "button button--primary button--full", type: "button", onClick: ae, children: d.authForgotBack })] }) : jsxs("form", { onSubmit: le, children: [p === "register" && jsxs(Fragment, { children: [jsx("label", { className: "field-label", htmlFor: "auth-name", children: d.authNameLabel }), jsx("input", { className: "field", id: "auth-name", name: "name", autoComplete: "name", required: true })] }), jsx("label", { className: "field-label", htmlFor: "auth-email", children: d.authEmailLabel }), jsx("input", { className: "field", id: "auth-email", name: "email", type: "email", autoComplete: "email", required: true }), p !== "forgot" && jsxs(Fragment, { children: [jsx("label", { className: "field-label", htmlFor: "auth-password", children: d.authPasswordLabel }), jsx("input", { className: "field", id: "auth-password", name: "password", type: "password", autoComplete: p === "login" ? "current-password" : "new-password", minLength: 8, required: true })] }), S && jsx("p", { className: "form-error", role: "alert", children: S }), jsx("button", { className: "button button--primary button--full", disabled: v, children: v ? d.authWorking : p === "login" ? d.authLoginButton : p === "register" ? d.authCreateAccount : d.authSendResetLink })] }), p === "login" && !A && jsx("p", { className: "dialog-switch", children: jsx("button", { type: "button", className: "inline-button", onClick: () => {
    o(""), N("forgot");
  }, children: d.authForgotPassword }) }), p === "forgot" && !A && jsx("p", { className: "dialog-switch", children: jsx("button", { type: "button", className: "inline-button", onClick: ae, children: d.authForgotBack }) }), p !== "forgot" && jsxs("p", { className: "dialog-switch", children: [p === "login" ? d.authNewUser : d.authExistingUser, " ", jsx("button", { type: "button", className: "inline-button", onClick: () => {
    o(""), N(p === "login" ? "register" : "login");
  }, children: p === "login" ? d.authCreateAccount : d.authSignIn })] }), p !== "forgot" && jsx("p", { className: "dialog-help", children: d.authHelp })] }) });
}
