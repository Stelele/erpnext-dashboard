import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import ui from '@nuxt/ui/vite'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [
      vue(),
      ui({
        ui: {
          pageGrid: {
            base: 'relative grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-8'
          }
        },
      }),
      VitePWA({
        devOptions: {
          enabled: true,
          type: 'module',
          navigateFallback: 'index.html'
        },
        manifest: {
          name: 'Njeremoto Dashboard',
          short_name: 'Njeremoto',
          description: 'Understand the current state of the Njeremoto estate',
          icons: [
            {
              src: '/favicon/android-icon-36x36.png',
              sizes: '36x36',
              type: 'image/png',
              purpose: ['any', 'maskable'],
            },
            {
              src: '/favicon/android-icon-48x48.png',
              sizes: '48x48',
              type: 'image/png',
              purpose: ['any', 'maskable'],
            },
            {
              src: '/favicon/android-icon-72x72.png',
              sizes: '72x72',
              type: 'image/png',
              purpose: ['any', 'maskable'],
            },
            {
              src: '/favicon/android-icon-96x96.png',
              sizes: '96x96',
              type: 'image/png',
              purpose: ['any', 'maskable'],
            },
            {
              src: '/favicon/android-icon-144x144.png',
              sizes: '144x144',
              type: 'image/png',
              purpose: ['any', 'maskable'],
            },
            {
              src: '/favicon/android-icon-192x192.png',
              sizes: '192x192',
              type: 'image/png',
              purpose: ['any', 'maskable'],
            },
            {
              src: '/logo.png',
              sizes: '300x300',
              type: 'image/png',
              purpose: ['any', 'maskable'],
            }
          ],
        },
        workbox: {
          runtimeCaching: [{
            urlPattern: ({ url }) => {
              return url.href.startsWith(env.VITE_ERPNEXT_URL ?? '');
            },
            handler: "CacheFirst" as const,
            options: {
              cacheName: "erpnext-api-cache",
              cacheableResponse: {
                statuses: [0, 200]
              }
            }
          }]
        }
      }),
    ],
  }
})
