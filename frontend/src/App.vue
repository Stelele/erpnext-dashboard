<template>
    <RouterView />
</template>

<script setup lang="ts">
import { onBeforeMount } from "vue";
import { useAuthStore } from "./stores/AuthStore";
import { update } from "./utils/UpdateData";

const authStore = useAuthStore();

onBeforeMount(async () => {
    await authStore.update();
    update();
});
</script>
