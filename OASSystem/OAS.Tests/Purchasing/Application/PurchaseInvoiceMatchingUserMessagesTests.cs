using NUnit.Framework;
using OAS.Client.Purchasing.Common;
using OAS.Contracts.Common.Errors;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class PurchaseInvoiceMatchingUserMessagesTests
{
    [Test]
    public void Quantity_conflict_includes_next_step_and_server_quantity_detail()
    {
        var message=PurchaseInvoiceMatchingMessages.FromApiError(new ApiError
        {
            Code="purchasing_match_quantity_exceeds_available_receipt",
            Message="المطلوب 8، المتاح 3 بعد المطابقة السابقة.", Status=409
        });
        Assert.Multiple(()=>
        {
            Assert.That(message,Does.Contain("المتاح 3"));
            Assert.That(message,Does.Contain("أعد تشغيل المطابقة"));
            Assert.That(message,Does.Contain("لم تُرحّل الفاتورة"));
        });
    }

    [Test]
    public void Shortfall_message_explains_other_invoices_and_returns()
    {
        var message=PurchaseInvoiceMatchingMessages.NotEnoughAvailable("البند 2 لا تتوفر له الكمية المطلوبة.");
        Assert.Multiple(()=>
        {
            Assert.That(message,Does.Contain("البند 2"));
            Assert.That(message,Does.Contain("فاتورة أخرى"));
            Assert.That(message,Does.Contain("المورد"));
        });
    }
}
