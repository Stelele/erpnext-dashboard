import type { NavigationMenuItem } from "@nuxt/ui"
import { defineStore } from "pinia"
import { ref } from "vue"

export const useNavStore = defineStore('navStore', () => {
    const navItems = ref<NavigationMenuItem[]>([
        { label: 'Overview', icon: 'i-lucide-layout-dashboard', to: '/overview' },
        { label: 'Sales', icon: 'i-lucide-shopping-cart', route: '/sales' },
        { label: 'Stock', icon: 'i-lucide-package', route: '/stock' },
        { label: 'Products', icon: 'i-lucide-tag', route: '/products' },
        { label: 'Customers', icon: 'i-lucide-users', route: '/customers' },
        { label: 'Reports', icon: 'i-lucide-bar-chart-2', route: '/reports' },
        { label: 'Notifications', icon: 'i-lucide-bell', route: '/notifications' },
    ])

    return {
        navItems,
    }
})