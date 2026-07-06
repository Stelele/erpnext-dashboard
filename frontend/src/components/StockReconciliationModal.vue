<template>
    <div class="p-4">
        <div v-if="!showConfirm">
            <UForm
                :schema="schema"
                :state="state"
                class="space-y-4"
                @submit="onFormSubmit"
            >
                <UFormField
                    label="Remarks (optional)"
                    name="remarks"
                    description="Add an optional note for the reconciliation entry."
                >
                    <UTextarea
                        v-model="state.remarks"
                        class="w-full"
                        :rows="3"
                        :disabled="submitting"
                    />
                </UFormField>

                <!-- Desktop -->
                <div class="hidden md:block">
                    <div class="mt-4">
                        <div
                            class="grid grid-cols-[2fr_1fr_1fr_100px_auto] gap-2 mb-2 text-xs font-medium text-[var(--ui-text-dimmed)] px-1"
                        >
                            <span>Item</span>
                            <span>Current Qty</span>
                            <span>Corrected Qty</span>
                            <span>Difference</span>
                            <span></span>
                        </div>
                        <div
                            v-for="(item, idx) in state.items"
                            :key="idx"
                            class="grid grid-cols-[2fr_1fr_1fr_100px_auto] gap-2 mb-2"
                        >
                            <UInputMenu
                                v-model="itemSelections[idx] as any"
                                v-model:search-term="itemSearchTerms[idx]"
                                :items="itemOptions[idx]"
                                value-key="item_code"
                                label-key="item_name"
                                :placeholder="itemSelections[idx]?.item_name || 'Search item...'"
                                class="w-full"
                                :ignore-filter="true"
                                :disabled="submitting"
                                @update:open="(open: boolean) => { if (open) onItemOpen(idx) }"
                                @update:model-value="() => onItemPicked(idx)"
                            />
                            <div class="flex items-center text-sm">
                                {{ formatNumber(item.current_qty, "decimal") }}
                            </div>
                            <UInput
                                v-model="item.corrected_qty"
                                type="number"
                                :min="0"
                                :step="0.01"
                                class="w-full"
                                :disabled="submitting"
                            />
                            <div
                                class="flex items-center justify-end text-sm font-medium"
                                :class="diffClass(idx)"
                            >
                                {{ formatDiff(idx) }}
                            </div>
                            <UButton
                                color="error"
                                variant="ghost"
                                icon="i-lucide-x"
                                size="sm"
                                :disabled="submitting || state.items.length <= 1"
                                @click="removeItem(idx)"
                            />
                        </div>
                        <UButton
                            variant="outline"
                            icon="i-lucide-plus"
                            size="sm"
                            class="w-full"
                            :disabled="submitting"
                            @click="addItem"
                        >
                            Add Item
                        </UButton>
                    </div>
                </div>

                <!-- Mobile -->
                <div class="md:hidden space-y-4">
                    <div>
                        <div class="flex items-center justify-between mb-3">
                            <span class="text-sm font-medium">Items</span>
                            <UButton
                                variant="outline"
                                icon="i-lucide-plus"
                                size="xs"
                                :disabled="submitting"
                                @click="addItem"
                            >
                                Add
                            </UButton>
                        </div>
                        <div class="space-y-3">
                            <UCard
                                v-for="(item, idx) in state.items"
                                :key="idx"
                            >
                                <template #header>
                                    <div class="flex justify-between items-center gap-2">
                                        <USelectMenu
                                            v-model="itemSelections[idx] as any"
                                            v-model:search-term="itemSearchTerms[idx]"
                                            :items="itemOptions[idx]"
                                            value-key="item_code"
                                            label-key="item_name"
                                            :placeholder="itemSelections[idx]?.item_name || 'Search item...'"
                                            :ignore-filter="true"
                                            :search-input="{ placeholder: 'Search items...' }"
                                            class="flex-1"
                                            :disabled="submitting"
                                            @update:open="(open: boolean) => { if (open) onItemOpen(idx) }"
                                            @update:model-value="() => onItemPicked(idx)"
                                        />
                                        <UButton
                                            color="error"
                                            variant="ghost"
                                            icon="i-lucide-x"
                                            size="sm"
                                            :disabled="submitting || state.items.length <= 1"
                                            @click="removeItem(idx)"
                                        />
                                    </div>
                                </template>
                                <div class="grid grid-cols-3 gap-2">
                                    <UFormField label="Current Qty" size="xs">
                                        <div class="h-9 flex items-center text-sm">
                                            {{ formatNumber(item.current_qty, "decimal") }}
                                        </div>
                                    </UFormField>
                                    <UFormField label="Corrected Qty" size="xs">
                                        <UInput
                                            v-model="item.corrected_qty"
                                            type="number"
                                            :min="0"
                                            :step="0.01"
                                            :disabled="submitting"
                                        />
                                    </UFormField>
                                    <UFormField label="Diff" size="xs">
                                        <div
                                            class="h-9 flex items-center text-sm font-medium"
                                            :class="diffClass(idx)"
                                        >
                                            {{ formatDiff(idx) }}
                                        </div>
                                    </UFormField>
                                </div>
                            </UCard>
                        </div>
                    </div>
                </div>

                <div
                    class="flex justify-end gap-2 pt-4 border-t border-[var(--ui-border)]"
                >
                    <UButton
                        type="submit"
                        color="primary"
                        :loading="submitting"
                        :disabled="submitting"
                    >
                        Review & Submit
                    </UButton>
                </div>
            </UForm>
        </div>

        <div v-else class="space-y-4">
            <div class="text-sm text-[var(--ui-text)]">
                <p class="font-medium text-base mb-2">
                    Confirm Stock Reconciliation
                </p>
                <p>
                    This will create a Stock Reconciliation entry in ERPNext.
                    <strong>Stock levels will be updated immediately.</strong>
                </p>
            </div>

            <div
                class="bg-[var(--ui-bg-elevated)] rounded-lg p-5 space-y-4 text-sm"
            >
                <div v-if="validItems.length > 0">
                    <p
                        class="text-xs font-semibold text-[var(--ui-text-muted)] uppercase tracking-wide mb-3"
                    >
                        Items
                    </p>
                    <div class="space-y-1.5">
                        <div
                            v-for="item in validItems"
                            :key="item.item_code"
                            class="flex justify-between"
                        >
                            <span class="font-medium">{{ item.item_name }}</span>
                            <span class="text-[var(--ui-text-muted)]">
                                {{ formatNumber(item.current_qty, "decimal") }} &rarr;
                                {{ formatNumber(item.corrected_qty, "decimal") }}
                                ({{ itemDiff(item) }})
                            </span>
                        </div>
                    </div>
                </div>

                <template v-if="state.remarks">
                    <USeparator />
                    <div>
                        <p
                            class="text-xs font-semibold text-[var(--ui-text-muted)] uppercase tracking-wide mb-3"
                        >
                            Remarks
                        </p>
                        <p>{{ state.remarks }}</p>
                    </div>
                </template>
            </div>

            <div class="flex justify-end gap-2">
                <UButton
                    color="neutral"
                    variant="outline"
                    @click="showConfirm = false"
                >
                    Back
                </UButton>
                <UButton
                    color="primary"
                    :loading="submitting"
                    @click="confirmSubmit"
                >
                    Confirm & Submit
                </UButton>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import * as z from "zod";
