// @ts-nocheck
import { StatusDialog } from "./StatusDialog";
import { apiFetch } from "../../lib/api";
import { jsx } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function VerifyEmailPage({ onContinue: c }) {
  const f = useLocale(), [x, d] = useState(f.loading), [p, N] = useState("");
  return useEffect(() => {
    const v = new URLSearchParams(location.search).get("token");
    if (!v) {
      N(f.authMissingToken);
      return;
    }
    apiFetch(`/v1/auth/verify-email?token=${encodeURIComponent(v)}`, { method: "GET" }).then(() => d(f.authVerifyBody)).catch((U) => N(U.message));
  }, []), jsx(StatusDialog, { title: p ? f.authVerifyFailed : f.authVerifyTitle, body: x, error: p, onContinue: c });
}
