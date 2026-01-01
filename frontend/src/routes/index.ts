import { createRouter, createWebHistory } from "vue-router";

import DashboardView from "../views/Dashboard/DashboardView.vue";
import OverviewView from "../views/Dashboard/OverviewView.vue";
import ExpensesView from "../views/Dashboard/ExpensesView.vue";

import { authGuard } from "@auth0/auth0-vue";

export const router = createRouter({
  routes: [
    {
      path: "/",
      component: DashboardView,
      beforeEnter: authGuard,
      redirect: "/overview",
      children: [
        {
          path: "/expenses",
          name: "Expenses",
          component: ExpensesView,
        },
        {
          path: "/overview",
          name: "Overview",
          component: OverviewView,
        },
      ],
    },
  ],
  history: createWebHistory(),
});
