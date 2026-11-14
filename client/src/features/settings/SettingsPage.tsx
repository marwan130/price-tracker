// @ts-nocheck
import { API_BASE_URL } from "../../lib/api";
import { DISPLAY_CURRENCIES } from "../../lib/markets";
import { Dropdown } from "../../components/ui/Dropdown";
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { Icon } from "../../components/ui/Icon";
import { MARKETS } from "../../lib/markets";
import { MatchReviewsPanel } from "./MatchReviewsPanel";
import { SettingsSkeleton } from "../../components/ui/skeletons";
import { apiFetch } from "../../lib/api";
import { formatShortDateOrDash } from "../../lib/format";
import { marketLabel } from "../../lib/markets";
import { useEffect, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function SettingsPage({ locale: c, setLocale: f, theme: x, setTheme: d, market: p, setMarket: N, displayCurrency: v, setDisplayCurrency: U, compareIds: S, token: o, notify: A, onSignIn: Y }) {
  const w = useLocale(), [le, ae] = useState("account"), [se, ie] = useState(""), [O, z] = useState(false), [$, _] = useState(null), [M, X] = useState(false), [G, Z] = useState(!!o), [re, xe] = useState(""), [je, te] = useState(false), [be, he] = useState(false), [ne, T] = useState(false);
  useEffect(() => {
    if (!o) {
      ie(""), z(false), he(false), _(null), te(false), Z(false), xe("");
      return;
    }
    let J = true;
    return Z(true), xe(""), apiFetch("/v1/users/me").then((pe) => {
      J && (ie(pe.email), z(pe.emailVerified), he(pe.isTrackingPaused), _(pe.deletionScheduledAtUtc || null), te(pe.role === "Admin"));
    }).catch((pe) => {
      J && (xe(pe instanceof Error ? pe.message : w.requestFailed), A(pe.message));
    }).finally(() => {
      J && Z(false);
    }), () => {
      J = false;
    };
  }, [o, A]);
  async function H() {
    X(true);
    try {
      await apiFetch("/v1/auth/resend-verification", { method: "POST", body: JSON.stringify({ email: se }) }), A(w.settingsVerificationSent);
    } catch (J) {
      A(J.message);
    } finally {
      X(false);
    }
  }
  async function I() {
    if (!o) {
      Y();
      return;
    }
    T(true);
    try {
      const J = !be;
      await apiFetch(`/v1/users/me/tracking/${J ? "pause" : "resume"}`, { method: "PATCH" }), he(J), A(J ? w.settingsTrackingPaused : w.settingsTrackingActive);
    } catch (J) {
      A(J.message);
    } finally {
      T(false);
    }
  }
  async function ue(J) {
    if (!o) return Y();
    try {
      const pe = await fetch(`${API_BASE_URL}/v1/users/me/export?format=${J}`, { headers: { Authorization: `Bearer ${o}` } });
      if (!pe.ok) throw new Error(w.requestFailed);
      let tt;
      if (J === "json") {
        const W = await pe.json(), F = W.data ?? W;
        tt = new Blob([JSON.stringify({ ...F, clientPreferences: { theme: x, locale: c, market: p, displayCurrency: v, compareIds: S } }, null, 2)], { type: "application/json" });
      } else tt = await pe.blob();
      const Ge = URL.createObjectURL(tt), h = document.createElement("a");
      h.href = Ge, h.download = `dealio-account.${J}`, h.click(), window.setTimeout(() => URL.revokeObjectURL(Ge), 0), A(w.settingsExportComplete);
    } catch (pe) {
      A(pe.message);
    }
  }
  async function g() {
    if (window.confirm(w.settingsDeleteConfirm)) try {
      const J = await apiFetch("/v1/users/me", { method: "DELETE" });
      _(J.deletionScheduledAtUtc), he(true), A(w.settingsDeleteSuccess);
    } catch (J) {
      A(J.message);
    }
  }
  async function k() {
    try {
      await apiFetch("/v1/users/me/deletion/cancel", { method: "POST" }), _(null), A(w.settingsDeleteCancelled);
    } catch (J) {
      A(J.message);
    }
  }
  const K = [{ value: "system", label: w.settingsThemeSystem }, { value: "light", label: w.settingsThemeLight }, { value: "dark", label: w.settingsThemeDark }], Q = [{ value: "en", label: "English" }, { value: "ar", label: "\u0627\u0644\u0639\u0631\u0628\u064A\u0629" }], ee = [{ value: "", label: w.myProductsAllMarkets }, ...MARKETS.map((J) => ({ value: J.code, label: marketLabel(J, c) }))], me = new Intl.DisplayNames([c], { type: "currency" }), B = [{ value: "", label: w.settingsCurrencyMarket }, ...DISPLAY_CURRENCIES.map((J) => ({ value: J, label: `${J} \xB7 ${me.of(J) || J}` }))], Te = [{ id: "account", label: w.settingsAccountHeading }, { id: "appearance", label: w.settingsAppearanceHeading }, { id: "markets", label: w.settingsMarketsHeading }, { id: "notifications", label: w.settingsNotificationsHeading }, { id: "tracking", label: w.settingsTrackingHeading }];
  return G ? jsx(SettingsSkeleton, {}) : jsxs(Fragment, { children: [jsx("div", { className: "page-heading", children: jsxs("div", { children: [jsx("h1", { children: w.settingsHeading }), jsx("p", { children: w.settingsSubheading })] }) }), jsxs("div", { className: "settings-layout", children: [jsx("nav", { className: "settings-category-nav", "aria-label": w.settingsHeading, children: Te.map((J) => jsx("button", { type: "button", className: le === J.id ? "is-active" : "", "aria-pressed": le === J.id, onClick: () => ae(J.id), children: J.label }, J.id)) }), jsxs("div", { className: "settings-panels", children: [le === "account" && jsx(Fragment, { children: jsxs("section", { className: "settings-panel", id: "account", children: [jsxs("div", { children: [jsx("h2", { children: w.settingsAccountHeading }), o && !re && !G && jsx("p", { className: "account-email-info", children: $ ? w.settingsDeletionScheduled(formatShortDateOrDash($)) : se || w.settingsAccountUnavailable }), o && !re && !G && jsxs("span", { className: `status ${O ? "status--active" : "status--paused"}`, children: [jsx("i", {}), O ? w.settingsEmailVerified : w.settingsEmailUnverified] }), !o && jsx("p", { className: "account-email-info", children: w.settingsAccountGuest })] }), jsxs("div", { className: "account-actions", children: [o && !re && !G && !O && jsx("button", { className: "button button--tonal", disabled: M || !se || !!$, onClick: () => {
    H();
  }, children: M ? w.settingsSending : w.settingsSendVerification }), o && !G && !re && ($ ? jsx("button", { className: "button button--primary", onClick: () => {
    k();
  }, children: w.settingsCancelDeleteButton }) : jsx("button", { className: "button button--danger", onClick: () => {
    g();
  }, children: w.settingsDeleteButton })), o && !G && jsxs("div", { className: "settings-account-export-actions", children: [jsx("button", { className: "button button--export button--export-blue", onClick: () => { ue("json"); }, children: w.settingsExportJson }), jsx("button", { className: "button button--export button--export-yellow", onClick: () => { ue("csv"); }, children: w.settingsExportCsv })] }), !o && jsx("button", { className: "button button--account-access", onClick: Y, children: w.settingsAccountAccess })] })] }) }), le === "appearance" && jsxs("section", { className: "settings-panel", id: "appearance", children: [jsxs("div", { children: [jsx("h2", { children: w.settingsAppearanceHeading }), jsx("p", { children: w.settingsAppearanceSubheading })] }), jsxs("div", { className: "settings-controls", children: [jsxs("div", { className: "field-label", children: [jsx("span", { children: w.settingsTheme }), jsx(Dropdown, { value: x, options: K, onChange: d, ariaLabel: w.settingsTheme })] }), jsxs("div", { className: "field-label", children: [jsx("span", { children: w.settingsLanguage }), jsx(Dropdown, { value: c, options: Q, onChange: (J) => f(J), ariaLabel: w.settingsLanguage })] })] })] }), le === "markets" && jsxs("section", { className: "settings-panel", id: "markets", children: [jsxs("div", { children: [jsx("h2", { children: w.settingsMarketsHeading }), jsx("p", { children: w.settingsMarketsSubheading })] }), jsxs("div", { className: "settings-controls", children: [jsxs("div", { className: "field-label", children: [jsx("span", { children: w.settingsPreferredMarket }), jsx(Dropdown, { value: p, options: ee, onChange: (J) => {
    N(J), localStorage.setItem("pt-market", J);
  }, ariaLabel: w.settingsPreferredMarket })] }), jsxs("div", { className: "field-label", children: [jsx("span", { children: w.settingsDisplayCurrency }), jsx(Dropdown, { value: v, options: B, onChange: (J) => {
    U(J), localStorage.setItem("pt-currency", J);
  }, ariaLabel: w.settingsDisplayCurrency })] })] })] }), le === "notifications" && jsxs("section", { className: "settings-panel", id: "notifications", children: [jsxs("div", { children: [jsx("h2", { children: w.settingsNotificationsHeading }), jsx("p", { children: w.settingsNotificationsSubheading })] }), jsxs("span", { className: "status status--active", children: [jsx("i", {}), w.settingsNotificationsPerProduct] })] }), le === "tracking" && jsxs(Fragment, { children: [jsxs("section", { className: "settings-panel", id: "tracking", children: [jsxs("div", { children: [jsx("h2", { children: w.settingsTrackingHeading }), jsx("p", { children: w.settingsTrackingSubheading })] }), o ? jsx("button", { className: `button button--pause-all ${be ? "is-paused" : ""}`, disabled: ne || G || !!re || !!$, onClick: () => {
    I();
  }, children: ne ? w.settingsSaving : jsxs(Fragment, { children: [jsx(Icon, { name: be ? "play" : "pause" }), be ? w.settingsResumeAll : w.settingsPauseAll] }) }) : jsx("button", { className: "button button--blue", onClick: Y, children: w.settingsTrackingSignIn })] }), je && jsx(MatchReviewsPanel, { notify: A })] })] })] })] });
}
