import type { AccountMappings, Expense } from "@/types/Expenses";
import type { ErpNextService } from "./ErpNextService";
import type { JournalEntry } from "@/types/JournalEntry";

export interface ExpenseSubmissionResult {
  success: boolean;
  expense: Expense;
  error?: Error;
}

function addDraftExpense(
  erpNextService: ErpNextService,
  accountMappings: AccountMappings,
  expense: Expense,
): Promise<JournalEntry | undefined> {
  const incomeAccount = accountMappings.income;
  const expenseAccount = accountMappings.expenses[expense.expenseTypeId];

  if (!incomeAccount || !expenseAccount) {
    return Promise.resolve(undefined);
  }

  return erpNextService.addDraftExpenseJournalEntry(
    expense,
    incomeAccount,
    expenseAccount,
  );
}

async function bulkAddDraftExpenses(
  erpNextService: ErpNextService,
  accountMappings: AccountMappings,
  expenses: Expense[],
): Promise<ExpenseSubmissionResult[]> {
  const batchSize = 20;
  const results: ExpenseSubmissionResult[] = [];

  for (let i = 0; i < expenses.length; i += batchSize) {
    const batch = expenses.slice(i, i + batchSize);
    const batchResults = await Promise.allSettled(
      batch.map(async (expense) => {
        const response = await addDraftExpense(
          erpNextService,
          accountMappings,
          expense,
        );
        return { expense, response };
      }),
    );

    for (const result of batchResults) {
      if (result.status === "fulfilled") {
        results.push({
          success: result.value.response !== undefined,
          expense: result.value.expense,
          error: result.value.response === undefined
            ? new Error("Journal entry creation failed")
            : undefined,
        });
      } else {
        results.push({
          success: false,
          expense: (result.reason as any)?.expense ?? {} as Expense,
          error: result.reason as Error,
        });
      }
    }
  }

  return results;
}

export { addDraftExpense, bulkAddDraftExpenses };
