// @ts-nocheck
import { APP_NAME } from "../../lib/brand";
import { AuthDialog } from "../auth/AuthDialog";
import { ComparePage } from "../compare/ComparePage";
import { ComparePageSkeleton } from "../../components/ui/skeletons";
import { DISPLAY_CURRENCIES } from "../../lib/markets";
import { DashboardPage } from "../dashboard/DashboardPage";
import { DashboardSkeleton } from "../../components/ui/skeletons";
import { DiscoverPage } from "../discover/DiscoverPage";
import { DiscoverPageSkeleton } from "../../components/ui/skeletons";
import { DisplayCurrencyContext } from "../../lib/i18n-context";
import { Dropdown } from "../../components/ui/Dropdown";
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { GlobalSearchDialog } from "../search/GlobalSearchDialog";
import { Icon } from "../../components/ui/Icon";
import { LocaleContext } from "../../lib/i18n-context";
import { MARKETS } from "../../lib/markets";
import { MyProductsPage } from "../products/MyProductsPage";
import { NotFoundPage } from "../../components/ui/NotFoundPage";
import { NoticeListSkeleton } from "../../components/ui/skeletons";
import { NotificationsPage } from "../notifications/NotificationsPage";
import { ProductDetailsPage } from "../products/ProductDetailsPage";
import { ProductDetailsSkeleton } from "../../components/ui/skeletons";
import { ResetPasswordPage } from "../auth/ResetPasswordPage";
import { SettingsPage } from "../settings/SettingsPage";
import { SettingsSkeleton } from "../../components/ui/skeletons";
import { SidebarSkeleton } from "../../components/ui/skeletons";
import { SkeletonTable } from "../../components/ui/skeletons";
import { TopbarSkeleton } from "../../components/ui/skeletons";
import { TrackPriceDialog } from "../products/TrackPriceDialog";
import { VerifyEmailPage } from "../auth/VerifyEmailPage";
import { apiFetch } from "../../lib/api";
import { asUuid } from "../../lib/api";
import { detectMarketFromTimezone } from "../../lib/markets";
import { localizeCategoryName } from "../../lib/i18n-context";
import { marketCurrencyOf } from "../../lib/markets";
import { navLabelsForRoute } from "../../lib/i18n-context";
import { normalizeProductUrl } from "../../lib/api";
import { routeNameFromPath } from "../../lib/routing";
import { streamFirstCandidate } from "../../lib/api";
import { translations } from "../../i18n";
import { useCallback, useEffect, useRef, useState } from "react";

// "Actual back button" behavior: the back controls unwind the browser
// history so the user returns to exactly the previous page/list, instead
// of jumping to a fixed destination. Fallback keeps the original fixed
// detination when the app was entered through a deep link (there is no
// in-app previous entry to go back to: history.state is null then).
function backOrFallback(fallback) {
  if (window.history && window.history.length > 1 && window.history.state !== null) {
    window.history.back();
    return;
  }
  fallback();
}

