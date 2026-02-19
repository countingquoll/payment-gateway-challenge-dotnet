namespace PaymentGateway.Api.Services;

public class CurrencyService
{
    private static readonly IReadOnlyList<string> SupportedCurrencies =
    [
        "USD", "EUR", "GBP"
    ];

    public IReadOnlyList<string> GetCurrencyCodes() => SupportedCurrencies;
}
