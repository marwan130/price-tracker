// @ts-nocheck
import { jsx } from "react/jsx-runtime";
import { useEffect, useRef, useState } from "react";
import { sanitizeImageUrl, resolveProductImage } from "../../lib/api";

const MIN_PHOTO_SIDE = 80;

const isIconSizedBitmap = (img) => {
  const w = img.naturalWidth, h = img.naturalHeight;
  return w > 0 && h > 0 && (w < MIN_PHOTO_SIDE || h < MIN_PHOTO_SIDE);
};

export function ProductImage({ source: c, baseUrl: f, productId: u, alt: x, loading: d = "eager", layered: L = false }) {
  const [resolved, h] = useState("");
  const [fallback, setFallback] = useState("");
  const fallbackAttempted = useRef(false);
  useEffect(() => {
    if (c || (!u && !f)) return;
    h("");
    let K = true;
    resolveProductImage(u, f).then((p) => { if (K) h(p || ""); }).catch(() => { if (K) h(""); });
    return () => { K = false; };
  }, [c, u, f]);
  useEffect(() => {
    setFallback("");
    fallbackAttempted.current = false;
  }, [c, u, f]);
  const p = fallback || resolved || (c ? sanitizeImageUrl(c, f) : "");

  async function handleImageError(event) {
    const image = event.currentTarget;
    if (!fallbackAttempted.current && (u || f)) {
      fallbackAttempted.current = true;
      try {
        const replacement = sanitizeImageUrl(await resolveProductImage(u, f, true), f);
        if (replacement && replacement !== image.currentSrc) {
          setFallback(replacement);
          return;
        }
      } catch { /* keep the unavailable image out of the layout */ }
    }
    image.remove();
  }

  const [frames, setFrames] = useState(null);
  useEffect(() => {
    if (!L) return;
    if (!p) { setFrames((fr) => fr && fr.length ? [] : fr); return; }
    setFrames((fr) => {
      const list = fr || [];
      return list[list.length - 1]?.url === p ? list : [...list, { url: p, ready: false }];
    });
  }, [p, L]);
  useEffect(() => {
    if (!L) return;
    const last = frames?.[frames.length - 1];
    if (!last?.ready || frames.length < 2) return;
    const t = setTimeout(() => setFrames((fr) => fr.length > 1 ? fr.slice(-1) : fr), 160);
    return () => clearTimeout(t);
  }, [frames, L]);

  const anyReady = L ? !!frames?.some((fr) => fr.ready) : false;
  const [scanOver, k] = useState(false);
  useEffect(() => { if (L) k(false); }, [p, L]);
  useEffect(() => {
    if (!L || anyReady || scanOver) return;
    const t = setTimeout(() => k(true), 12000);
    return () => clearTimeout(t);
  }, [L, p, anyReady, scanOver]);

  if (!L) {
    return p ? jsx("img", {
      className: "product-image", src: p, alt: x, width: 512, height: 512,
      loading: d, decoding: "async", referrerPolicy: "strict-origin-when-cross-origin",
      onLoad: (N) => { if (isIconSizedBitmap(N.currentTarget)) { void handleImageError(N); } },
      onError: (N) => { void handleImageError(N); },
    }) : null;
  }

  const scanEl = scanOver ? null : jsx("div", { className: "product-image-scan", "aria-hidden": "true" });
  if (!frames || !frames.length) return scanEl;
  return (anyReady ? [] : [scanEl]).concat(frames.map((fr) => jsx("img", {
    className: "product-image product-image--stack" + (fr.ready ? " is-current" : ""),
    src: fr.url,
    alt: x,
    width: 512,
    height: 512,
    loading: d,
    decoding: "async",
    referrerPolicy: "strict-origin-when-cross-origin",
    onLoad: (N) => {
      if (isIconSizedBitmap(N.currentTarget)) { setFrames((fr2) => fr2.filter((g) => g.url !== fr.url)); return; }
      setFrames((fr2) => fr2.map((g) => g.url === fr.url ? { ...g, ready: true } : g));
    },
    onError: () => setFrames((fr2) => fr2.filter((g) => g.url !== fr.url)),
  }, fr.url)));
}
