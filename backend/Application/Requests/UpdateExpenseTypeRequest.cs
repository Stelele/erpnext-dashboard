namespace Application.Requests;

public record UpdateExpenseTypeRequest(
    string Name,
    string Description
);
