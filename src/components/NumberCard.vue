<template>
    <UPageCard class=" h-24">
        <CartTitle class="font-bold text-lg -mt-4">{{ props.title }}</CartTitle>
        <div class="w-full flex justify-between items-center -mt-4">
            <CartTitle class="text-4xl">{{ props.value }}</CartTitle>
            <div class="flex items-center gap-1 h-full" :class="{
                'text-green-500': props.direction === 'up',
                'text-red-500': props.direction === 'down'
            }">
                <UIcon :name="icon" class="size-8" />
                <CartTitle class="text-lg">{{ props.percentChange }}%</CartTitle>
            </div>
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import { computed } from 'vue';

export interface NumberCardProps {
    title: string
    value: number
    direction?: 'up' | 'down' | 'flat'
    percentChange: number
}

const icon = computed(() => {
    if (props.direction === 'up') {
        return 'i-lucide-arrow-up-right'
    }

    if (props.direction === 'down') {
        return 'i-lucide-arrow-down-right'
    }

    return 'i-lucide-arrow-right'
})

const props = defineProps<NumberCardProps>()
</script>