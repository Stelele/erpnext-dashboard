import { createRouter, createWebHistory } from "vue-router";

import { authGuard } from "@auth0/auth0-vue";

const OverviewView = () => import("@/views/OverviewView.vue");
const ExpensesView = () => import("@/views/ExpensesView.vue");
const SalesView = () => import("@/views/SalesView.vue");
const StockView = () => import("@/views/StockView.vue");

export const router = createRouter({
  routes: [
    {
      path: "/",
      name: "Overview",
      beforeEnter: authGuard,
      component: OverviewView,
    },
    {
      path: "/expenses",
      name: "Expenses",
      beforeEnter: authGuard,
      component: ExpensesView,
    },
    {
      path: "/sales",
      name: "Sales",
      beforeEnter: authGuard,
      component: SalesView,
    },
    {
      path: "/stock",
      name: "Stock",
      beforeEnter: authGuard,
      component: StockView,
    },
    {
      path: "/:pathMatch(.*)*",
      redirect: "/",
    },
  ],
  history: createWebHistory(),
});
