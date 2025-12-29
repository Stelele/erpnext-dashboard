using Application.Abstractions;
using FluentValidation;

namespace Application.Sites;

public record CreateSiteCommand(
    string Name,
    string Url,
    string Description,
    string ApiToken
    ) : ICommand<Guid>;

public class CreateSiteCommandValidator : AbstractValidator<CreateSiteCommand>
{
    public CreateSiteCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Url)
            .NotEmpty()
            .Must(BeAValidUrl)
            .WithMessage("The URL must be a valid absolute URL.");

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }

    private bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && uriResult.Scheme == Uri.UriSchemeHttps;
    }
}

