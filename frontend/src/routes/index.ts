import { createRouter, createWebHistory } from "vue-router";
import OverviewView from "../views/OverviewView.vue";
import ExpensesView from "../views/ExpensesView.vue";

export const router = createRouter({
  routes: [
    {
      path: '/',
      redirect: '/overview'

    },
    {
      path: '/overview',
      name: 'Overview',
      component: OverviewView,
    },
    {
      path: '/expenses',
      name: 'Expenses',
      component: ExpensesView,
    }
  ],
  history: createWebHistory()
})