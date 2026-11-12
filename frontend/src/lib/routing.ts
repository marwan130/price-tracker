// @ts-nocheck
export const routeNameFromPath = () => {
  const c = location.pathname;
  if (c === "/" || c === "") return "dashboard";
  if (c.startsWith("/product/")) return "products";
  const f = c.split("/")[1];
  return ["dashboard", "discover", "products", "compare", "notifications", "settings"].includes(f) ? f : "not-found";
};
