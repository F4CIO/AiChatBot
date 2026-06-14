import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  // Relative base => asset URLs are emitted relative to index.html (./assets/...),
  // so the built output can be deployed under ANY subfolder (or the domain root)
  // and moved around the server without recompiling.
  base: './',
  plugins: [react()],
  server: { port: 5101, strictPort: true },
})
