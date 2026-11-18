import { defineStore } from "pinia";
import { computed } from "vue";
import { useFeatureStore } from "@/stores/FeatureStore";
import { FEATURES } from "@/constants/features";

interface NavRegistryItem {
  label: string;
  icon: string;
  to: string;
  feature?: string;
}

const NAV_REGISTRY: NavRegistryItem[] = [
  { label: "Overview", icon: "i-lucide-layout-dashboard", to: "/" },
  { label: "Sales", icon: "i-lucide-shopping-cart", to: "/sales", feature: FEATURES.sales },
  { label: "Stock", icon: "i-lucide-package", to: "/stock", feature: FEATURES.stock },
  { label: "Expenses", icon: "i-lucide-receipt", to: "/expenses", feature: FEATURES.expense },
];

export const useNavStore = defineStore("navStore", () => {
  const featureStore = useFeatureStore();

  // Emits plain navigation items (the registry-only `feature` key is stripped).
  const navItems = computed(() =>
    NAV_REGISTRY.filter((item) => !item.feature || featureStore.hasFeature(item.feature)).map(
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      ({ feature, ...rest }) => rest,
    ),
  );

  return {
    navItems,
  };
});
