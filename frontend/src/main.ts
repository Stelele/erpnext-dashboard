import "./style.css";
import App from "./App.vue";
import { createApp } from "vue";
import { router } from "./routes";
import { createPinia } from "pinia";
import ui from "@nuxt/ui/vue-plugin";
import { addCollection } from "@iconify/vue";
import lucide from "@iconify-json/lucide/icons.json";
import { createAuth0 } from "@auth0/auth0-vue";
import { createHead } from "@unhead/vue/client";

addCollection(lucide);

// Force dark mode permanently
localStorage.setItem('vueuse-color-scheme', 'dark');
document.documentElement.classList.add('dark');
document.documentElement.classList.remove('light');

const app = createApp(App);
const pinia = createPinia();
const head = createHead();

app.use(
  createAuth0({
    domain: import.meta.env.VITE_AUTH0_DOMAIN,
    clientId: import.meta.env.VITE_AUTH0_CLIENT_ID,
    cacheLocation: import.meta.env.DEV ? 'localstorage' : undefined,
    authorizationParams: {
      audience: import.meta.env.VITE_AUTH0_AUDIENCE,
      redirect_uri: window.location.origin,
      scope: "openid profile email read:users read:sites read:companies read:expenses update:expenses",
    },
  }),
);

app.use(router);
app.use(ui);
app.use(pinia);
app.use(head);

app.mount("#app");
