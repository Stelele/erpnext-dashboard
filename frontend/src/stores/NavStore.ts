import type { NavigationMenuItem } from "@nuxt/ui";
import { defineStore } from "pinia";
import { ref } from "vue";

export const useNavStore = defineStore("navStore", () => {
  const navItems = ref<NavigationMenuItem[]>([
    { label: "Overview", icon: "i-lucide-layout-dashboard", to: "/" },
    // { label: 'Sales', icon: 'i-lucide-shopping-cart', route: '/sales' },
    // { label: 'Stock', icon: 'i-lucide-package', route: '/stock' },
    // { label: 'Products', icon: 'i-lucide-tag', route: '/products' },
    { label: "Expenses", icon: "i-lucide-receipt", to: "/expenses" },
  ]);

  return {
    navItems,
  };
});
