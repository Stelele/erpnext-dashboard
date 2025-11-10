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
        <UNavigationMenu :items="navItems" orientation="vertical" class="-mx-2.5" />
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
import BasePage from './layouts/BasePage.vue';
import { useAuthStore } from './stores/AuthStore';
import { navItems } from './utils/NavItems';

const authStore = useAuthStore()

</script>
