import "./style.css";
import App from "./App.vue";
import { createApp } from "vue";
import { router } from "./routes";
import { createPinia } from "pinia";
import ui from "@nuxt/ui/vue-plugin";
import { addCollection } from "@iconify/vue";
import lucide from "@iconify-json/lucide/icons.json";
import { createAuth0 } from "@auth0/auth0-vue";

addCollection(lucide);

const app = createApp(App);
const pinia = createPinia();

app.use(
  createAuth0({
    domain: import.meta.env.VITE_AUTH0_DOMAIN,
    clientId: import.meta.env.VITE_AUTH0_CLIENT_ID,
    authorizationParams: {
      audience: import.meta.env.VITE_AUTH0_AUDIENCE,
      redirect_uri: window.location.origin,
      scope: "openid profile email read:users read:sites read:companies",
    },
  }),
);

app.use(router);
app.use(ui);
console.log("frontend");
app.use(pinia);

app.mount("#app");
