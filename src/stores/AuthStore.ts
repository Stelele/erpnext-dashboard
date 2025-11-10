import { defineStore } from "pinia";
import { computed, ref } from "vue";

export const useAuthStore = defineStore('authStore', () => {
    const token = computed(() => import.meta.env.VITE_ERPNEXT_TOKEN)
    const url = computed(() => import.meta.env.VITE_ERPNEXT_URL)
    const company = computed(() => import.meta.env.VITE_ERPNEXT_COMPANY)

    return {
        token,
        url,
        company,
    }
})