using OAS.Contracts.Sales.Returns;

namespace OAS.Application.Sales.Returns.Services;

/// <summary>
/// Allocates each posted return first against the invoice's unpaid portion.
/// Only the excess creates a customer-credit entitlement. This is intentionally
/// separate from payment and refund operations; it never writes a GL journal.
/// </summary>
public static class CustomerReturnCreditCalculator
{
    public sealed record PostedReturn(
        Guid Id,
        string Code,
        DateOnly ReturnDate,
        DateTimeOffset? PostedAtUtc,
        decimal Amount,
        decimal HistoricalBaseAmount);

    public static IReadOnlyList<CustomerReturnCreditItemDto> Calculate(
        Guid customerId,
        Guid invoiceId,
        string invoiceCode,
        Guid currencyId,
        string currencyCode,
        Guid baseCurrencyId,
        string baseCurrencyCode,
        decimal originalInvoiceAmount,
        decimal paymentsAppliedToInvoiceAmount,
        byte baseDecimalPlaces,
        IReadOnlyCollection<PostedReturn> returns)
    {
        if (customerId == Guid.Empty || invoiceId == Guid.Empty)
            throw new ArgumentException("Customer and sales invoice IDs are required.");
        if (originalInvoiceAmount < 0m || paymentsAppliedToInvoiceAmount < 0m)
            throw new ArgumentOutOfRangeException(nameof(originalInvoiceAmount));
        if (baseDecimalPlaces > 6)
            throw new ArgumentOutOfRangeException(nameof(baseDecimalPlaces));
        if (paymentsAppliedToInvoiceAmount > originalInvoiceAmount + 0.0001m)
            throw new InvalidOperationException("Invoice payment allocations exceed the original invoice amount.");

        // A return reduces the unpaid original invoice before its excess can
        // become cash-refundable/customer credit. This avoids issuing refunds on
        // amounts which simply cancelled an unpaid receivable.
        decimal remainingUnpaid = Math.Max(0m, originalInvoiceAmount - paymentsAppliedToInvoiceAmount);
        var result = new List<CustomerReturnCreditItemDto>();
        foreach (var source in returns
            .OrderBy(x => x.PostedAtUtc ?? DateTimeOffset.MaxValue)
            .ThenBy(x => x.ReturnDate)
            .ThenBy(x => x.Id))
        {
            if (source.Amount <= 0m || source.HistoricalBaseAmount < 0m)
                throw new InvalidOperationException("Posted return amounts must be positive and base amounts nonnegative.");

            var appliedAgainstInvoice = Math.Min(source.Amount, remainingUnpaid);
            remainingUnpaid -= appliedAgainstInvoice;
            var excess = source.Amount - appliedAgainstInvoice;
            var historicalBase = Math.Round(
                source.HistoricalBaseAmount * (excess / source.Amount),
                baseDecimalPlaces,
                MidpointRounding.AwayFromZero);

            result.Add(new CustomerReturnCreditItemDto(
                source.Id, source.Code, invoiceId, invoiceCode, customerId,
                source.ReturnDate, currencyId, currencyCode,
                baseCurrencyId, baseCurrencyCode,
                source.Amount, appliedAgainstInvoice, excess, historicalBase));
        }

        if (returns.Sum(x => x.Amount) > originalInvoiceAmount + 0.0001m)
            throw new InvalidOperationException("Posted returns exceed the original invoice amount.");

        return result;
    }
}
