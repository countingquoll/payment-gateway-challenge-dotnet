using FluentValidation;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Validators;

public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
{
    public PostPaymentRequestValidator(CurrencyService currencyService)
    {
        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12)
            .WithMessage("ExpiryMonth must be between 1 and 12.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Currency)
            .Must(currency => currencyService.GetCurrencyCodes().Contains(currency))
            .WithMessage("Currency is not supported.");

        RuleFor(x => x)
            .Must(x =>
            {
                var now = DateTime.UtcNow;
                var expiry = new DateTime(x.ExpiryYear, Math.Clamp(x.ExpiryMonth, 1, 12), 1);
                return expiry >= new DateTime(now.Year, now.Month, 1);
            })
            .WithName("ExpiryMonth")
            .WithMessage("Card has expired.");
    }
}
