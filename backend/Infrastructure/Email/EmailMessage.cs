namespace Infrastructure.Email;

public record EmailMessage(string To, string Subject, string HtmlBody);
