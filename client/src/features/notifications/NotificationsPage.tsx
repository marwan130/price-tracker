// @ts-nocheck
import { Dropdown } from "../../components/ui/Dropdown";
import { EmptyState } from "../../components/ui/EmptyState";
import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { GuestPage } from "../../components/ui/GuestPage";
import { Icon } from "../../components/ui/Icon";
import { NoticeListSkeleton } from "../../components/ui/skeletons";
import { Price } from "../../components/ui/Price";
import { apiFetch } from "../../lib/api";
import { formatShortDateOrDash } from "../../lib/format";
import { useEffect, useState } from "react";
import { useLocale } from "../../lib/i18n-context";

export function NotificationsPage({ token: c, notify: f, onSignIn: x, onOpen: d }) {
  const p = useLocale(), [N, v] = useState([]), [U, S] = useState(!!c), [o, A] = useState(""), [Y, w] = useState("all"), [le, ae] = useState("all"), [se, ie] = useState(0);
  if (useEffect(() => {
    if (!c) return;
    let _ = true;
    return S(true), apiFetch("/v1/notifications?Page=0&Size=100").then((M) => {
      _ && (v(M.content), A(""));
    }).catch(() => {
      _ && A(p.notificationsLoadErrorBody);
    }).finally(() => {
      _ && S(false);
    }), () => {
      _ = false;
    };
  }, [c, se]), !c) return jsx(GuestPage, { title: p.notificationsHeading, eyebrow: p.notificationsEyebrow, pageClass: "notifications-heading", body: p.guestPageAccessBody, onSignIn: x });
  const O = N.filter((_) => (Y === "all" || (Y === "read" ? _.isRead : !_.isRead)) && (le === "all" || _.eventType === le)), z = async (_) => {
    try {
      await apiFetch(`/v1/notifications/${_}/read`, { method: "PATCH" }), v((M) => M.map((X) => X.notificationId === _ ? { ...X, isRead: true } : X)), window.dispatchEvent(new Event("pt-unread-changed"));
    } catch {
      f(p.notificationUpdateFailed);
    }
  }, $ = async () => {
    try {
      await apiFetch("/v1/notifications/read-all", { method: "PATCH" }), v((_) => _.map((M) => ({ ...M, isRead: true }))), window.dispatchEvent(new Event("pt-unread-changed"));
    } catch {
      f(p.notificationReadFailed);
    }
  };
  return jsxs(Fragment, { children: [jsx("div", { className: "page-heading notifications-heading", children: jsxs("div", { children: [jsx("div", { className: "eyebrow", children: p.notificationsEyebrow }), jsx("h1", { children: p.notificationsHeading })] }) }), jsxs("div", { className: "toolbar notifications-toolbar", children: [jsxs("label", { className: "sort-control", children: [jsx("span", { className: "sort-control-label", children: p.notificationsShowFilter }), jsx(Dropdown, { className: "filter-dropdown", ariaLabel: p.notificationsShowFilter, value: Y, options: [{ value: "all", label: p.notificationsFilterAll }, { value: "unread", label: p.notificationsFilterUnread }, { value: "read", label: p.notificationsFilterRead }], onChange: w })] }), jsxs("label", { className: "sort-control", children: [jsx("span", { className: "sort-control-label", children: p.notificationsFilterType }), jsx(Dropdown, { className: "filter-dropdown", ariaLabel: p.notificationsFilterType, value: le, options: [{ value: "all", label: p.notificationsFilterAll }, { value: "targetReached", label: p.notificationsFilterTarget }, { value: "furtherDecrease", label: p.notificationsFilterFurther }, { value: "trackingStopped", label: p.notificationsFilterStopped }], onChange: (_) => ae(_) })] }), jsxs("div", { className: "notifications-header-actions", children: [jsx("span", { className: "results-count notifications-unread-count", children: p.notificationsUnread(N.filter((_) => !_.isRead).length) }), jsxs("button", { className: "button button--mark-all-read", disabled: !N.some((_) => !_.isRead), onClick: () => {
    $();
  }, children: [jsx(Icon, { name: "check" }), p.notificationsMarkAllRead] })] })] }), U ? jsx(NoticeListSkeleton, { count: 4 }) : o ? jsx(EmptyState, { className: "notifications-load-error", title: p.notificationsLoadError, body: o, action: jsx("button", { className: "button button--quiet", onClick: () => ie((_) => _ + 1), children: p.tryAgain }) }) : O.length ? jsx("div", { className: "notice-list", children: O.map((_) => jsxs("article", { className: `notice-item ${_.isRead ? "" : "notice-item--unread"}`, children: [jsx("span", { className: "notice-icon", children: jsx(Icon, { name: "bell" }) }), jsxs("div", { className: "notice-copy", children: [jsxs("div", { className: "notice-top", children: [jsx("h3", { children: _.productId ? jsx("button", { className: "product-link", onClick: () => d(_.productId), children: _.productName }) : _.productName }), jsx("time", { children: formatShortDateOrDash(_.sentAt) })] }), jsxs("p", { children: [_.eventType === "trackingStopped" ? p.notificationsTrackingStopped : _.eventType === "furtherDecrease" ? p.notificationsFurtherDrop + " " : p.notificationsTargetReached + " ", _.eventType !== "trackingStopped" && jsx(Price, { amount: _.triggeredPrice, currencyCode: _.triggeredCurrencyCode || _.currencyCode }), _.eventType !== "trackingStopped" && jsxs(Fragment, { children: [" (target ", jsx(Price, { amount: _.targetPrice, currencyCode: _.targetCurrencyCode || _.currencyCode }), _.convertedTriggeredPrice != null && jsxs("span", { children: [" · ", p.notificationsEquivalent, ": ", jsx(Price, { amount: _.convertedTriggeredPrice, currencyCode: _.targetCurrencyCode || _.currencyCode })] }), _.storeName ? ` · ${_.storeName}` : "", ")."] })] }), jsxs("span", { className: `delivery-state delivery-state--${_.status.toLowerCase()}`, children: [p.notificationsDeliveryLabel + ": ", p.notificationsDeliveryState(_.status)] })] }), !_.isRead && jsx("button", { className: "text-button", onClick: () => {
    z(_.notificationId);
  }, children: p.notificationsMarkRead })] }, _.notificationId)) }) : jsx(EmptyState, { title: p.notificationsEmpty, body: p.notificationsEmptyBody })] });
}
