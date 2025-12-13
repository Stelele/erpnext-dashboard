<template>
  <UApp>
    <UHeader>
      <template #left>
        <div class=" w-full h-full flex gap-2 items-center p-1">
          <img src="/logo.png" class="object-contain h-20" alt="Njeremoto Logo">
        </div>
      </template>
      <UPageHeader :title="`${authStore.company} Dashboard`" />
      <template #right>
        <div class="flex gap-1 m-1">
          <UDropdownMenu :items="filterItems" class="w-full visible lg:hidden">
            <UButton variant="subtle" color="neutral" class="w-full">
              <div class="flex flex-col">
                <CartTitle class="text-md font-bold">{{ dataStore.currentPeriod }}</CartTitle>
                <CartTitle class="text-sm">{{ dateRange }}</CartTitle>
              </div>
            </UButton>
          </UDropdownMenu>
          <UButton @click="refreshData" icon="i-lucide-refresh-ccw" size="md" color="neutral" variant="ghost"
            class="hover:cursor-pointer" />
          <UColorModeButton class="hover:cursor-pointer" />
        </div>
      </template>
      <template #body>
        <UNavigationMenu :items="navStore.navItems" orientation="vertical" class="-mx-2.5" />
      </template>
    </UHeader>
    <UMain>
      <UDashboardGroup>
        <SideBar />
        <BasePage>
          <RouterView class="p-2" />
        </BasePage>
      </UDashboardGroup>
    </UMain>
  </UApp>
</template>

<script setup lang="ts">
import { computed, onBeforeMount } from 'vue';
import BasePage from './layouts/BasePage.vue';
import { useAuthStore } from './stores/AuthStore';
import { useDataStore } from './stores/DataStore';
import { useNavStore } from './stores/NavStore';
import { filterItems } from './utils/MenuItems';
import moment from 'moment';

const authStore = useAuthStore()
const navStore = useNavStore()
const dataStore = useDataStore()

const dateRange = computed(() => {
  if (['Today', 'Yesterday'].includes(dataStore.currentPeriod)) {
    return moment().format('DD MMM YY')
  }

  return `${moment(dataStore.dateRange.start).format('DD MMM YY')} - ${moment(dataStore.dateRange.end).format('DD MMM YY')}`
})

moment.updateLocale('en', {
  week: {
    dow: 1, // Monday is the first day of the week.
  }
})

onBeforeMount(() => {
  dataStore.getData(dataStore.currentPeriod)
  setInterval(() => {
    dataStore.getData(dataStore.currentPeriod)
  }, 1000 * 60 * 5)
})

function refreshData() {
  dataStore.getData(dataStore.currentPeriod)
}

</script>
