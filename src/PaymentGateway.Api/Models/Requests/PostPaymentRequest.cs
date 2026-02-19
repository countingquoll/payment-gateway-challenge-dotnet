using System.ComponentModel.DataAnnotations;

using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest : IValidatableObject
{
    public int CardNumberLastFour { get; set; }

    [Range(1, 12, ErrorMessage = "ExpiryMonth must be between 1 and 12.")]
    public int ExpiryMonth { get; set; }

    public int ExpiryYear { get; set; }

    public string Currency { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public int Amount { get; set; }

    public int Cvv { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currencyService = (CurrencyService)validationContext.GetService(typeof(CurrencyService))!;
        if (!currencyService.GetCurrencyCodes().Contains(Currency))
            yield return new ValidationResult(
                "Currency is not supported.",
                [nameof(Currency)]);

        var now = DateTime.UtcNow;
        var expiry = new DateTime(ExpiryYear, Math.Clamp(ExpiryMonth, 1, 12), 1);

        if (expiry < new DateTime(now.Year, now.Month, 1))
            yield return new ValidationResult(
                "Card has expired.",
                [nameof(ExpiryMonth), nameof(ExpiryYear)]);
    }
}