import type { NavigationMenuItem } from "@nuxt/ui";

export const navItems: NavigationMenuItem[] = [
    { label: 'Overview', icon: 'i-lucide-layout-dashboard', to: '/overview' },
    { label: 'Sales', icon: 'i-lucide-shopping-cart', route: '/sales' },
    { label: 'Stock', icon: 'i-lucide-package', route: '/stock' },
    { label: 'Products', icon: 'i-lucide-tag', route: '/products' },
    { label: 'Customers', icon: 'i-lucide-users', route: '/customers' },
    { label: 'Reports', icon: 'i-lucide-bar-chart-2', route: '/reports' },
    { label: 'Notifications', icon: 'i-lucide-bell', route: '/notifications' },
]