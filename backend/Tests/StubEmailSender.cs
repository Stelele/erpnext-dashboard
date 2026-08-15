using Infrastructure.Email;

namespace Tests;

public class StubEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}
