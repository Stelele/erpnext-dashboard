import { createRouter, createWebHistory } from "vue-router";
import OverviewView from "../views/OverviewView.vue";
import ExpensesView from "../views/ExpensesView.vue";
import { authGuard } from "@auth0/auth0-vue";

export const router = createRouter({
  routes: [
    {
      path: "/",
      component: OverviewView,
      // beforeEnter: authGuard,
    },
    {
      path: "/expenses",
      name: "Expenses",
      component: ExpensesView,
      // beforeEnter: authGuard,
    },
  ],
  history: createWebHistory(),
});
