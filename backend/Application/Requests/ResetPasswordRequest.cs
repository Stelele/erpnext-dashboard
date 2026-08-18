namespace Application.Requests;

public record ResetPasswordRequest(string Token, string NewPassword);
