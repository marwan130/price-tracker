// @ts-nocheck
import { apiFetch } from "./api";

// One server-side rate snapshot feeds every displayed price; the browser derives
// cross rates locally (target / source) so search results for ANY market render
// in the currency chosen in settings without further round trips.
const RATES_STORAGE_KEY = "pt-fx-rates";

let ratesCache = null;

let ratesPromise = 0;

function snapshotExpiresAt(nextUpdateAtUtc) {
  const next = nextUpdateAtUtc ? Date.parse(nextUpdateAtUtc) : 0;
  // Cap local reuse at the provider's own freshness horizon, with a sane default.
  const bound = next > Date.now() ? next : Date.now() + 3600 * 1e3;
  return Math.min(bound, Date.now() + 36 * 3600 * 1e3);
}

function acceptSnapshot(v) {
  if (!v || !v.rates || !v.rateUpdatedAtUtc) return null;
  const snapshot = {
    rates: v.rates,
    rateUpdatedAtUtc: v.rateUpdatedAtUtc,
    expiresAt: snapshotExpiresAt(v.nextUpdateAtUtc)
  };
  ratesCache = snapshot;
  try {
    localStorage.setItem(RATES_STORAGE_KEY, JSON.stringify(snapshot));
  } catch {
    // Storage can be full or blocked; the in-memory copy still covers this page.
  }
  return snapshot;
}

async function getRateSnapshot() {
  if (ratesCache && ratesCache.expiresAt > Date.now()) return ratesCache;
  try {
    const stored = JSON.parse(localStorage.getItem(RATES_STORAGE_KEY) || "null");
    if (stored?.rates && stored.expiresAt > Date.now()) {
      ratesCache = stored;
      return stored;
    }
  } catch {
    // Corrupt storage entry is treated as absent.
  }
  ratesPromise ||= apiFetch("/v1/currencies/rates")
    .then(acceptSnapshot)
    .finally(() => {
      ratesPromise = 0;
    });
  return ratesPromise;
}

export async function convertCurrency(c, f, x) {
  const d = f.toUpperCase(), p = x.toUpperCase();
  const snapshot = await getRateSnapshot();
  if (snapshot) {
    const from = snapshot.rates[d], to = snapshot.rates[p];
    if (from > 0 && to > 0)
      return { amount: to / from * c, rateUpdatedAtUtc: snapshot.rateUpdatedAtUtc };
  }
  const v = await apiFetch("/v1/currencies/convert", { method: "POST", body: JSON.stringify({ targetCurrency: p, items: [{ amount: c, currencyCode: d }] }) });
  const A = v?.items?.[0];
  return A?.convertedAmount == null || !v.rateUpdatedAtUtc ? null : { amount: A.convertedAmount, rateUpdatedAtUtc: v.rateUpdatedAtUtc, attributionUrl: v.attributionUrl };
}

export function cachedCrossRate(f, x) {
  if (!ratesCache) return null;
  const d = String(f).toUpperCase(), p = String(x).toUpperCase();
  if (d === p) return 1;
  const from = ratesCache.rates[d], to = ratesCache.rates[p];
  return from > 0 && to > 0 ? to / from : null;
}

