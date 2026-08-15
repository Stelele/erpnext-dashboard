using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Email;

public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(
                options.Value.Host,
                options.Value.Port,
                options.Value.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                ct);

            if (!string.IsNullOrEmpty(options.Value.Username))
                await client.AuthenticateAsync(options.Value.Username, options.Value.Password, ct);

            var mime = new MimeMessage
            {
                From = { MailboxAddress.Parse(options.Value.From) },
                To = { MailboxAddress.Parse(message.To) },
                Subject = message.Subject,
                Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody()
            };

            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}", message.To);
            throw;
        }
    }
}
