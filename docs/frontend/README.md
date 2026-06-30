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

## Configuration

Create local environment settings as needed:

```text
VITE_API_URL=https://localhost:7000
```

If `VITE_API_URL` is not set, the app defaults to `https://localhost:7000`.

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