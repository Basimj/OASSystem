using System.Globalization;
using OAS.UiLib.Core.Models.Sales;
using NUnit.Framework;

namespace OAS.Tests.Sales.Client;

[TestFixture]
public sealed class ManualLensInputTests
{
    [TestCase("3", 3)]
    [TestCase("-6", -6)]
    [TestCase("+3.00", 3)]
    [TestCase("-1.25", -1.25)]
    [TestCase("−6", -6)]
    [TestCase("-٦٫٢٥", -6.25)]
    [TestCase("۳٫۵", 3.5)]
    [TestCase("0", 0)]
    public void DecimalParserAcceptsSignedAndLocalizedMeasurements(string input, double expected)
    {
        var accepted = OpticalInputNumberParser.TryParseDecimal(input, out var value);
        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(value, Is.EqualTo((decimal)expected));
        });
    }

    [Test]
    public void ParsingSignedCylinderDoesNotDependOnOperatingSystemCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.That(OpticalInputNumberParser.TryParseDecimal("-6.25", out var value), Is.True);
            Assert.That(value, Is.EqualTo(-6.25m));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestCase("abc")]
    [TestCase("3..5")]
    [TestCase("--6")]
    [TestCase("1 234")]
    public void InvalidMeasurementsDoNotBecomeSuccessfulNullValues(string input)
    {
        Assert.That(OpticalInputNumberParser.TryParseDecimal(input, out var value), Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void EmptyInputIsMissingNotZero()
    {
        Assert.That(OpticalInputNumberParser.TryParseDecimal(" ", out var value), Is.True);
        Assert.That(value, Is.Null);
    }

    [TestCase("150", 150)]
    [TestCase("١٥٠", 150)]
    public void AxisParserAcceptsValidAngles(string input, int expected)
    {
        Assert.That(OpticalInputNumberParser.TryParseAxis(input, out var value), Is.True);
        Assert.That(value, Is.EqualTo((short)expected));
    }

    [TestCase("-6")]
    [TestCase("0")]
    [TestCase("181")]
    [TestCase("3.5")]
    public void AxisParserRejectsInvalidAngles(string input)
    {
        Assert.That(OpticalInputNumberParser.TryParseAxis(input, out _), Is.False);
    }

    [Test]
    public void ManualMeasurementFieldsAndValidationAreSharedByOrderAndInvoice()
    {
        var lines = Read("Shared/UiLib/Components/Sales/Orders/UiSalesLinesEditor.razor");
        var order = Read("Shared/UiLib/Components/Sales/Orders/UiCustomerOrderEditor.razor");
        var invoice = Read("Shared/UiLib/Components/Sales/Invoices/UiSalesInvoiceEditor.razor");
        var host = Read("OAS.Client/Sales/Workspace/SalesWorkspaceHost.razor.cs");
        Assert.Multiple(() =>
        {
            Assert.That(lines, Does.Contain("@oninput=\"e => OpticalDecimalChanged(line, nameof(line.CYL), e)\""));
            Assert.That(lines, Does.Contain("OpticalInputNumberParser.TryParseDecimal"));
            Assert.That(lines, Does.Contain("public bool ValidateOpticalInputs()"));
            Assert.That(order, Does.Contain("_salesLinesEditor?.ValidateOpticalInputs()"));
            Assert.That(invoice, Does.Contain("_salesLinesEditor?.ValidateOpticalInputs()"));
            Assert.That(host, Does.Contain("_activeSalesInvoiceEditor is { } invoiceEditor && !invoiceEditor.ValidateOpticalInputs()"));
        });
    }

    private static string Read(string path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var full = Path.Combine(directory.FullName, path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full)) return File.ReadAllText(full);
            directory = directory.Parent;
        }
        throw new FileNotFoundException(path);
    }
}
