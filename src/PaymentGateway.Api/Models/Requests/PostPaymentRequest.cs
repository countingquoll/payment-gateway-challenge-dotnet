using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    public int CardNumberLastFour { get; set; }

    [Range(1, 12)]
    public int ExpiryMonth { get; set; }

    public int ExpiryYear { get; set; }

    public string Currency { get; set; }

    public int Amount { get; set; }

    public int Cvv { get; set; }
}