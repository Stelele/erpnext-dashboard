<template>
  <UApp>
    <UHeader>
      <template #left>
        <div class=" w-full h-full flex gap-2 items-center">
          <img src="/logo.png" class="object-contain h-20" alt="Njeremoto Logo">
        </div>
      </template>
      <UPageHeader :title="`${authStore.company} Dashboard`" />
      <template #right>
        <UColorModeButton class="hover:cursor-pointer" />
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
import { onBeforeMount } from 'vue';
import BasePage from './layouts/BasePage.vue';
import { useAuthStore } from './stores/AuthStore';
import { useDataStore } from './stores/DataStore';
import { useNavStore } from './stores/NavStore';
import moment from 'moment';

const authStore = useAuthStore()
const navStore = useNavStore()
const dataStore = useDataStore()

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

</script>
