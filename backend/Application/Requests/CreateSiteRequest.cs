using FluentValidation;

namespace Application.Requests;

public record CreateSiteRequest(
    string Name,
    string Url,
    string Description,
    string ApiToken
);

public class CreateSiteRequestValidator : AbstractValidator<CreateSiteRequest>
{
    public CreateSiteRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).NotEmpty().Must(BeAValidUrl).WithMessage("The URL must be a valid absolute URL.");
        RuleFor(x => x.Description).MaximumLength(1000);
    }

    private static bool BeAValidUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && uriResult.Scheme == Uri.UriSchemeHttps;
}
