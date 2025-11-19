<template>
    <UDashboardSidebar collapsible class="pt-16" :ui="{ footer: 'border-t border-default' }">
        <template #default="{ collapsed }">
            <UNavigationMenu :collapsed="collapsed" :items="navStore.navItems" orientation="vertical" />
        </template>
        <template #footer="{ collapsed }">
            <div class="flex flex-col gap-1">
                <UDropdownMenu :collapsed="collapsed" :items="filterItems" class="w-full">
                    <UButton variant="subtle" color="neutral" class="w-full">
                        <div class="flex flex-col w-full">
                            <CartTitle class="text-lg font-bold">{{ dataStore.currentPeriod }}</CartTitle>
                            <CartTitle class="text-sm">{{ dateRange }}
                            </CartTitle>
                        </div>
                    </UButton>
                </UDropdownMenu>
                <CartTitle class="text-sm">Last Refresh: {{ dataStore.lastRefresh }}</CartTitle>
            </div>
        </template>
    </UDashboardSidebar>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useDataStore } from '../stores/DataStore';
import { useNavStore } from '../stores/NavStore';
import moment from 'moment';
import type { DropdownMenuItem } from '@nuxt/ui';
import type { Period } from '../utils/PeriodUtilities';

const navStore = useNavStore()
const dataStore = useDataStore()

const dateRange = computed(() => {
    if (['Today', 'Yesterday'].includes(dataStore.currentPeriod)) {
        return moment().format('DD MMM YY')
    }

    return `${moment(dataStore.dateRange.start).format('DD MMM YY')} - ${moment(dataStore.dateRange.end).format('DD MMM YY')}`
})

const filterItems = computed<DropdownMenuItem[]>(() => [
    { label: 'Today', onSelect: () => onFilterChange('Today') },
    { label: 'This Week', onSelect: () => onFilterChange('This Week') },
    { label: 'This Month', onSelect: () => onFilterChange('This Month') },
    { label: 'This Quarter', onSelect: () => onFilterChange('This Quarter') },
    { label: 'This Year', onSelect: () => onFilterChange('This Year') },
])

function onFilterChange(value: Period) {
    dataStore.currentPeriod = value
    dataStore.getData(value)
}
</script>