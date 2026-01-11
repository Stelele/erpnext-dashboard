<template>
    <div class="col-span-6">
        <UProgress v-if="props.loading" v-model="value" />
    </div>
</template>

<script setup lang="ts">
import { ref } from "vue";

export interface LoadingBarProps {
    loading: boolean;
}

const props = defineProps<LoadingBarProps>();

const value = ref(null);
</script>
