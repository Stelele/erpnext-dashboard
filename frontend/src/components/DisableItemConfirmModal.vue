<template>
    <UModal
        v-model:open="open"
        title="Disable Item"
        :dismissible="false"
        :fullscreen="fullscreen"
    >
        <template #body>
            <div class="p-4 space-y-4">
                <div class="bg-[var(--ui-bg-elevated)] rounded-lg p-4 space-y-2 text-sm">
                    <div class="flex justify-between">
                        <span class="text-[var(--ui-text-muted)]">Item</span>
                        <span class="font-medium">{{ props.itemName }}</span>
                    </div>
                    <div class="flex justify-between">
                        <span class="text-[var(--ui-text-muted)]">Code</span>
                        <span class="font-medium">{{ props.itemCode }}</span>
                    </div>
                    <div class="flex justify-between">
                        <span class="text-[var(--ui-text-muted)]">Current Quantity</span>
                        <span class="font-medium">{{ props.currentQty }}</span>
                    </div>
                </div>

                <div
                    class="text-sm bg-[var(--ui-bg-elevated)] rounded-lg p-4"
                >
                    <p>This will zero out stock via a reconciliation and disable the item in ERPNext. <strong>This cannot be undone.</strong></p>
                </div>

                <UFormField
                    label="Remarks (optional)"
                    description="Add an optional note for the stock zero-out entry."
                >
                    <UTextarea
                        v-model="remarks"
                        class="w-full"
                        placeholder="Reason for disabling..."
                        :rows="3"
                    />
                </UFormField>

                <div class="flex justify-end gap-2 pt-2">
                    <UButton
                        color="neutral"
                        variant="outline"
                        @click="open = false"
                    >
                        Cancel
                    </UButton>
                    <UButton
                        color="error"
                        @click="onConfirm"
                    >
                        Disable Item
                    </UButton>
                </div>
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";

const props = defineProps<{
    itemCode: string;
    itemName: string;
    currentQty: number;
    fullscreen?: boolean;
}>();

const emit = defineEmits<{
    confirm: [{ remarks?: string }];
}>();

const open = defineModel<boolean>("open", { default: false });
const remarks = ref("");

watch(open, (isOpen) => {
    if (isOpen) {
        remarks.value = "";
    }
});

function onConfirm() {
    emit("confirm", { remarks: remarks.value || undefined });
}
</script>
