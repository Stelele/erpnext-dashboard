<template>
    <div class="w-full h-full grid grid-cols-1 gap-4 p-4">
        <UForm
            :schema="schema"
            :state="state"
            class="space-y-4"
            @submit="onSubmit"
        >
            <UFormField
                label="Date"
                name="date"
                :required="true"
                description="Please select the date the expense occured."
            >
                <UPopover>
                    <UButton
                        color="neutral"
                        variant="subtle"
                        icon="i-lucide-calendar"
                        class="w-full"
                        :disabled="loading"
                    >
                        {{ displayDate }}
                    </UButton>

                    <template #content>
                        <UCalendar v-model="state.date" class="p-2" />
                    </template>
                </UPopover>
            </UFormField>

            <UFormField
                label="Expense Type"
                name="expenseTypeId"
                :required="true"
                description="Please select the expense type."
            >
                <UInputMenu
                    v-model="state.expenseTypeId"
                    :items="mappedItems"
                    value-key="expenseTypeId"
                    label-key="expenseTypeName"
                    class="w-full"
                    :disabled="loading"
                />
            </UFormField>

            <UFormField
                label="Amount"
                name="amount"
                :required="true"
                description="Please enter the amount of the expense."
            >
                <UInputNumber
                    v-model="state.amount"
                    class="w-full"
                    :step="0.01"
                    :disabled="loading"
                />
            </UFormField>

            <UFormField
                label="Description"
                name="description"
                :required="true"
                description="Please enter a description of the expense."
            >
                <UTextarea
                    v-model="state.description"
                    class="w-full"
                    :rows="6"
                    :disabled="loading"
                />
            </UFormField>

            <UButton type="submit" class="hover:cursor-pointer" :loading="loading" :disabled="loading">
                Submit
            </UButton>
        </UForm>
    </div>
</template>

<script setup lang="ts">
import { CalendarDate, getLocalTimeZone } from "@internationalized/date";
import * as z from "zod";
import moment from "moment";
import { computed, reactive, shallowRef, watch } from "vue";
import type { Expense, CompanyExpenseMapping } from "@/types/Expenses";

const props = defineProps<{
    mappings: CompanyExpenseMapping[];
    loading?: boolean;
}>();

const emit = defineEmits<{
    onSubmit: [Expense];
}>();

const configuredMappings = computed(() =>
    props.mappings.filter(
        (m) => m.erpnextAccountName && m.erpnextAccountName.trim() !== "",
    ),
);

const mappedItems = computed(() =>
    configuredMappings.value.map((m) => ({
        expenseTypeId: m.expenseTypeId,
        expenseTypeName: m.expenseTypeName,
    })),
);

const expenseTypeIds = computed(() =>
    configuredMappings.value.map((m) => m.expenseTypeId) as [string, ...string[]],
);

const schema = z.object({
    date: z
        .object({
            year: z.number(),
            month: z.number().min(1).max(12),
            day: z.number().min(1).max(31),
        })
        .transform(({ year, month, day }) =>
            shallowRef(new CalendarDate(year, month, day)),
        ),
    expenseTypeId: z.enum(expenseTypeIds.value),
    amount: z.number().gt(0),
    description: z.string().min(10),
});
type Schema = z.output<typeof schema>;

const state = reactive<Schema>({
    date: shallowRef(
        new CalendarDate(
            moment().year(),
            moment().month() + 1,
            moment().date(),
        ),
    ),
    expenseTypeId: "",
    amount: 0,
    description: "",
});

watch(
    () => props.mappings,
    (newMappings) => {
        const configured = newMappings.filter(
            (m) => m.erpnextAccountName && m.erpnextAccountName.trim() !== "",
        );
        if (configured.length > 0) {
            state.expenseTypeId = configured[0].expenseTypeId;
        }
    },
    { deep: true },
);

const displayDate = computed(() =>
    moment(state.date.toDate(getLocalTimeZone())).format("dddd, DD MMM YYYY"),
);

function onSubmit() {
    const expense: Expense = {
        date: moment(state.date.toDate(getLocalTimeZone())).format(
            "YYYY-MM-DD",
        ),
        expenseTypeId: state.expenseTypeId,
        amount: state.amount,
        description: state.description,
    };

    emit("onSubmit", expense);
}
</script>
