import { createRouter, createWebHistory } from "vue-router";

import { authGuard } from "@/guards/auth";

const LoginView = () => import("@/views/LoginView.vue");
const ResetPasswordView = () => import("@/views/ResetPasswordView.vue");
const OverviewView = () => import("@/views/OverviewView.vue");
const ExpensesView = () => import("@/views/ExpensesView.vue");
const SalesView = () => import("@/views/SalesView.vue");
const StockView = () => import("@/views/StockView.vue");

export const router = createRouter({
  routes: [
    {
      path: "/login",
      name: "Login",
      component: LoginView,
      meta: { public: true },
      beforeEnter: authGuard,
    },
    {
      path: "/reset-password",
      name: "ResetPassword",
      component: ResetPasswordView,
      meta: { public: true },
      beforeEnter: authGuard,
    },
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