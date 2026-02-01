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
      },
    }),
    VitePWA({
      registerType: "autoUpdate",
      includeAssets: ["favicon/**/*", "logo.png"],
      devOptions: {
        enabled: false,
        type: "module",
        navigateFallback: "index.html",
      },
      manifest: {
        name: "Njeremoto Dashboard",
        short_name: "Njeremoto",
        start_url: "/",
        scope: "/",
        display: "standalone",
        theme_color: "#111827",
        background_color: "#111827",
        icons: [
          {
            src: "/favicon/android-icon-36x36.png",
            sizes: "36x36",
            type: "image/png",
            purpose: ["any", "maskable"],
          },
          {
            src: "/favicon/android-icon-48x48.png",
            sizes: "48x48",
            type: "image/png",
            purpose: ["any", "maskable"],
          },
          {
            src: "/favicon/android-icon-72x72.png",
            sizes: "72x72",
            type: "image/png",
            purpose: ["any", "maskable"],
          },
          {
            src: "/favicon/android-icon-96x96.png",
            sizes: "96x96",
            type: "image/png",
            purpose: ["any", "maskable"],
          },
          {
            src: "/favicon/android-icon-144x144.png",
            sizes: "144x144",
            type: "image/png",
            purpose: ["any", "maskable"],
          },
          {
            src: "/favicon/android-icon-192x192.png",
            sizes: "192x192",
            type: "image/png",
            purpose: ["any", "maskable"],
          },
          {
            src: "/logo.png",
            sizes: "300x300",
            type: "image/png",
            purpose: ["any", "maskable"],
          },
        ],
      },
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
