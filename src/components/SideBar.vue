<template>
    <UDashboardSidebar collapsible class="pt-16" :ui="{ footer: 'border-t border-default' }">
        <template #default="{ collapsed }">
            <UNavigationMenu :collapsed="collapsed" :items="navStore.navItems" orientation="vertical" />
        </template>
    </UDashboardSidebar>
</template>

<script setup lang="ts">
import { useNavStore } from '../stores/NavStore';

const navStore = useNavStore()
</script>