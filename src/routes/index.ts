import { createRouter, createWebHistory } from "vue-router";

export const router = createRouter({
  routes: [
    {
      path: '/',
      redirect: '/overview'

    },
    {
      path: '/overview',
      name: 'Overview',
      component: () => import('../views/OverviewView.vue'),
    },
    {
      path: '/expenses',
      name: 'Expenses',
      component: () => import('../views/ExpensesView.vue'),
    }
  ],
  history: createWebHistory()
})