namespace OrderSphere.Webhooks.Application.Features.Subscriptions.CreateSubscription;

public sealed class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("A URL is required.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https")
            .WithMessage("Only absolute HTTPS URLs are accepted.");

        RuleFor(x => x.Events)
            .NotEmpty().WithMessage("At least one event type is required.");
    }
}
