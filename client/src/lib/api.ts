// @ts-nocheck
import { translations } from "../i18n";

export function normalizeProductUrl(c) {
  try {
    const f = new URL(c.replaceAll("&amp;", "&")), x = f.hostname.toLowerCase().replace(/^www\./, ""), d = f.pathname.split("/").filter(Boolean), p = d.findIndex((N) => N.toLowerCase() === "dp");
    return p >= 0 && d[p + 1] ? `${x}/dp/${d[p + 1].toLowerCase()}` : (x === "noon.com" || x.endsWith(".noon.com")) && d.length >= 3 && d.at(-1)?.toLowerCase() === "p" ? `${x}/${d[0].toLowerCase()}/${d.at(-2)?.toLowerCase()}/p` : `${x}${f.pathname.replace(/\/$/, "")}`.toLowerCase();
  } catch {
    return c.trim().toLowerCase();
  }
}

export const API_BASE_URL = "";

export const sanitizeImageUrl = (c, f) => {
  try {
    const image = new URL(c.replaceAll("&amp;", "&"), f || window.location.origin);
    const host = image.hostname.toLowerCase();
    const ipLiteral = host.startsWith("[") || /^\d{1,3}(?:\.\d{1,3}){3}$/.test(host);
    const localHost = host === "localhost" || host.endsWith(".localhost") || host.endsWith(".local") || host.endsWith(".internal");
    return !image.username && !image.password && !ipLiteral && !localHost && (!image.port || image.port === "80" || image.port === "443")
      && (image.protocol === "https:" || image.protocol === "http:") ? image.href : "";
  } catch {
    return "";
  }
};

// Images live on retailer CDNs; when a server-mapped image is missing this
// asks the API to mine one from the tracked product page. The endpoint rate-
// limits and negative-caches, so a render burst collapses to one call per
// product; failures cache as "" and retry on the next page visit.
const productImageCache = new Map();

export async function resolveProductImage(productId, pageUrl, refresh = false) {
  const key = productId || `url:${pageUrl || ""}`;
  if (refresh) productImageCache.delete(key);
  if (productImageCache.has(key)) return productImageCache.get(key);
  if (!productId && !pageUrl) return "";
  const query = productId
    ? `productId=${encodeURIComponent(productId)}`
    : `url=${encodeURIComponent(pageUrl)}`;
  const p = await apiFetch(`/v1/products/image-resolve?${query}`);
  const url = sanitizeImageUrl(typeof p === "string" ? p : p?.imageUrl || "");
  productImageCache.set(key, url);
  return url;
}

export async function apiFetch(c, f = {}) {
  const x = localStorage.getItem("pt-token"), d = await fetch(`${API_BASE_URL}${c}`, { ...f, headers: { "Content-Type": "application/json", ...x ? { Authorization: `Bearer ${x}` } : {}, ...f.headers } }), p = await d.json().catch(() => null);
  if (!d.ok || p?.success === false) { const locale = document.documentElement.lang.toLowerCase().startsWith("ar") ? translations.ar : translations.en; throw new Error(locale.requestFailed); }
  return p && "data" in p ? p.data : p;
}

export async function streamFirstCandidate(c, f) {
  return new Promise((x, d) => {
    const p = new EventSource(`${API_BASE_URL}/v1/products/search/stream?sessionId=${encodeURIComponent(c)}`), N = window.setTimeout(() => {
      p.close(), d(new Error(f.previewVerificationTimeout));
    }, 18e4), v = (U, S) => {
      window.clearTimeout(N), p.close(), U ? d(U) : S && x(S);
    };
    p.addEventListener("candidate", (U) => {
      try {
        const S = JSON.parse(U.data);
        S.payload && v(void 0, S.payload);
      } catch {
        v(new Error(f.previewDataUnreadable));
      }
    }), p.addEventListener("sourceFailed", () => v(new Error(f.previewDetailsFailed))), p.onerror = () => v(new Error(f.previewStreamInterrupted));
  });
}

export const asUuid = (c) => /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(c) ? c : void 0;
