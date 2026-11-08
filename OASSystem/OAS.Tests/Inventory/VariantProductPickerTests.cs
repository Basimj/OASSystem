using System.Reflection;
using NUnit.Framework;
using OAS.Client.Inventory.Pages;
using OAS.Contracts.Inventory.Products;
using OAS.UiLib.Core.Models;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class VariantProductPickerTests
{
    [Test]
    public void NewVariantListsActiveProductsFromEveryCategoryWithoutSelectingCategoryFirst()
    {
        var first = Product("PR-100", "منتج أول", Guid.NewGuid(), active: true);
        var second = Product("PR-200", "منتج ثان", Guid.NewGuid(), active: true);
        var inactive = Product("PR-300", "منتج موقوف", first.CategoryId, active: false);
        var page = CreatePage("Create", [first, second, inactive]);

        Assert.That(GetPrivate<string?>(page, "_categoryId"), Is.Null);
        Assert.That(GetPrivate<string?>(page, "_productId"), Is.Null);
        Assert.That(Options(page).Select(x => x.Value),
            Is.EquivalentTo(new[] { first.Id.ToString("D"), second.Id.ToString("D") }));
    }

    [Test]
    public void ChoosingParentProductSetsItsCategoryAndTypeAutomatically()
    {
        var typeId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = Product("PR-400", "منتج ثالث", categoryId, active: true);
        var page = CreatePage("Create", [product]);
        SetPrivate(page, "_categories", new[] { new ProductCategoryDto(categoryId, "CAT", "تصنيف", typeId, true) });

        typeof(ProductsPage).GetMethod("SetVariantProduct", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(page, [product.Id.ToString("D")]);

        Assert.Multiple(() =>
        {
            Assert.That(GetPrivate<string?>(page, "_productId"), Is.EqualTo(product.Id.ToString("D")));
            Assert.That(GetPrivate<string?>(page, "_categoryId"), Is.EqualTo(categoryId.ToString("D")));
            Assert.That(GetPrivate<string?>(page, "_productTypeId"), Is.EqualTo(typeId.ToString("D")));
        });
    }

    [Test]
    public void ExistingVariantCanStillShowItsInactiveParentButCannotChooseItForNewVariant()
    {
        var inactive = Product("PR-500", "منتج موقوف", Guid.NewGuid(), active: false);
        var createPage = CreatePage("Create", [inactive]);
        SetPrivate(createPage, "_productId", inactive.Id.ToString("D"));
        Assert.That(Options(createPage), Is.Empty);

        var editPage = CreatePage("Edit", [inactive]);
        SetPrivate(editPage, "_productId", inactive.Id.ToString("D"));
        Assert.That(Options(editPage).Select(x => x.Value), Is.EqualTo(new[] { inactive.Id.ToString("D") }));
    }

    private static ProductsPage CreatePage(string mode, ProductDto[] products)
    {
        var page = new ProductsPage();
        SetPrivate(page, "_products", products);
        var field = typeof(ProductsPage).GetField("_mode", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(page, Enum.Parse(field.FieldType, mode));
        return page;
    }

    private static ProductDto Product(string code, string name, Guid categoryId, bool active) =>
        new(Guid.NewGuid(), code, name, categoryId, null, null, true, active);

    private static IReadOnlyList<UiSelectOption> Options(ProductsPage page) =>
        (IReadOnlyList<UiSelectOption>)typeof(ProductsPage)
            .GetProperty("ProductOptions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(page)!;

    private static T GetPrivate<T>(ProductsPage page, string name) =>
        (T)typeof(ProductsPage).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page)!;

    private static void SetPrivate(ProductsPage page, string name, object? value) =>
        typeof(ProductsPage).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, value);
}
