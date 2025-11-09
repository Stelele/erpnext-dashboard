<template>
    <UDashboardSidebar collapsible class="pt-16" :ui="{ footer: 'border-t border-default' }">
        <template #default="{ collapsed }">
            <UNavigationMenu :collapsed="collapsed" :items="navItems" orientation="vertical" />
        </template>
    </UDashboardSidebar>
</template>

<script setup lang="ts">
import { navItems } from '../utils/NavItems';
</script>