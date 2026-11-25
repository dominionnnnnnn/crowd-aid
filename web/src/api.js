import axios from 'axios'

// Relative base URL: works in dev (Vite proxy) and prod (Caddy proxy) without changes.
// Your partner's SimplyTodo code hardcodes "http://localhost:5159/api" — swap it for this
// (or import.meta.env.VITE_API_URL) before deploying, or the live site will call the
// visitor's own localhost.
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
  headers: { 'Content-Type': 'application/json' },
})

export default api
