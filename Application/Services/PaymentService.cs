using OnlineShoppingSystem.Domain.Interfaces;
using OnlineShoppingSystem.Domain.Models;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Simulates wallet-based payment processing by debiting the customer's balance.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly DataStore _store;

    public PaymentService(DataStore store)
    {
        _store = store;
    }

    /// <summary>Adds funds to the customer's wallet; amount must be positive.</summary>
    public void AddFunds(Customer customer, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Top-up amount must be greater than zero.");

        customer.WalletBalance += amount;
        _store.Save();
    }

    /// <summary>
    /// Debits the order amount from the customer's wallet.
    /// Throws if the wallet balance is insufficient.
    /// </summary>
    public Payment ProcessPayment(Customer customer, decimal amount, int orderId)
    {
        if (customer.WalletBalance < amount)
            throw new InvalidOperationException(
                $"Insufficient wallet balance. Required: R{amount:F2}, Available: R{customer.WalletBalance:F2}");

        customer.WalletBalance -= amount;

        var payment = new Payment
        {
            Id          = _store.NextPaymentId(),
            OrderId     = orderId,
            CustomerId  = customer.Id,
            Amount      = amount,
            Status      = PaymentStatus.Success,
            ProcessedAt = DateTime.Now
        };

        _store.Payments.Add(payment);
        _store.Save();
        return payment;
    }

    public List<Payment> GetPaymentHistory(int customerId) =>
        _store.Payments
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.ProcessedAt)
            .ToList();
}
