import type { DropdownMenuItem } from "@nuxt/ui";
import type { Period } from "./PeriodUtilities";
import { computed } from "vue";
import { useDataStore } from "../stores/DataStore";
import { update } from "./UpdateData";

export const filterItems = computed<DropdownMenuItem[]>(() => [
  { label: "Today", onSelect: () => onFilterChange("Today") },
  { label: "This Week", onSelect: () => onFilterChange("This Week") },
  { label: "This Month", onSelect: () => onFilterChange("This Month") },
  { label: "This Quarter", onSelect: () => onFilterChange("This Quarter") },
  { label: "This Year", onSelect: () => onFilterChange("This Year") },
]);

function onFilterChange(value: Period) {
  const dataStore = useDataStore();
  dataStore.currentPeriod = value;
  update();
}