export function AppRoot() {
  const [c, f] = useState(routeNameFromPath), [x, d] = useState(location.pathname.startsWith("/product/") ? location.pathname.split("/")[2] : null), [candidateProduct, setCandidateProduct] = useState(history.state?.candidateProduct ?? null), [p, N] = useState(localStorage.getItem("pt-token")), [v, U] = useState(localStorage.getItem("pt-name") || ""), [S, o] = useState(""), [A, Y] = useState(() => localStorage.getItem("pt-sidebar-collapsed") === "true"), w = useCallback((V) => {
    Y((fe) => {
      const Ae = typeof V == "function" ? V(fe) : V;
      return localStorage.setItem("pt-sidebar-collapsed", String(Ae)), Ae;
    });
  }, []), [le, ae] = useState(false), se = useRef(null), [ie, O] = useState(false), [z, $] = useState(false), [_, M] = useState(null), [X, G] = useState(false), [addProductMode, setAddProductMode] = useState(false), Z = useRef(0), re = useRef(false), [xe, je] = useState(""), [te, be] = useState(() => {
    if (!localStorage.getItem("pt-token")) return localStorage.removeItem("pt-compare"), [];
    try {
      return JSON.parse(localStorage.getItem("pt-compare") || "[]");
    } catch {
      return [];
    }
  }), [he, ne] = useState(localStorage.getItem("pt-locale") || "en"), T = (V) => {
    localStorage.setItem("pt-locale", V), document.documentElement.lang = V, document.documentElement.dir = V === "ar" ? "rtl" : "ltr", ne(V), window.dispatchEvent(new Event("pt-locale-changed"));
  }, H = translations[he] ?? translations.en, I = navLabelsForRoute(H), [ue, g] = useState(() => {
    const V = localStorage.getItem("pt-market");
    return MARKETS.some((fe) => fe.code === V) ? V || "" : localStorage.getItem("pt-market-choice") === "all" ? "" : detectMarketFromTimezone(Intl.DateTimeFormat().resolvedOptions().timeZone) ?? "";
  }), [k, K] = useState(() => {
    const V = localStorage.getItem("pt-market"), fe = MARKETS.some((It) => It.code === V), Ae = detectMarketFromTimezone(Intl.DateTimeFormat().resolvedOptions().timeZone);
    return !fe && !Ae && localStorage.getItem("pt-market-choice") !== "all";
  }), [Q, ee] = useState(() => DISPLAY_CURRENCIES.includes(localStorage.getItem("pt-currency") || "") && localStorage.getItem("pt-currency") || ""), [me, B] = useState(localStorage.getItem("pt-theme") || "system"), [Te, J] = useState(true), [pe, tt] = useState(false), [Sz, Tn] = useState(() => window.matchMedia("(max-width: 760px)").matches);
  useEffect(() => {
    const V = setTimeout(() => J(false), 300);
    return () => clearTimeout(V);
  }, []), useEffect(() => {
    const V = window.matchMedia("(max-width: 760px)");
    const fe = () => Tn(V.matches);
    return V.addEventListener("change", fe), () => V.removeEventListener("change", fe);
  }, []);
  const Ge = useCallback((V) => {
    o(V), window.setTimeout(() => o(""), 3600);
  }, []), h = useCallback((V) => {
    localStorage.setItem("pt-market-choice", V || "all"), g(V), K(false);
  }, []);
  useEffect(() => {
    k && (Ge(H.marketChoosePrompt), K(false));
  }, [k, Ge, H]);
  const W = useCallback((V) => {
    $(false), setAddProductMode(false), ae(false), Rt(false);
    const fe = `/${V === "dashboard" ? "" : V}`;
    (c === V || location.pathname === fe) && !x || (history.pushState({}, "", fe), d(null), setCandidateProduct(null), f(V), tt(true), setTimeout(() => tt(false), 240));
  }, [c, x]), F = useCallback((V) => {
    const fe = asUuid(typeof V === "string" ? V : V?.productId);
    const candidate = !fe && V && typeof V === "object" && /^https:\/\//i.test(V.productUrl || "") ? V : null;
    if (!fe && !candidate) return;
    const detailId = fe || "candidate";
    window.scrollTo({ top: 0, behavior: "instant" }), document.querySelector(".main-area")?.scrollTo({ top: 0, behavior: "instant" });
    if (x === detailId && (!candidate || candidateProduct?.productUrl === candidate.productUrl)) return;
    history.pushState(candidate ? { candidateProduct: candidate } : {}, "", `/product/${detailId}`), setCandidateProduct(candidate), d(detailId), f("products"), tt(true), setTimeout(() => tt(false), 240);
  }, [x, candidateProduct]);
  useEffect(() => {
    const V = () => {
      f(routeNameFromPath()), d(location.pathname.startsWith("/product/") ? location.pathname.split("/")[2] : null), setCandidateProduct(history.state?.candidateProduct ?? null), ae(false);
    }, fe = (Ae) => {
      const It = Ae.target;
      if (Ae.key === "/" && !It?.matches('input,textarea,[contenteditable="true"]') && (Ae.preventDefault(), setAddProductMode(false), $(true)), Ae.key === "Escape" && ($(false), setAddProductMode(false), O(false), ae(false), Rt(false)), Ae.key === "Tab") {
        const Ca = document.querySelectorAll('[aria-modal="true"]'), la = le ? document.querySelector("#primary-navigation.sidebar--mobile-open") : Ca.item(Ca.length - 1), na = la ? [...la.querySelectorAll('a[href],button:not(:disabled),input:not(:disabled),select:not(:disabled),textarea:not(:disabled),[tabindex]:not([tabindex="-1"])')].filter((jt) => jt.getClientRects().length) : [];
        if (na.length) {
          const jt = na[0], bl = na[na.length - 1];
          Ae.shiftKey && (document.activeElement === jt || !la?.contains(document.activeElement)) ? (Ae.preventDefault(), bl.focus()) : !Ae.shiftKey && (document.activeElement === bl || !la?.contains(document.activeElement)) && (Ae.preventDefault(), jt.focus());
        }
      }
    };
    return window.addEventListener("popstate", V), window.addEventListener("keydown", fe), () => {
      window.removeEventListener("popstate", V), window.removeEventListener("keydown", fe);
    };
  }, [le]), useEffect(() => {
    if (le) return document.querySelector("#primary-navigation .nav-item")?.focus(), () => se.current?.focus();
  }, [le]), useEffect(() => {
    const V = !!(z || ie || _), fe = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    if (V) {
      document.documentElement.classList.add("has-modal");
      const Ae = document.querySelectorAll('[aria-modal="true"]');
      Ae.item(Ae.length - 1)?.querySelector("input,button")?.focus();
    } else document.documentElement.classList.remove("has-modal");
    return () => {
      fe?.isConnected && fe.focus();
    };
  }, [z, ie, _]), useEffect(() => {
    localStorage.setItem("pt-theme", me);
    const V = window.matchMedia("(prefers-color-scheme: dark)"), fe = () => {
      const Ae = me === "system" ? V.matches ? "dark" : "light" : me;
      document.documentElement.dataset.theme = Ae, document.querySelector('meta[name="theme-color"]')?.setAttribute("content", Ae === "dark" ? "#211f1b" : "#f6f1e8");
    };
    if (fe(), me === "system") return V.addEventListener("change", fe), () => V.removeEventListener("change", fe);
  }, [me]), useEffect(() => {
    localStorage.setItem("pt-locale", he), document.documentElement.lang = he, document.documentElement.dir = he === "ar" ? "rtl" : "ltr";
  }, [he]), useEffect(() => {
    if (x) document.title = `${H.breadcrumbProductDetails} \xB7 ${APP_NAME}`;
    else {
      const V = I[c];
      document.title = V ? `${V} \xB7 ${APP_NAME}` : APP_NAME;
    }
  }, [c, x, I, H]), useEffect(() => {
    p ? localStorage.setItem("pt-compare", JSON.stringify(te)) : localStorage.removeItem("pt-compare");
  }, [te, p]), useEffect(() => {
    ue ? localStorage.setItem("pt-market", ue) : localStorage.removeItem("pt-market");
  }, [ue]), useEffect(() => {
    Q ? localStorage.setItem("pt-currency", Q) : localStorage.removeItem("pt-currency");
  }, [Q]);
  const de = (V) => be((fe) => fe.includes(V) ? fe.filter((Ae) => Ae !== V) : fe.length < 3 ? [...fe, V] : fe), Ne = () => {
    history.replaceState({}, "", "/"), d(null), f("dashboard");
  }, Ce = async (V = xe, openDetails = false) => {
    const fe = V.trim();
    if (fe) if (/^https?:\/\//i.test(fe)) {
      if (re.current) return;
      re.current = true;
      const Ae = ++Z.current;
      let It;
      try {
        It = new URL(fe);
      } catch {
        re.current = false, Ge(H.searchLinkRequired);
        return;
      }
      const Ca = It.hostname.replace(/^www\./i, ""), la = It.pathname.split("/").filter(Boolean), na = (la[la.length - 1] || "").replace(/[-_]+/g, " ").trim();
      // Topbar link entry goes straight to the details page once the page
      // resolves; other entry points keep the preview dialog.
      if (openDetails) {
        G(true), Ge(H.searchOpeningLink);
      } else M({ name: na || `Product from ${Ca}`, storeName: Ca, productUrl: fe, currency: "", submittedUrl: fe }), G(true);
      try {
        const jt = await apiFetch("/v1/products/search/url", { method: "POST", body: JSON.stringify({ url: fe }) }), bl = "sessionId" in jt ? await streamFirstCandidate(jt.sessionId, H) : jt;
        Ae === Z.current && (openDetails ? F({ ...bl }) : M({ ...bl, submittedUrl: fe }));
      } catch {
        Ae === Z.current && (Ge(H.searchOpenLinkFailed), M(null));
      } finally {
        Ae === Z.current && (re.current = false, G(false));
      }
    } else W("discover"), Ue(fe);
  }, [Be, ht] = useState(""), [Dt, at] = useState("all"), [Ft, lt] = useState([]), [xt, Ia] = useState(""), [nt, Ue] = useState(""), [Pt, Xe] = useState(0), [qt, Rt] = useState(false);
  useEffect(() => {
    let V = true;
    return apiFetch("/v1/categories").then((fe) => {
      V && lt(fe);
    }).catch(() => {
      V && lt([]);
    }), () => {
      V = false;
    };
  }, []);
  const aa = (V, fe = Dt === "all" ? "" : Dt) => {
    const Ae = V.trim();
    if (Ae) {
      if ($(false), /^https?:\/\//i.test(Ae)) {
        Ce(Ae, true);
        return;
      }
      Ia(fe), Ue(Ae), Xe((It) => It + 1), W("discover");
    }
  }, Wa = (V) => {
    $(false);
    const fe = asUuid(V.productId) || asUuid(V.productUrl);
    M({ ...V, submittedUrl: fe ? void 0 : V.productUrl, existingProductId: fe });
  }, addSearchCandidate = async (V) => {
    if (!p) {
      Wa(V);
      return true;
    }
    if (X) return false;
    G(true);
    try {
      const fe = asUuid(V.productId) ? await apiFetch(`/v1/products/${V.productId}`) : await apiFetch("/v1/products/import-url", { method: "POST", body: JSON.stringify({ url: V.productUrl }) });
      const Ae = (await apiFetch(`/v1/listings/by-product/${fe.productId}`)).filter((It) => It.isActive && /^[A-Za-z]{3}$/.test(It.currencyCode || ""));
      const It = Ae.find((Ca) => normalizeProductUrl(Ca.productUrl) === normalizeProductUrl(V.productUrl)) || Ae.find((Ca) => (Ca.storeName || "").toLowerCase() === (V.storeName || "").toLowerCase() && Ca.currencyCode?.toUpperCase() === V.currency?.toUpperCase());
      if (!It || !Number.isFinite(It.currentPrice) || It.currentPrice <= 0) throw new Error(H.previewNoOffer);
      await apiFetch("/v1/tracking", { method: "POST", body: JSON.stringify({ productId: fe.productId, listingId: It.listingId, targetPrice: It.currentPrice, currencyCode: It.currencyCode.toUpperCase(), notifyEmail: true, notifyFurtherDecreaseEmail: true }) });
      $(false), setAddProductMode(false), Ge(H.previewTrackingStarted), window.dispatchEvent(new Event("pt-tracking-changed"));
      return true;
    } catch (fe) {
      Ge(fe instanceof Error ? fe.message : H.requestFailed);
      return false;
    } finally {
      G(false);
    }
  };
  if (location.pathname === "/verify-email") return jsx(VerifyEmailPage, { onContinue: Ne });
  if (location.pathname === "/reset-password") return jsx(ResetPasswordPage, { onContinue: Ne });
  /* The drawer (<=760px viewport) always renders the expanded sidebar: the
     collapsed rail markup would otherwise strip the label spans out of the
     DOM entirely, leaving icon rows floating in a wide blank drawer. The
     user's collapse toggle still applies on desktop widths. */
  const Cz = A && !Sz;
  const yl = DISPLAY_CURRENCIES.includes(Q) ? Q : marketCurrencyOf(ue);
  return jsx(LocaleContext.Provider, { value: H, children: jsx(DisplayCurrencyContext.Provider, { value: yl, children: jsxs("div", { className: "app-shell", children: [jsx("a", { className: "skip-link", href: "#main-content", children: H.skipToContent }), le && jsx("button", { className: "mobile-nav-scrim", "aria-label": H.closeNavigation, onClick: () => ae(false) }), jsxs("aside", { id: "primary-navigation", className: `sidebar ${Cz ? "sidebar--collapsed" : ""} ${le ? "sidebar--mobile-open" : ""}`, children: [jsx("div", { className: `sidebar-top ${Cz ? "sidebar-top--collapsed" : ""}`, children: Te ? Cz ? jsx("div", { style: { display: "grid", placeItems: "center", width: "100%", padding: "4px 0 16px" }, children: jsx("span", { className: "skeleton-box", style: { width: 36, height: 36, borderRadius: 10 } }) }) : jsxs("div", { style: { display: "flex", alignItems: "center", justifyContent: "space-between", width: "100%", padding: "4px 6px 18px" }, children: [jsx("span", { className: "skeleton-box", style: { width: 110, height: 28, borderRadius: 6 } }), jsx("span", { className: "skeleton-box", style: { width: 32, height: 32, borderRadius: 8 } })] }) : jsxs(Fragment, { children: [jsx("button", { className: "sidebar-brand-btn", onClick: () => W("dashboard"), "aria-label": `${APP_NAME} home`, children: jsx("img", { src: "/dealio-cropped.png", alt: APP_NAME, className: "sidebar-logo-img", width: 241, height: 68 }) }), jsx("button", { className: "sidebar-collapse-btn", onClick: () => w(true), "aria-label": H.sideCollapse, title: H.sideCollapse, children: jsx(Icon, { name: "sidebar-toggle" }) }), jsx("button", { className: "sidebar-brand-btn sidebar-brand-btn--minimized", onClick: () => w(false), "aria-label": H.sideExpand, title: H.sideExpand, children: [jsx("img", { src: "/dealio-d.png", alt: APP_NAME, className: "sidebar-logo-minimized", width: 206, height: 206 }), jsx("span", { className: "sidebar-minimized-toggle-icon", children: jsx(Icon, { name: "sidebar-toggle" }) })] })] }) }), jsx("nav", { className: "sidebar-nav", "aria-label": H.navigationLabel, children: Te ? jsx(SidebarSkeleton, { collapsed: Cz }) : ["dashboard", "discover", "products", "compare", "notifications"].map((V, fe) => jsxs("button", { "aria-label": I[V], "aria-current": c === V && !x ? "page" : void 0, className: `nav-item ${c === V && !x ? "is-active" : ""}`, onClick: () => { V === "discover" && (Ue(""), Ia("")); W(V); }, title: Cz ? I[V] : void 0, children: [jsx(Icon, { name: ["home", "search", "list", "compare", "bell"][fe] }), jsx("span", { className: "nav-item-label", children: I[V] }), V === "compare" && te.length > 0 && jsx("b", { className: "nav-count", children: te.length })] }, V)) }), jsx("div", { className: "side-spacer" }), jsx("div", { className: "sidebar-bottom", children: Te ? Cz ? jsxs("div", { style: { display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }, children: [jsx("span", { className: "skeleton-box", style: { width: 44, height: 44, borderRadius: 10 } }), jsx("span", { className: "skeleton-box", style: { width: 44, height: 44, borderRadius: 10 } })] }) : jsxs("div", { style: { display: "flex", flexDirection: "column", gap: 8 }, children: [jsx("span", { className: "skeleton-box", style: { width: "100%", height: 42, borderRadius: 12 } }), jsx("span", { className: "skeleton-box", style: { width: "100%", height: 42, borderRadius: 12 } })] }) : jsxs(Fragment, { children: [jsxs("button", { "aria-label": H.navSettings, "aria-current": c === "settings" ? "page" : void 0, className: `nav-item ${c === "settings" ? "is-active" : ""}`, onClick: () => W("settings"), title: Cz ? H.navSettings : void 0, children: [jsx(Icon, { name: "settings" }), !Cz && jsx("span", { children: H.navSettings })] }), p ? jsxs("button", { className: "nav-item sidebar-auth-item", onClick: () => {
    localStorage.removeItem("pt-token"), localStorage.removeItem("pt-name"), N(null), U(""), Ge(H.signedOut);
  }, title: v ? `${H.signOut} (${v})` : H.signOut, "aria-label": H.signOut, children: [jsx(Icon, { name: "sign-out" }), jsx("span", { className: "side-footer-label", children: v ? `${H.signOut} (${v})` : H.signOut })] }) : jsxs("button", { className: "nav-item sidebar-auth-item", onClick: () => O(true), title: Cz ? H.signIn : void 0, "aria-label": H.signIn, children: [jsx(Icon, { name: "sign-in" }), jsx("span", { className: "side-footer-label", children: H.signIn })] })] }) })] }), jsxs("main", { className: "main-area", children: [qt && jsx("div", { className: "topbar-dim-backdrop", onClick: (V) => {
    V.stopPropagation(), Rt(false);
  } }), jsx("header", { className: `topbar ${qt ? "topbar--active" : ""}`, children: Te ? jsx(TopbarSkeleton, {}) : jsxs(Fragment, { children: [jsxs("button", { ref: se, className: "mobile-nav-toggle", "aria-label": H.openNavigation, "aria-expanded": le, "aria-controls": "primary-navigation", onClick: () => ae((V) => !V), children: [jsx("span", {}), jsx("span", {}), jsx("span", {})] }), jsxs("div", { className: "breadcrumbs", children: [jsx("span", { children: APP_NAME }), jsx("span", { className: "crumb-sep", children: "/" }), jsx("strong", { children: x ? H.breadcrumbProductDetails : I[c] })] }), jsxs("div", { className: "topbar-search-area", children: [jsxs("div", { className: "topbar-search-input-wrap", onClick: (V) => {
    V.stopPropagation(), Rt(true);
  }, children: [jsx(Icon, { name: "search" }), jsx("input", { type: "text", value: Be, onFocus: () => Rt(true), onChange: (V) => ht(V.target.value), onKeyDown: (V) => {
    V.key === "Enter" && (Rt(false), aa(Be));
  }, placeholder: H.globalSearchPlaceholder, "aria-label": H.globalSearchLabel }), Be && jsx("button", { type: "button", className: "topbar-clear-btn", onClick: () => {
    ht(""), Ue("");
  }, "aria-label": H.clearSearch, children: jsx(Icon, { name: "close" }) })] }), jsxs("div", { onClick: () => Rt(false), style: { display: "contents" }, children: [jsx(Dropdown, { value: Dt, options: [{ value: "all", label: H.allCategories }, ...Ft.map((V) => ({ value: String(V.categoryId), label: localizeCategoryName(V.name, H) }))], onChange: (V) => {
    at(V), Be.trim() && aa(Be, V === "all" ? "" : V);
  }, className: "topbar-cat-dropdown", ariaLabel: H.discoverSearchLabel }), jsx(Dropdown, { value: ue, options: [{ value: "", label: H.marketAll }, ...MARKETS.map((V) => ({ value: V.code, label: `${he === "ar" ? V.nameAr : V.code === "AE" ? "UAE" : V.code === "SA" ? "KSA" : V.name} · ${V.currency}` }))], onChange: (V) => {
    h(V), Be.trim() && aa(Be);
  }, className: "topbar-market-dropdown", ariaLabel: H.marketLabel })] }), jsx("button", { type: "button", className: "button button--primary button--small topbar-search-btn", onClick: (V) => {
    V.stopPropagation(), Rt(false);
    const fe = Be.trim();
    aa(fe);
  }, children: X ? H.loading : H.globalSearchLabel })] })] }) }), jsx("div", { className: "page-wrap", id: "main-content", tabIndex: -1, "data-page": x ? "product-detail" : c, children: Te || pe ? x ? jsx(ProductDetailsSkeleton, {}) : c === "dashboard" ? jsx(DashboardSkeleton, {}) : c === "discover" ? jsx(DiscoverPageSkeleton, {}) : c === "products" ? jsx(SkeletonTable, { rows: 5 }) : c === "compare" ? jsx(ComparePageSkeleton, {}) : c === "notifications" ? jsx(NoticeListSkeleton, { count: 4 }) : jsx(SettingsSkeleton, {}) : x ? jsx(ProductDetailsPage, { key: x, id: x, candidate: x === "candidate" ? candidateProduct : null, token: p, notify: Ge, onCompare: de, compareIds: te, onBack: () => backOrFallback(() => W("products")), onTrack: Wa }) : c === "dashboard" ? jsx(DashboardPage, { token: p, addValue: xe, setAddValue: je, onSubmit: Ce, previewBusy: X, openProduct: F, onDiscover: () => W("discover"), onSignIn: () => O(true), notify: Ge }) : c === "discover" ? jsx(DiscoverPage, { query: nt, categoryId: xt, market: ue, searchPulse: Pt, onSearch: aa, onPreview: Wa, onCompare: de, compareIds: te, onOpen: F, onBack: () => backOrFallback(() => { Ue(""); Ia(""); }) }) : c === "products" ? jsx(MyProductsPage, { token: p, onOpen: F, onAddProduct: () => { setAddProductMode(true); $(true); }, onPreview: Ce, onCompare: de, compareIds: te, notify: Ge, onSignIn: () => O(true), categories: Ft, markets: MARKETS, locale: he }) : c === "compare" ? jsx(ComparePage, { token: p, ids: te, onRemove: (V) => de(V), onOpen: F, notify: Ge }) : c === "notifications" ? jsx(NotificationsPage, { token: p, notify: Ge, onSignIn: () => O(true), onOpen: F }) : c === "settings" ? jsx(SettingsPage, { locale: he, setLocale: T, theme: me, setTheme: B, market: ue, setMarket: h, displayCurrency: Q, setDisplayCurrency: ee, compareIds: te, token: p, notify: Ge, onSignIn: () => O(true) }) : jsx(NotFoundPage, { onGo: W }) })] }), _ && jsx(TrackPriceDialog, { item: _, token: p, busy: X, suspended: ie, onClose: () => {
    Z.current++, re.current = false, M(null), G(false);
  }, onSignIn: () => O(true), notify: Ge }), z && jsx(GlobalSearchDialog, { market: ue, categoryId: Dt === "all" ? "" : Dt, addMode: addProductMode, busy: X, onClose: () => { $(false); setAddProductMode(false); }, onSearch: aa, onSelect: addProductMode ? addSearchCandidate : Wa }), ie && jsx(AuthDialog, { onClose: () => O(false), onSuccess: (V, fe, Ae) => {
    localStorage.setItem("pt-token", V), localStorage.setItem("pt-name", fe), N(V), U(fe), O(false), Ge(Ae ? H.signIn : H.authRegisterSuccess);
  }, notify: Ge }), S && jsx("div", { role: "status", className: "toast", children: S })] }) }) });
}
