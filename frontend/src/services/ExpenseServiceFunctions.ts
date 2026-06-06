import type { Expense } from "@/types/Expenses";
import type { JournalEntry } from "@/types/JournalEntry";
import type { ErpNextService, AccountMappings, AccountResponse } from "@/services/ErpNextService";

export interface ExpenseSubmissionResult {
  success: boolean;
  expense: Expense;
  error?: Error;
}

export function addDraftExpense(
  erpNextService: ErpNextService,
  accountMappings: AccountMappings,
  expense: Expense,
): Promise<JournalEntry | undefined> {
  const incomeAccount = accountMappings.incomes["Cash"];
  const expenseAccount = accountMappings.expenses[expense.expenseType];
  return erpNextService.addDraftExpenseJournalEntry(
    expense,
    incomeAccount,
    expenseAccount,
  );
}

export async function bulkAddDraftExpenses(
  erpNextService: ErpNextService,
  accountMappings: AccountMappings,
  expenses: Expense[],
): Promise<ExpenseSubmissionResult[]> {
  const results: ExpenseSubmissionResult[] = [];
  const batchSize = 20;

  for (let i = 0; i < expenses.length; i += batchSize) {
    const batch = expenses.slice(i, i + batchSize);
    const batchResults = await Promise.allSettled(
      batch.map((expense) => addDraftExpense(erpNextService, accountMappings, expense)),
    );

    batchResults.forEach((result, index) => {
      const expense = batch[index];
      if (!expense) return;

      if (result.status === "fulfilled") {
        results.push({ success: true, expense });
      } else {
        results.push({
          success: false,
          expense,
          error: result.reason instanceof Error ? result.reason : new Error(String(result.reason)),
        });
      }
    });
  }

  return results;
}
