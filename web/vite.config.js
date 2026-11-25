import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The React app always calls relative "/api/...".
// - Dev: Vite proxies /api to the API container (no CORS headaches).
// - Prod: Caddy does the same proxying on the VPS.
export default defineConfig({
  plugins: [react()],
  server: {
    host: true,      // listen on 0.0.0.0 so Docker can expose it
    port: 5173,
    watch: { usePolling: true }, // reliable hot reload on Windows + Docker
    proxy: {
      '/api': {
        target: process.env.API_PROXY_TARGET || 'http://localhost:5159',
        changeOrigin: true,
      },
    },
  },
})
