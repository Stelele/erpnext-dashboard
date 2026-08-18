namespace Infrastructure.Email;

public class HttpEmailOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}