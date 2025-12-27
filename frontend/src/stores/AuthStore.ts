import { useAuth0 } from "@auth0/auth0-vue";
import { defineStore } from "pinia";
import { ref } from "vue";
import { computed } from "vue";

export const useAuthStore = defineStore("authStore", () => {
  const token = computed(() => import.meta.env.VITE_ERPNEXT_TOKEN);
  const url = computed(() => import.meta.env.VITE_ERPNEXT_URL);
  const company = computed(() => import.meta.env.VITE_ERPNEXT_COMPANY);

  const givenName = ref("");
  const email = ref("");

  async function update() {
    const { getAccessTokenSilently } = useAuth0();

    const token = await getAccessTokenSilently();
    const payloadBase64 = token.split(".")[1] as string;
    const payload = JSON.parse(
      atob(payloadBase64.replace(/-/g, "+").replace(/_/g, "/")),
    );

    const nameSpace = "https://meta.dashboard.com/";
    const meta = payload[nameSpace];

    givenName.value = meta?.given_name || "Guest User";
    email.value = meta?.email || "";
  }

  return {
    token,
    url,
    company,
    givenName,
    email,
    update,
  };
});
