using FluentValidation.TestHelper;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validators;

namespace PaymentGateway.Api.Tests;

public class PostPaymentRequestValidatorTests
{
    private readonly PostPaymentRequestValidator _validator = new(new CurrencyService());

    private static PostPaymentRequest ValidRequest() => new()
    {
        CardNumberLastFour = 1234,
        ExpiryMonth = 6,
        ExpiryYear = DateTime.UtcNow.Year + 1,
        Currency = "GBP",
        Amount = 100,
        Cvv = 123
    };

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void ShouldHaveErrorForInvalidExpiryMonth(int month)
    {
        var request = ValidRequest();
        request.ExpiryMonth = month;

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.ExpiryMonth)
            .WithErrorMessage("ExpiryMonth must be between 1 and 12.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void ShouldNotHaveErrorForValidExpiryMonth(int month)
    {
        var request = ValidRequest();
        request.ExpiryMonth = month;

        _validator.TestValidate(request)
            .ShouldNotHaveValidationErrorFor(x => x.ExpiryMonth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldHaveErrorForInvalidAmount(int amount)
    {
        var request = ValidRequest();
        request.Amount = amount;

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than 0.");
    }

    [Fact]
    public void ShouldNotHaveErrorForAmountOfOne()
    {
        var request = ValidRequest();
        request.Amount = 1;

        _validator.TestValidate(request)
            .ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("AUD")]
    [InlineData("")]
    public void ShouldHaveErrorForUnsupportedCurrency(string currency)
    {
        var request = ValidRequest();
        request.Currency = currency;

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency is not supported.");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public void ShouldNotHaveErrorForSupportedCurrency(string currency)
    {
        var request = ValidRequest();
        request.Currency = currency;

        _validator.TestValidate(request)
            .ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void ShouldHaveErrorForExpiredCard()
    {
        var now = DateTime.UtcNow;
        var request = ValidRequest();
        request.ExpiryMonth = now.Month;
        request.ExpiryYear = now.Year - 1;

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.ExpiryMonth)
            .WithErrorMessage("Card has expired.");
    }

    [Fact]
    public void ShouldNotHaveErrorForCardExpiringThisMonth()
    {
        var now = DateTime.UtcNow;
        var request = ValidRequest();
        request.ExpiryMonth = now.Month;
        request.ExpiryYear = now.Year;

        _validator.TestValidate(request)
            .ShouldNotHaveValidationErrorFor(x => x.ExpiryMonth);
    }

    [Fact]
    public void ShouldNotHaveAnyErrorsForValidRequest()
    {
        _validator.TestValidate(ValidRequest())
            .ShouldNotHaveAnyValidationErrors();
    }
}
