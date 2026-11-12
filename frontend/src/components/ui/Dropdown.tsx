// @ts-nocheck
import { jsx } from "react/jsx-runtime";
import { mountThemedDropdown } from "./themed-dropdown";
import { useEffect, useRef } from "react";

export function Dropdown({ value: c, options: f, onChange: x, placeholder: d, className: p = "", ariaLabel: N }) {
  const S = useRef(null), U = useRef(null);
  const config = { value: c, options: f, onChange: x, placeholder: d, className: p, ariaLabel: N };
  useEffect(() => {
    if (!S.current) return;
    U.current = mountThemedDropdown(S.current, config);
    return () => { U.current?.destroy(); U.current = null; };
  }, []);
  useEffect(() => { U.current?.update(config); }, [c, f, x, d, p, N]);
  return jsx("div", { ref: S, className: `custom-dropdown ${p}` });
}
