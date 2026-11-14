# Frontend README

The frontend is a React 19 application built with TypeScript, Vite, Tailwind CSS, Axios, Zustand, React Hook Form, Zod, Recharts, and Lucide icons.

## Structure

```text
frontend/
  src/
    components/  Shared layout and dashboard components
    context/     Theme and currency providers
    hooks/       Shared React hooks
    lib/         API client, stores, service worker setup
    pages/       Route-level screens
    types/       Shared TypeScript types
  public/        PWA assets
  dist/          Production build output
```

## Runtime Responsibilities

- Authenticates through the backend auth endpoints.
- Injects bearer tokens into API requests.
- Refreshes expired access tokens using refresh tokens.
- Renders product search, tracking, notifications, admin, stores, price history, login, and registration flows.
- Hides protected navigation items (Products, Dashboard) for unauthenticated users.

## Configuration

Create a local `.env` file in the `frontend/` directory:

```text
VITE_API_URL=http://localhost:5001
```

If `VITE_API_URL` is not set at build time, the app falls back to the production Azure backend URL.

## Development

```bash
cd frontend
npm install
npm run dev
```

## Production Build

```bash
cd frontend
npm run build
```

The output is written to `frontend/dist`.

## Deployment

The frontend is containerised with `docker/Dockerfile.frontend`. In CI the `VITE_API_URL` build argument is injected from the `API_URL` GitHub secret so the production image always points at the live Azure backend.