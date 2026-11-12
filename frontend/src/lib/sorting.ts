// @ts-nocheck
export const fieldToSortChoice = { productName: "name", currentPrice: "lowest", targetPrice: "target", difference: "change", biggestDrop: "drop", targetProximity: "proximity", lastUpdated: "updated", isActive: "status" };

export const sortChoiceConfigs = { name: { key: "productName", direction: "asc" }, lowest: { key: "currentPrice", direction: "asc" }, target: { key: "targetPrice", direction: "asc" }, change: { key: "difference", direction: "asc" }, drop: { key: "biggestDrop", direction: "desc" }, proximity: { key: "targetProximity", direction: "asc" }, updated: { key: "lastUpdated", direction: "desc" }, status: { key: "isActive", direction: "desc" } };

function trackSortValue(c, f) {
  if (f === "productName") return c.productName;
  if (f === "isActive") return Number(c.isActive);
  if (f === "lastUpdated") return c.currentPriceUpdatedAt ? Date.parse(c.currentPriceUpdatedAt) : void 0;
  if (f === "currentPrice") return c.currentPriceInDisplayCurrency ?? void 0;
  if (f === "targetPrice") return c.targetPriceInDisplayCurrency ?? void 0;
  if (f === "biggestDrop") return c.priceDropPercent ?? void 0;
  if (!(c.currentPriceInDisplayCurrency == null || c.targetPriceInDisplayCurrency == null)) return f === "targetProximity" ? Math.abs(c.currentPriceInDisplayCurrency - c.targetPriceInDisplayCurrency) / Math.max(1, c.targetPriceInDisplayCurrency) : c.currentPriceInDisplayCurrency - c.targetPriceInDisplayCurrency;
}

function compareTrackingRows(c, f, x, d) {
  const p = trackSortValue(c, x), N = trackSortValue(f, x), v = p == null || typeof p == "number" && !Number.isFinite(p), U = N == null || typeof N == "number" && !Number.isFinite(N);
  return v ? U ? 0 : 1 : U ? -1 : (typeof p == "string" && typeof N == "string" ? p.localeCompare(N) : Number(p) - Number(N)) * d;
}

export function filterAndSortTrackings(c, f, x) {
  const d = f.query.trim().toLocaleLowerCase();
  return c.filter((N) => {
    const v = f.status === "all" || (f.status === "active" ? N.isActive : !N.isActive), U = !d || `${N.productName} ${N.storeName || ""}`.toLocaleLowerCase().includes(d), S = f.category === "all" || N.categoryName === f.category, o = f.market === "all" || N.marketCountry === f.market, A = N.currentPriceInDisplayCurrency == null || N.targetPriceInDisplayCurrency == null ? null : N.currentPriceInDisplayCurrency - N.targetPriceInDisplayCurrency, Y = f.price === "all" || f.price === "missing" && N.currentPrice == null || f.price === "reached" && A !== null && A <= 0 || f.price === "near" && A !== null && A > 0 && A <= N.targetPriceInDisplayCurrency * 0.1 || f.price === "above" && A !== null && A > N.targetPriceInDisplayCurrency * 0.1;
    return v && U && S && o && Y;
  }).sort((N, v) => compareTrackingRows(N, v, x.key, x.direction === "asc" ? 1 : -1));
}
