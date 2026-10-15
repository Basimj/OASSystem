using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OAS.Tests.Sales.Client;

[TestFixture]
public sealed class CompleteSalesWorkflowClientUiTests
{
    private static readonly Regex RawHtml = new(
        @"<\s*(div|span|main|section|header|nav|form|input|button|label|table|select|textarea|p|h1|h2|ul|li)(?=\s|/?>)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    [TestCase("OAS.Client/Sales/Checkout/Pages/SalesCheckoutPage.razor", "@page \"/sales/checkout\"")]
    [TestCase("OAS.Client/Sales/OrderTracking/Pages/CustomerOrderOperationsPage.razor", "@page \"/sales/order-tracking\"")]
    [TestCase("OAS.Client/Purchasing/CustomerDemand/Pages/CustomerDemandTrackingPage.razor", "@page \"/purchasing/customer-demand\"")]
    [TestCase("OAS.Client/Optical/Pages/OpticalJobsPage.razor", "@page \"/optical/jobs\"")]
    [TestCase("OAS.Client/Sales/Checkout/Pages/DeliverySettlementPage.razor", "@page \"/sales/delivery/{OrderId:guid}\"")]
    public void WorkflowPage_HasExpectedRoute_AndNoRawHtml(string relativePath, string route)
    {
        var text = File.ReadAllText(Find(relativePath));
        Assert.That(text, Does.Contain(route));
        Assert.That(RawHtml.IsMatch(text), Is.False,
            $"Workflow Client page {relativePath} must render UiLib components only.");
    }

    [Test]
    public void OpticalSurface_DoesNotRenderFinancialFields()
    {
        var text = File.ReadAllText(Find("Shared/UiLib/Components/Optical/UiOpticalJobsSurface.razor"));
        foreach (var forbidden in new[] { "InvoiceTotal", "LinePrice", "Discount", "Outstanding", "PaymentMethod", "CreditLimit", "JournalEntryId", "ReceiptVoucherId" })
            Assert.That(text, Does.Not.Contain(forbidden), $"Lab UI leaked financial field {forbidden}.");
    }

    [Test]
    public void PostedInvoice_CollectsThroughReceiptVoucherFlow()
    {
        var host = File.ReadAllText(Find("OAS.Client/Sales/Workspace/SalesWorkspaceHost.razor"));
        var code = File.ReadAllText(Find("OAS.Client/Sales/Workspace/SalesWorkspaceHost.razor.cs"));
        var dialog = File.ReadAllText(Find("Shared/UiLib/Components/Sales/Invoices/UiSalesInvoiceCollectionDialog.razor"));

        Assert.That(host, Does.Contain("تحصيل دفعة"));
        Assert.That(host, Does.Contain("invoice.OutstandingAmount > 0.0001m"));
        Assert.That(code, Does.Contain("CollectSalesInvoicePaymentResultAsync"));
        Assert.That(code, Does.Not.Contain("UpdateSalesInvoiceResultAsync(invoice.Id.Value"));
        Assert.That(dialog, Does.Contain("سند قبض مستقل"));
        Assert.That(dialog, Does.Contain("إنشاء سند قبض وترحيله"));
    }

    [Test]
    public void CheckoutPaymentEditor_SupportsOtherSettlementAccount()
    {
        var text = File.ReadAllText(Find("Shared/UiLib/Components/Sales/Checkout/UiSalesPaymentLinesEditor.razor"));
        Assert.That(text, Does.Contain("PaymentMethod == \"Other\""));
        Assert.That(text, Does.Contain("SettlementAccountId"));
        Assert.That(text, Does.Contain("SearchSettlementAccountsAsync"));
    }

    private static string Find(string relativePath)
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        throw new FileNotFoundException(relativePath);
    }
}
