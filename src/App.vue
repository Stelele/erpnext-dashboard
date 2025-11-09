<template>
  <UApp>
    <UHeader>
      <template #left>
        <div class=" w-full h-full flex gap-2 items-center">
          <img src="/logo.png" class="object-contain h-20" alt="Njeremoto Logo">
        </div>
      </template>
      <UPageHeader title="Njeremoto Enterprise Dashboard" />
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
        <RouterView class="p-2" />
      </UDashboardGroup>
    </UMain>
  </UApp>
</template>

<script setup lang="ts">
import { navItems } from './utils/NavItems';
</script>
