import { createRouter, createWebHistory } from "vue-router";

import OverviewView from "@/views/OverviewView.vue";
import ExpensesView from "@/views/ExpensesView.vue";
import SalesView from "@/views/SalesView.vue";

import { authGuard } from "@auth0/auth0-vue";

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
      path: "/:pathMatch(.*)*",
      redirect: "/",
    },
  ],
  history: createWebHistory(),
});
