using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();
    
    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999),
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostPaymentReturns201WithPaymentResponse()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = _random.Next(1, 12),
            ExpiryYear = DateTime.UtcNow.Year + _random.Next(1, 5),
            Currency = "GBP",
            Amount = _random.Next(1, 10000),
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.NotEqual(Guid.Empty, paymentResponse!.Id);
        Assert.Equal(request.CardNumberLastFour, paymentResponse.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(request.Currency, paymentResponse.Currency);
        Assert.Equal(request.Amount, paymentResponse.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task PostPaymentReturns400ForInvalidExpiryMonth(int invalidMonth)
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = invalidMonth,
            ExpiryYear = _random.Next(2025, 2030),
            Currency = "GBP",
            Amount = _random.Next(1, 10000),
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("AUD")]
    [InlineData("")]
    public async Task PostPaymentReturns400ForUnsupportedCurrency(string unsupportedCurrency)
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = _random.Next(1, 12),
            ExpiryYear = DateTime.UtcNow.Year + _random.Next(1, 5),
            Currency = unsupportedCurrency,
            Amount = _random.Next(1, 10000),
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public async Task PostPaymentReturns201ForSupportedCurrency(string supportedCurrency)
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = _random.Next(1, 12),
            ExpiryYear = DateTime.UtcNow.Year + _random.Next(1, 5),
            Currency = supportedCurrency,
            Amount = _random.Next(1, 10000),
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostPaymentReturns400ForInvalidAmount(int invalidAmount)
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = _random.Next(1, 12),
            ExpiryYear = DateTime.UtcNow.Year + _random.Next(1, 5),
            Currency = "GBP",
            Amount = invalidAmount,
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPaymentReturns201ForAmountOfOne()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = _random.Next(1, 12),
            ExpiryYear = DateTime.UtcNow.Year + _random.Next(1, 5),
            Currency = "GBP",
            Amount = 1,
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostPaymentReturns400ForExpiredCard()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = now.Month,
            ExpiryYear = now.Year - 1,
            Currency = "GBP",
            Amount = _random.Next(1, 10000),
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPaymentReturns201ForCardExpiringThisMonth()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = now.Month,
            ExpiryYear = now.Year,
            Currency = "GBP",
            Amount = _random.Next(1, 10000),
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostPaymentReturns400WithMessageForInvalidExpiryMonth()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = 0,
            ExpiryYear = _random.Next(2025, 2030),
            Currency = "GBP",
            Amount = _random.Next(1, 10000),
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("ExpiryMonth must be between 1 and 12.", problem!.Errors["ExpiryMonth"]);
    }

    [Fact]
    public async Task PostPaymentReturns400WithMessageForInvalidAmount()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = _random.Next(1, 12),
            ExpiryYear = _random.Next(2025, 2030),
            Currency = "GBP",
            Amount = 0,
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Amount must be greater than 0.", problem!.Errors["Amount"]);
    }

    [Fact]
    public async Task PostPaymentReturns400WithMessageForUnsupportedCurrency()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = _random.Next(1, 12),
            ExpiryYear = _random.Next(2025, 2030),
            Currency = "JPY",
            Amount = _random.Next(1, 10000),
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Currency is not supported.", problem!.Errors["Currency"]);
    }

    [Fact]
    public async Task PostPaymentReturns400WithMessageForExpiredCard()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = now.Month,
            ExpiryYear = now.Year - 1,
            Currency = "GBP",
            Amount = _random.Next(1, 10000),
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Card has expired.", problem!.Errors["ExpiryMonth"]);
    }

    [Fact]
    public async Task PostPaymentStoresPaymentRetrievableById()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumberLastFour = _random.Next(1111, 9999),
            ExpiryMonth = _random.Next(1, 12),
            ExpiryYear = _random.Next(2025, 2030),
            Currency = "USD",
            Amount = _random.Next(1, 10000),
            Cvv = _random.Next(100, 999)
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var postResponse = await client.PostAsJsonAsync("/api/Payments", request);
        var created = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();
        var getResponse = await client.GetAsync($"/api/Payments/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }
}