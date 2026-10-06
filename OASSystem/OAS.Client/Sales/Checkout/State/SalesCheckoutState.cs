using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Enums;

namespace OAS.Client.Sales.Checkout.State;

public sealed class SalesCheckoutState
{
    public SalesCheckoutContextDto? Context { get; private set; }
    public CheckoutCustomerOrderResultDto? Result { get; private set; }
    public SalesPaymentPlan PaymentPlan { get; set; } = SalesPaymentPlan.FullNow;
    public bool IsBusy { get; set; }
    public string? Error { get; set; }
    public string? Success { get; set; }
    public string IdempotencyKey { get; private set; } = Guid.NewGuid().ToString("N");

    public void SetContext(SalesCheckoutContextDto? context)
    {
        Context = context;
        PaymentPlan = context?.Order.PaymentPlan ?? SalesPaymentPlan.FullNow;
        Result = null;
        Error = null;
        Success = null;
        IdempotencyKey = Guid.NewGuid().ToString("N");
    }

    public void SetResult(CheckoutCustomerOrderResultDto? result)
    {
        Result = result;
        if (result is not null)
        {
            Success = string.IsNullOrWhiteSpace(result.InvoiceCode)
                ? $"تم تنفيذ العملية للطلب {result.OrderCode}."
                : $"تم تنفيذ العملية للطلب {result.OrderCode} وإنشاء الفاتورة {result.InvoiceCode}.";
            IdempotencyKey = Guid.NewGuid().ToString("N");
        }
    }

    public void ClearMessages(){ Error=null; Success=null; }
}
