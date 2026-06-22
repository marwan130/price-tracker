import React, { createContext, useContext, useState } from "react";

export type CurrencyCode = "EGP" | "SAR" | "AED" | "USD";

interface CurrencyContextType {
  currency: CurrencyCode;
  setCurrency: (code: CurrencyCode) => void;
  convertPrice: (price: number | null | undefined, fromCurrency: string | null) => number | null;
  formatPrice: (price: number | null | undefined, fromCurrency: string | null) => string;
}

const rates: Record<CurrencyCode, number> = {
  USD: 1.0,
  EGP: 48.0,
  SAR: 3.75,
  AED: 3.67,
};

const CurrencyContext = createContext<CurrencyContextType | undefined>(undefined);

export function CurrencyProvider({ children }: { children: React.ReactNode }) {
  const [currency, setCurrencyState] = useState<CurrencyCode>(() => {
    const saved = localStorage.getItem("preferred_currency");
    return (saved as CurrencyCode) || "EGP";
  });

  const setCurrency = (code: CurrencyCode) => {
    setCurrencyState(code);
    localStorage.setItem("preferred_currency", code);
  };

  const convertPrice = (price: number | null | undefined, fromCurrency: string | null): number | null => {
    if (price == null) return null;
    const from = (fromCurrency?.toUpperCase() || "EGP") as CurrencyCode;
    const fromRate = rates[from] || rates["EGP"];
    const toRate = rates[currency];
    
    // Convert to USD base first, then to target currency
    const inUSD = price / fromRate;
    return inUSD * toRate;
  };

  const formatPrice = (price: number | null | undefined, fromCurrency: string | null): string => {
    if (price == null) return "—";
    const converted = convertPrice(price, fromCurrency);
    if (converted == null) return "—";
    
    const symbolMap: Record<CurrencyCode, string> = {
      USD: "$",
      EGP: "EGP",
      SAR: "SAR",
      AED: "AED",
    };
    
    return `${symbolMap[currency]} ${converted.toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })}`;
  };

  return (
    <CurrencyContext.Provider value={{ currency, setCurrency, convertPrice, formatPrice }}>
      {children}
    </CurrencyContext.Provider>
  );
}

export function useCurrency() {
  const context = useContext(CurrencyContext);
  if (!context) throw new Error("useCurrency must be used within a CurrencyProvider");
  return context;
}