import { computed, reactive, ref, watch, onMounted } from "vue";
import type { ItemOption } from "@/services/ErpNextService";
import { ErpNextService } from "@/services/ErpNextService";
import { useStockDataStore } from "@/stores/StockDataStore";
import { formatNumber } from "@/utils/FormatNumber";

const emit = defineEmits<{
    onSubmit: [
        payload: { items: { item_code: string; qty: number; valuation_rate: number }[]; remarks?: string },
    ];
}>();

const props = defineProps<{ loading?: boolean }>();
const submitting = computed(() => props.loading ?? false);
watch(submitting, (v) => {
    if (v) showConfirm.value = false;
});
const erpnext = new ErpNextService();
const stockStore = useStockDataStore();
const showConfirm = ref(false);

interface ReconItem {
    item_code: string;
    item_name: string;
    current_qty: number;
    corrected_qty: number;
    valuation_rate: number;
}

const state = reactive({
    remarks: "",
    items: [
        { item_code: "", item_name: "", current_qty: 0, corrected_qty: 0, valuation_rate: 0 },
    ] as ReconItem[],
});

const schema = z.object({
    remarks: z.string().optional(),
    items: z
        .array(
            z.object({
                item_code: z.string().min(1, "Item is required"),
                item_name: z.string().optional(),
                current_qty: z.number(),
                corrected_qty: z.number(),
            }),
        )
        .min(1, "At least one item required"),
});

