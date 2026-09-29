# Calcio-Analytic — Frontend

React + TypeScript + Vite web app for the Calcio-Analytic football odds & match
analytics platform. Phase 1 foundation: a landing page that checks the backend
`/health` endpoint.

## Stack

- React 18 + TypeScript
- Vite 6
- Tailwind CSS 4 (via the `@tailwindcss/vite` plugin)

## Getting started

```powershell
npm install
copy .env.example .env   # then adjust VITE_API_BASE_URL if needed
npm run dev
```

The dev server runs on http://localhost:5173.

## Configuration

| Variable            | Default                 | Description                     |
| ------------------- | ----------------------- | ------------------------------- |
| `VITE_API_BASE_URL` | `http://localhost:8080` | Base URL of the .NET backend API |

## Scripts

- `npm run dev` — start the Vite dev server
- `npm run build` — type-check and build the production bundle to `dist/`
- `npm run preview` — preview the production build locally

## Docker

```powershell
docker build -t calcio-analytic-frontend .
docker run -p 8080:80 calcio-analytic-frontend
```

The image builds the app with Node and serves the static `dist/` output with
nginx (SPA fallback configured in `nginx.conf`).
