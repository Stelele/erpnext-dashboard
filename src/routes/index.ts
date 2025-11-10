import { createRouter, createWebHistory } from "vue-router";
import OverviewView from "../views/OverviewView.vue";

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
  ],
  history: createWebHistory()
})