const validItems = computed(() => state.items.filter((i) => i.item_code));

const itemSelections = ref<(ItemOption | null)[]>([null]);
const itemOptions = ref<ItemOption[][]>([[]]);
const itemSearchTerms = ref<string[]>([""]);
const itemTimers: Record<number, ReturnType<typeof setTimeout>> = {};

function formatDiff(idx: number) {
    const item = state.items[idx];
    if (!item) return "";
    const diff = item.corrected_qty - item.current_qty;
    const sign = diff >= 0 ? "+" : "";
    return `${sign}${formatNumber(diff, "decimal")}`;
}

function itemDiff(item: ReconItem) {
    const diff = item.corrected_qty - item.current_qty;
    const sign = diff >= 0 ? "+" : "";
    return `${sign}${formatNumber(diff, "decimal")}`;
}

function diffClass(idx: number) {
    const item = state.items[idx];
    if (!item) return "";
    const diff = item.corrected_qty - item.current_qty;
    if (diff > 0) return "text-green-500";
    if (diff < 0) return "text-red-500";
    return "text-[var(--ui-text-muted)]";
}

function watchRow(idx: number) {
    watch(
        () => itemSearchTerms.value[idx],
        (term) => {
            if (itemTimers[idx]) clearTimeout(itemTimers[idx]);
            itemTimers[idx] = setTimeout(async () => {
                try {
                    const results = await erpnext.searchItems(term || "");
                    if (results) itemOptions.value[idx] = results;
                } catch {
                    /* ignore */
                }
            }, 300);
        },
    );
}

watchRow(0);

onMounted(async () => {
    try {
        const items = await erpnext.searchItems("");
        if (items) itemOptions.value[0] = items;
    } catch {
        /* ignore */
    }
});

async function onItemOpen(idx: number) {
    if (!itemOptions.value[idx]?.length) {
        try {
            const results = await erpnext.searchItems("");
            if (results) itemOptions.value[idx] = results;
        } catch {
            /* ignore */
        }
    }
}

function onItemPicked(idx: number) {
    const itemCode = itemSelections.value[idx] as unknown as string;
    if (!itemCode) return;
    const sel = itemOptions.value[idx]?.find((i) => i.item_code === itemCode);
    if (!sel) return;
    const target = state.items[idx];
    if (!target) return;
    target.item_code = sel.item_code;
    target.item_name = sel.item_name;

    const stockRow = stockStore.stockTableData.find(
        (r) => r.item_code === sel.item_code,
    );
    if (stockRow) {
        target.current_qty = stockRow.real_qty;
        target.corrected_qty = stockRow.real_qty;
        target.valuation_rate = stockRow.buying_price || 1;
    }
}

function addItem() {
    state.items.push({
        item_code: "",
        item_name: "",
        current_qty: 0,
        corrected_qty: 0,
        valuation_rate: 0,
    });
    itemSelections.value.push(null);
    itemOptions.value.push([]);
    itemSearchTerms.value.push("");
    const newIdx = state.items.length - 1;
    watchRow(newIdx);
    onItemOpen(newIdx);
}

function removeItem(idx: number) {
    if (state.items.length > 1) {
        state.items.splice(idx, 1);
        itemSelections.value.splice(idx, 1);
        itemOptions.value.splice(idx, 1);
        itemSearchTerms.value.splice(idx, 1);
        if (itemTimers[idx]) {
            clearTimeout(itemTimers[idx]);
            delete itemTimers[idx];
        }
    }
}

function onFormSubmit() {
    showConfirm.value = true;
}

function confirmSubmit() {
    emit("onSubmit", {
        items: validItems.value.map((i) => ({
            item_code: i.item_code,
            qty: i.corrected_qty,
            valuation_rate: i.valuation_rate || 1,
        })),
        remarks: state.remarks || undefined,
    });
}
</script>
