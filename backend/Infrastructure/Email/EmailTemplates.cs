using System.Net;

namespace Infrastructure.Email;

public static class EmailTemplates
{
    public static EmailMessage PasswordSetup(string to, string name, string resetUrl) =>
        new(
            to,
            "Set your Njeremoto Dashboard password",
            $"<p>Hi {WebUtility.HtmlEncode(name)},</p>" +
            $"<p>An account has been created for you on the Njeremoto Dashboard.</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(resetUrl)}\">Set your password</a>. This link expires in 24 hours.</p>");

    public static EmailMessage PasswordReset(string to, string name, string resetUrl) =>
        new(
            to,
            "Reset your Njeremoto Dashboard password",
            $"<p>Hi {WebUtility.HtmlEncode(name)},</p>" +
            $"<p>Click the link below to reset your Njeremoto Dashboard password.</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(resetUrl)}\">Reset your password</a>. This link expires in 24 hours.</p>");
}
