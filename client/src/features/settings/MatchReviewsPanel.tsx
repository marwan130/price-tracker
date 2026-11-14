// @ts-nocheck
import { apiFetch } from "../../lib/api";
import { formatDisplayValue } from "../../lib/format";
import { jsx, jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function MatchReviewsPanel({ notify: c }) {
  const f = useLocale(), [x, d] = useState([]), [S, o] = useState(true), [A, Y] = useState(null), [w, le] = useState(""), [ae, se] = useState(0);
  useEffect(() => {
    let O = true;
    return o(true), le(""), apiFetch("/v1/operations/match-reviews").then(async (z) => {
      O && d(z);
    }).catch((z) => {
      O && le(z instanceof Error ? z.message : f.settingsMatchReviewLoadError);
    }).finally(() => {
      O && o(false);
    }), () => {
      O = false;
    };
  }, [ae, f.settingsMatchReviewLoadError]);
  async function ie(O, z) {
    if (!(z && !O.sourceProductId)) {
      Y(O.reviewEventId);
      try {
        await apiFetch(`/v1/operations/match-reviews/${O.sessionId}/${O.reviewEventId}/resolve`, { method: "POST", body: JSON.stringify({ accept: z, productId: z ? O.sourceProductId : void 0 }) }), c(f.settingsMatchReviewSaved), se((_) => _ + 1);
      } catch (_) {
        c(_ instanceof Error ? _.message : f.actionFailed);
      } finally {
        Y(null);
      }
    }
  }
  return jsx("section", { className: "settings-panel settings-panel--wide", id: "match-reviews", children: jsxs("div", { children: [jsx("h2", { children: f.settingsMatchReviewHeading }), jsx("p", { children: f.settingsMatchReviewBody }), S ? jsx("p", { role: "status", children: f.loading }) : w ? jsx("p", { className: "form-error", role: "alert", children: w || f.settingsMatchReviewLoadError }) : x.length === 0 ? jsx("p", { children: f.settingsMatchReviewEmpty }) : jsx("div", { className: "match-review-list", children: x.map((O) => {
    return jsxs("article", { className: "match-review-card", children: [jsxs("div", { children: [jsx("span", { className: "eyebrow", children: f.settingsMatchReviewSource }), jsx("strong", { children: O.sourceProductName || f.compareUnavailable })] }), jsxs("div", { children: [jsx("span", { className: "eyebrow", children: f.settingsMatchReviewCandidate }), jsx("strong", { children: O.candidate.name }), jsxs("p", { children: [O.candidate.storeName, " \xB7 ", formatDisplayValue(O.candidate.price, O.candidate.currency)] }), jsx("a", { href: O.candidate.productUrl, target: "_blank", rel: "noreferrer", children: O.candidate.productUrl })] }), jsxs("div", { className: "match-review-actions", children: [jsx("button", { className: "button button--quiet", disabled: A === O.reviewEventId, onClick: () => {
      ie(O, false);
    }, children: f.settingsMatchReviewDismiss }), jsx("button", { className: "button button--primary", disabled: A === O.reviewEventId || !$, onClick: () => {
      ie(O, true);
    }, children: f.settingsMatchReviewAccept })] })] }, `${O.sessionId}-${O.reviewEventId}`);
  }) })] }) });
}
