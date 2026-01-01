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
                name="expenseType"
                :required="true"
                description="Please select the expense type."
            >
                <UInputMenu
                    v-model="state.expenseType"
                    :items="expenseTypes"
                    class="w-full"
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
                />
            </UFormField>

            <UButton type="submit" class="hover:cursor-pointer">
                Submit
            </UButton>
        </UForm>
    </div>
</template>

<script setup lang="ts">
import { CalendarDate, getLocalTimeZone } from "@internationalized/date";
import * as z from "zod";
import moment from "moment";
import { computed, reactive, shallowRef } from "vue";
import type { Expense, ExpenseType } from "@/types/Expenses";

const emit = defineEmits<{
    onSubmit: [Expense];
}>();

const expenseTypes: ExpenseType[] = [
    "Sekuru",
    "Canteen",
    "Spoiled Meat",
    "Utilities",
    "Consumables",
    "Other",
];
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
    expenseType: z.enum(expenseTypes),
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
    expenseType: "Sekuru",
    amount: 0,
    description: "",
});

const displayDate = computed(() =>
    moment(state.date.toDate(getLocalTimeZone())).format("dddd, DD MMM YYYY"),
);

function onSubmit() {
    const expense: Expense = {
        date: moment(state.date.toDate(getLocalTimeZone())).format(
            "YYYY-MM-DD",
        ),
        expenseType: state.expenseType,
        amount: state.amount,
        description: state.description,
    };

    emit("onSubmit", expense);
}
</script>
