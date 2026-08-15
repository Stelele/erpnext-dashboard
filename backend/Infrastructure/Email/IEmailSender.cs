namespace Infrastructure.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
