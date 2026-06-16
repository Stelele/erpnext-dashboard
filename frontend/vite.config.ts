import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import ui from "@nuxt/ui/vite";
import { VitePWA } from "vite-plugin-pwa";
import path from "node:path";

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "src"),
    },
  },
  plugins: [
    vue(),
    ui({
      ui: {
        pageGrid: {
          base: "relative grid grid-cols-6 sm:grid-cols-2 md:grid-cols-6 lg:grid-cols-6 gap-4",
        },
        table: {
          slots: {
            th: "px-3.5 py-3.5 text-lg text-highlighted text-left rtl:text-right font-semibold [&:has([role=checkbox])]:pe-0",
            td: "p-3.5 text-lg text-normal whitespace-nowrap [&:has([role=checkbox])]:pe-0",
          },
        },
      },
    }),
    VitePWA({
      registerType: "autoUpdate",
      includeAssets: ["favicon/**/*", "logo.png"],
      devOptions: {
        enabled: mode === "development",
        type: "module",
        navigateFallback: "index.html",
      },
      manifest: false,
      workbox: {
        globPatterns: ["**/*.{html,js,css,svg,png,ico,webp,woff2}"],
        navigateFallback: "/index.html",
        runtimeCaching: [
          {
            urlPattern: ({ url }) => url.pathname.startsWith("/api"),
            handler: "NetworkFirst",
            options: {
              cacheName: "api-cache",
              networkTimeoutSeconds: 5,
              cacheableResponse: { statuses: [0, 200] },
            },
          },
          {
            urlPattern: ({ request }) => request.destination === "image",
            handler: "StaleWhileRevalidate",
            options: {
              cacheName: "image-cache",
              expiration: { maxEntries: 200, maxAgeSeconds: 60 * 60 * 24 * 30 },
              cacheableResponse: { statuses: [0, 200] },
            },
          },
        ],
      },
    }),
  ],
}));
