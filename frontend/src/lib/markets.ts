// @ts-nocheck
export const MARKETS = [{ code: "AE", currency: "AED", name: "United Arab Emirates", nameAr: "\u0627\u0644\u0625\u0645\u0627\u0631\u0627\u062A \u0627\u0644\u0639\u0631\u0628\u064A\u0629 \u0627\u0644\u0645\u062A\u062D\u062F\u0629" }, { code: "BH", currency: "BHD", name: "Bahrain", nameAr: "\u0627\u0644\u0628\u062D\u0631\u064A\u0646" }, { code: "EG", currency: "EGP", name: "Egypt", nameAr: "\u0645\u0635\u0631" }, { code: "JO", currency: "JOD", name: "Jordan", nameAr: "\u0627\u0644\u0623\u0631\u062F\u0646" }, { code: "KW", currency: "KWD", name: "Kuwait", nameAr: "\u0627\u0644\u0643\u0648\u064A\u062A" }, { code: "OM", currency: "OMR", name: "Oman", nameAr: "\u0639\u064F\u0645\u0627\u0646" }, { code: "QA", currency: "QAR", name: "Qatar", nameAr: "\u0642\u0637\u0631" }, { code: "SA", currency: "SAR", name: "Saudi Arabia", nameAr: "\u0627\u0644\u0633\u0639\u0648\u062F\u064A\u0629" }, { code: "TR", currency: "TRY", name: "T\xFCrkiye", nameAr: "\u062A\u0631\u0643\u064A\u0627" }];

export const DISPLAY_CURRENCIES = [.../* @__PURE__ */ new Set([...MARKETS.map((c) => c.currency), "EUR", "GBP", "USD"])].sort();

export const marketCurrencyOf = (c) => MARKETS.find((f) => f.code === c)?.currency ?? "";

export const marketLabel = (c, f) => `${f === "ar" ? c.nameAr : c.name} \xB7 ${c.currency}`;

const TIMEZONE_MARKETS = { "Asia/Dubai": "AE", "Asia/Bahrain": "BH", "Africa/Cairo": "EG", "Asia/Amman": "JO", "Asia/Kuwait": "KW", "Asia/Muscat": "OM", "Asia/Qatar": "QA", "Asia/Riyadh": "SA", "Europe/Istanbul": "TR" };

export function detectMarketFromTimezone(c) {
  return c ? TIMEZONE_MARKETS[c] ?? null : null;
}
