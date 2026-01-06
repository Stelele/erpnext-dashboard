import { createRouter, createWebHistory } from "vue-router";

import OverviewView from "@/views/OverviewView.vue";
import ExpensesView from "@/views/ExpensesView.vue";

import { authGuard } from "@auth0/auth0-vue";

export const router = createRouter({
  routes: [
    {
      path: "/expenses",
      name: "Expenses",
      beforeEnter: authGuard,
      component: ExpensesView,
    },
    {
      path: "/",
      name: "Overview",
      beforeEnter: authGuard,
      component: OverviewView,
    },
  ],
  history: createWebHistory(),
});
