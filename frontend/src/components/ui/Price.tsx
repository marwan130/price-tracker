// @ts-nocheck
import { DisplayCurrencyContext } from "../../lib/i18n-context";
import { convertCurrency } from "../../lib/currency-client";
import { formatDisplayValue } from "../../lib/format";
import { formatNumber } from "../../lib/format";
import { jsx, jsxs } from "react/jsx-runtime";
import { useContext, useEffect, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function Price({ amount: c, currencyCode: f, className: x }) {
  const d = useLocale(), p = useContext(DisplayCurrencyContext), N = f?.toUpperCase() || "", v = `${N}:${c ?? ""}:${p}`, [U, S] = useState(null);
  useEffect(() => {
    let Y = true;
    return c == null || c <= 0 || !/^[A-Z]{3}$/.test(N) || !p || N === p ? (S(null), () => {
      Y = false;
    }) : (S(null), convertCurrency(c, N, p).then((w) => {
      Y && S({ key: v, value: w });
    }).catch(() => {
      Y && S({ key: v, value: null });
    }), () => {
      Y = false;
    });
  }, [c, N, p, v]);
  const o = U?.key === v ? U.value : null, A = c == null ? "\u2014" : o ? formatDisplayValue(o.amount, p) : N ? formatDisplayValue(c, N) : formatNumber(c, { maximumFractionDigits: 2 });
  return jsxs("span", { className: "price-display", children: [jsx("strong", { className: x, children: A }), o && c != null && jsx("small", { className: "converted-price", children: d.convertedOriginal(formatDisplayValue(c, N)) })] });
}
