<template>
    <UPageCard
        class="h-24 col-span-2"
        :title="props.title"
        :ui="{
            container: 'gap-y-1.5',
            wrapper: 'items-start',
            leading:
                'p-2.5 rounded-full bg-primary/10 ring ring-inset ring-primary/25 flex-col',
            title: 'font-bold text-xs uppercase',
        }"
    >
        <div class="flex items-center gap-2">
            <div class="w-full flex justify-between items-center">
                <CartTitle class="text-3xl">{{ formatedValue }}</CartTitle>
                <div
                    v-if="props.direction && !isNaN(props.percentChange)"
                    class="flex items-center gap-1 h-full"
                    :style="{
                        color: trendColor,
                    }"
                >
                    <UIcon :name="icon" class="size-8" />
                    <CartTitle class="text-md"
                        >{{ props.percentChange }}%</CartTitle
                    >
                </div>
            </div>
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useColorMode } from "@vueuse/core";

export type ChangeDirection = "up" | "down" | "flat";
export interface NumberCardProps {
    title: string;
    value: number;
    direction?: ChangeDirection;
    percentChange: number;
}
const props = defineProps<NumberCardProps>();
const colorMode = useColorMode();

const formatedValue = computed(() => {
    if (Number.isNaN(props.value)) {
        return "-";
    }

    if (Math.abs(props.value) - Math.floor(Math.abs(props.value)) === 0) {
        return props.value;
    }

    return props.value.toFixed(2);
});

const icon = computed(() => {
    if (props.direction === "up") {
        return "i-lucide-arrow-up-right";
    }

    if (props.direction === "down") {
        return "i-lucide-arrow-down-right";
    }

    return "i-lucide-arrow-right";
});

const trendColor = computed(() => {
    if (props.direction === "up") {
        return colorMode.value === "dark" ? "#00DC82" : "#00DC82";
    }

    if (props.direction === "down") {
        return colorMode.value === "dark" ? "#F43F5E" : "#F87171";
    }

    return colorMode.value === "dark" ? "#64748B" : "#94A3B8";
});
</script>
