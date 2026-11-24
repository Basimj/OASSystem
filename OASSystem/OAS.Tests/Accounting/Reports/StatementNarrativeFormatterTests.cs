using NUnit.Framework;
using OAS.Infrastructure.Accounting.Persistence.Queries;

namespace OAS.Tests.Accounting.Reports;

[TestFixture]
public sealed class StatementNarrativeFormatterTests
{
    [TestCase("salesreturn", "Customer return / SR-2026-000010", "سبب الإرجاع المدخل")]
    [TestCase("salesinvoice", "Sales tax / INV-2026-000034", "بيان الفاتورة الأصلي")]
    [TestCase("purchasereceipt", "GRNI GR-2026-000004", "ملاحظات الاستلام")]
    [TestCase("paymentvoucher", "Payment voucher PV-1", "Payment for rent")]
    public void SourceNarrativeTakesPriority(string type, string line, string note)
    {
        Assert.That(StatementNarrativeFormatter.Resolve(note, "DOC-1", type, line, "وصف القيد"), Is.EqualTo(note));
    }

    [TestCase("salesreturn", "Customer return / SR-2026-000010", "Sales return SR-2026-000010", "تخفيض ذمم العملاء")]
    [TestCase("salesreturn", "Sales tax reversal / SR-2026-000010", "Sales return SR-2026-000010", "عكس ضريبة المبيعات")]
    [TestCase("salesinvoice", "Sales tax / INV-2026-000034", "Sales invoice INV-2026-000034", "ضريبة المبيعات")]
    [TestCase("purchasereceipt", "GRNI GR-2026-000004", "Purchase receipt GR-2026-000004", "استحقاق مشتريات مستلمة وغير مفوترة")]
    [TestCase("salesinvoice", "COGS / INV-2026-000051 / line 1", "Sales invoice INV-2026-000051", "تكلفة البضاعة المباعة")]
    [TestCase("customersettlementfx", "Customer AR realized FX adjustment", "Customer realized FX settlement", "فرق الصرف المحقق على ذمم العملاء")]
    public void GeneratedDescriptionsHaveArabicFallbackEvenWithoutReference(string type, string line, string journal, string expected)
    {
        Assert.That(StatementNarrativeFormatter.Resolve(null, null, type, line, journal), Does.Contain(expected));
    }

    [Test]
    public void ManualLineAndMeaningfulJournalRemainIntact()
    {
        Assert.That(StatementNarrativeFormatter.Resolve(null, null, null, "تسديد إيجار", "قيد يومية", true), Is.EqualTo("تسديد إيجار"));
        Assert.That(StatementNarrativeFormatter.Resolve(null, null, "customersettlementfx", "Customer AR realized FX adjustment", "فرق صرف محقق لتحصيل فاتورة مبيعات INV-1"), Is.EqualTo("فرق صرف محقق لتحصيل فاتورة مبيعات INV-1"));
    }
}
