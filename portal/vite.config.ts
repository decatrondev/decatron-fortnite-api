import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// El portal se sirve estático desde Nginx en el mismo dominio que la API,
// así que las llamadas van a rutas relativas (/v1/...). En dev, proxy al backend.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/v1": "http://localhost:5199",
      "/sprites": "http://localhost:5199",
      "/swagger": "http://localhost:5199",
    },
  },
  build: { outDir: "dist" },
});
