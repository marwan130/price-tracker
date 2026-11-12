// @ts-nocheck
import { createContext, useContext } from "react";
import { translations } from "../i18n";

export const LocaleContext = createContext(translations.en);

export function useLocale() {
  return useContext(LocaleContext);
}

export function localizeCategoryName(c, f) {
  return { electronics: f.dealCategoryElectronics, "mobile phones": f.searchCategoryMobilePhones, laptops: f.searchCategoryLaptops, tablets: f.searchCategoryTablets, audio: f.dealCategoryAudio, "tvs & monitors": f.searchCategoryTvsMonitors, wearables: f.searchCategoryWearables, fashion: f.dealCategoryFashion, "home appliances": f.searchCategoryHomeAppliances, furniture: f.searchCategoryFurniture, beauty: f.dealCategoryBeauty, general: f.searchCategoryGeneral }[c.trim().toLowerCase()] ?? c;
}

export const navLabelsForRoute = (c) => ({ dashboard: c.navDashboard, discover: c.navDiscover, products: c.navProducts, compare: c.navCompare, notifications: c.navNotifications, settings: c.navSettings, "not-found": c.notFoundHeading });

export const DisplayCurrencyContext = createContext("");
