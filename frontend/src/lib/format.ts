// @ts-nocheck
import { translations } from "../i18n";

function resolveLocale() {
  return typeof document > "u" ? "en" : document.documentElement.lang === "ar" ? "ar-EG" : "en";
}

export function formatNumber(c, f = {}, x = resolveLocale()) {
  return new Intl.NumberFormat(x, f).format(c);
}

function formatCurrency(c, f, x = resolveLocale()) {
  if (/^[A-Za-z]{3}$/.test(f)) return new Intl.NumberFormat(x, { style: "currency", currency: f.toUpperCase(), maximumFractionDigits: 2 }).format(c);
  const d = translations[x.startsWith("ar") ? "ar" : "en"].discoverCurrencyUnavailable;
  return `${formatNumber(c, { maximumFractionDigits: 2 }, x)} ${f || d}`;
}

export function formatDateTime(c, f = { dateStyle: "medium", timeStyle: "short" }, x = resolveLocale()) {
  return new Intl.DateTimeFormat(x, f).format(new Date(c));
}

export const formatDisplayValue = (c, f = "") => c == null ? "\u2014" : formatCurrency(c, f);

export const formatShortDateOrDash = (c) => c ? formatDateTime(c) : "\u2014";
