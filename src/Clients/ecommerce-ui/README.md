# ecommerce-ui

Storefront and admin panel of the [E-Commerce Microservices](../../../README.md) project.
React 19 + TypeScript (strict) + Vite; every request goes through the API gateway.

- Architecture, folder structure and coding rules: [ARCHITECTURE.md](ARCHITECTURE.md) (Turkish)

## Commands

```bash
npm ci
npm run dev        # http://localhost:5173 (fixed port: it is an allowed origin in the backend CORS policy)
npm run lint       # ESLint
npm run typecheck  # tsc -b
npm run build      # type check + production build (requires VITE_API_URL)
```

`VITE_API_URL` points to the API gateway. In development it defaults to `http://localhost:5000`; see `.env.example`.
