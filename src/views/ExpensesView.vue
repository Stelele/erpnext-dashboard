<template>
    <BasePage>
        <div class="w-full h-full flex items-center justify-center">
            <UModal v-model:open="open" title="Create New Expense" :dismissible="false">
                <UButton trailing-icon="i-lucide-plus" size="md" class="hover:cursor-pointer">Add Expense</UButton>

                <template #body>
                    <ExpenseForm @on-submit="onSubmit" />
                </template>
            </UModal>
        </div>
    </BasePage>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import BasePage from '../layouts/BasePage.vue';
import type { Expense } from '../types/Expenses';
import { useDataStore } from '../stores/DataStore';

const open = ref(false)

const toast = useToast()
const dataStore = useDataStore()

async function onSubmit(expense: Expense) {
    toast.add({
        id: 'expense-submit',
        title: 'Submitting expense...',
    })
    open.value = false
    const response = await dataStore.addDraftExpense(expense)
    toast.remove('expense-submit')

    if (response) {
        toast.add({
            title: `Expense submitted successfully: ${response.name}`,
            color: 'success'
        })
    } else {
        toast.add({
            title: 'Failed to submit expense',
            color: 'error',
        })
    }
}
</script>