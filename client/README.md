# Dealio client

Dealio is a React 19, TypeScript, and Vite single-page application. It includes Dashboard, Discover, My Products, product details, Compare, Notifications, Settings, and authentication flows. The visual system supports Light, Dark, and System themes, with English and Arabic language selection and document-level LTR/RTL direction.

## Local development

```powershell
cd client
npm ci
npm run dev
```

Vite proxies `/v1` requests to `http://localhost:5001`. Set `VITE_API_URL` only when the API is hosted at a different origin; keep it empty in local Compose so nginx routes API requests to the API service.

## Build and focused client tests

```powershell
npm run build
npm test
```

`npm test` uses Node's built-in test runner and TypeScript type stripping to exercise the My Products filtering and sorting rules without an additional test dependency. It requires Node 24 or newer. Full browser-based accessibility, responsive, and Arabic RTL review remains outstanding.

## Product behavior

- Discovery search streams provider and retailer-verification events. Candidates are not treated as verified store offers until the scraper confirms their details.
- Markets and display currency are independent. Tracked-price sorting, comparisons, and price filters use values converted to the selected display currency when a fresh rate is available.
- Product images load directly from their retailer image URL and sit on a white, uncropped `object-fit: contain` surface. No background-removal service or image-processing request is used.
- Settings includes account export and grace-period deletion/cancellation. Account-file import is intentionally not part of the product.

The frontend production bundle is served by nginx in Compose, which also provides SPA fallback for direct application routes.
