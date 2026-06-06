<template>
    <UModal v-model:open="isOpen" title="Switch Company" :dismissible="true">
        <UButton
            variant="subtle"
            color="neutral"
            class="w-full"
            @click="isOpen = true"
        >
            <div class="flex flex-col w-full">
                <CartTitle class="text-sm font-medium">Switch Company</CartTitle>
            </div>
        </UButton>
        <template #body>
            <CompanySwitcherModalContent v-model="isOpen" />
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref } from "vue";
import CartTitle from "./CartTitle.vue";
import CompanySwitcherModalContent from "./CompanySwitcherModalContent.vue";

const isOpen = ref(false);
</script>
