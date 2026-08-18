namespace Application.Requests;

public record CreateExpenseTypeRequest(
    string Name,
    string Description
);
