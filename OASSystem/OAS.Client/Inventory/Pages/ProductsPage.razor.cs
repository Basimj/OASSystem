using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;
using System.Linq.Expressions;
using System.Globalization;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Inventory.Services;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Enums.Inventory;
using OAS.Contracts.Inventory.Products;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Inventory.Pages;

public partial class ProductsPage
{
    private enum ProductSection { Categories, Brands, Units, Products, Variants }
    private enum EditorMode { Empty, View, Create, Edit }

    private sealed record SectionItem(ProductSection Value, string Text, string Icon);

    private const string FrameModelField = "الموديل";
    private const string FrameMaterialField = "الخامة";
    private const string FrameRimField = "نوع الإطار / Rim";
    private const string FrameGenderField = "الجنس";
    private const string FrameShapeField = "الشكل";
    private const string TempleLengthField = "طول الذراع";
    private const string BridgeSizeField = "مقاس الجسر";
    private const string LensWidthField = "عرض العدسة";
    private const string LensTypeField = "نوع العدسة";
    private const string LensMaterialField = "خامة العدسة";
    private const string LensCoatingField = "الطلاء / Coating";
    private const string RefractiveIndexField = "معامل الانكسار";
    private const string SphereMinField = "Sphere Min";
    private const string SphereMaxField = "Sphere Max";
    private const string CylinderMinField = "Cylinder Min";
    private const string CylinderMaxField = "Cylinder Max";
    private const string AddMinField = "Add Min";
    private const string AddMaxField = "Add Max";

    [Inject] private IInventoryClientService Inventory { get; set; } = default!;
    [Inject] private InventorySpreadsheetClient SpreadsheetClient { get; set; } = default!;
    [Inject] private BrowserFileDownloadService SpreadsheetDownload { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    [Parameter, SupplyParameterFromQuery(Name = "productId")]
    public Guid? ProductIdFromQuery { get; set; }

    private static readonly IReadOnlyList<SectionItem> Sections =
    [
        new(ProductSection.Categories, "التصنيفات", "fa-solid fa-layer-group"),
        new(ProductSection.Brands, "العلامات التجارية", "fa-solid fa-tag"),
        new(ProductSection.Units, "الوحدات", "fa-solid fa-ruler"),
        new(ProductSection.Products, "المنتجات", "fa-solid fa-glasses"),
        new(ProductSection.Variants, "المتغيرات", "fa-solid fa-barcode")
    ];

    private IReadOnlyList<UiSectionTabItem> SectionTabs => Sections
        .Select(x => new UiSectionTabItem(x.Value.ToString(), x.Text, x.Value == _section))
        .ToArray();

    private ProductSection _section = ProductSection.Products;
    private EditorMode _mode = EditorMode.Empty;
    private bool _loading = true;
    private bool _saving;
    private bool _exporting;
    private string? _search;
    private readonly Dictionary<string, string> _fieldErrors = new(StringComparer.Ordinal);

    private IReadOnlyList<ProductCategoryDto> _categories = [];
    private IReadOnlyList<BrandDto> _brands = [];
    private IReadOnlyList<UnitDto> _units = [];
    private IReadOnlyList<ProductDto> _products = [];
    private IReadOnlyList<ProductVariantDto> _variants = [];
    private IReadOnlyList<FrameDetailsDto> _frameDetails = [];
    private IReadOnlyList<LensDetailsDto> _lensDetails = [];

    private Guid? _selectedId;

    // Shared/edit fields
    private string _code = string.Empty;
    private string _nameAr = string.Empty;
    private string? _nameEn;
    private bool _isActive = true;

    // Category
    private string? _parentCategoryId;

    // Brand
    private string _brandName = string.Empty;

    // Product
    private string? _categoryId;
    private string? _brandId;
    private string _productType = ((int)ProductType.Frame).ToString();
    private string? _description;
    private bool _isStockItem = true;

    // Frame details
    private string _frameModel = string.Empty;
    private string? _frameMaterial;
    private string? _frameRimType;
    private string? _frameGender;
    private string? _frameShape;
    private string? _templeLength;
    private string? _bridgeSize;
    private string? _lensWidth;

    // Lens details
    private string _lensType = string.Empty;
    private string? _lensMaterial;
    private string? _lensCoating;
    private string? _refractiveIndex;
    private string? _sphereMin;
    private string? _sphereMax;
    private string? _cylinderMin;
    private string? _cylinderMax;
    private string? _addMin;
    private string? _addMax;
    private bool _isPrescriptionLens;

    // Variant
    private string? _productId;
    private string? _barcode;
    private string? _variantName;
    private string? _color;
    private string? _size;
    private string? _unitId;
    private string _purchasePrice = "0";
    private string _sellingPrice = "0";

    private bool IsEditing => _mode is EditorMode.Create or EditorMode.Edit;
    private bool CanEdit => _mode == EditorMode.View && _selectedId.HasValue;
    private bool CanSave => IsEditing && !_saving;
    private ProductType CurrentProductType => int.TryParse(_productType, out var raw) ? (ProductType)raw : ProductType.Frame;
    private bool ShowsFrameDetails => _section == ProductSection.Products && CurrentProductType is ProductType.Frame or ProductType.Sunglasses;
    private bool ShowsLensDetails => _section == ProductSection.Products && CurrentProductType == ProductType.Lens;

    private string SectionTitle => _section switch
    {
        ProductSection.Categories => "تصنيفات المنتجات",
        ProductSection.Brands => "العلامات التجارية",
        ProductSection.Units => "وحدات القياس",
        ProductSection.Products => "المنتجات",
        ProductSection.Variants => "متغيرات المنتجات",
        _ => "المنتجات"
    };

    private string SectionSubtitle => $"{VisibleCount} سجل";

    private static readonly IReadOnlyList<string> SimpleHeaders = ["الكود", "الاسم", "الحالة"];
    private static readonly IReadOnlyList<string> ProductHeaders = ["الكود", "المنتج", "النوع", "الحالة"];
    private static readonly IReadOnlyList<string> VariantHeaders = ["SKU", "المتغير", "الباركود", "سعر البيع", "الحالة"];


    private int VisibleCount => _section switch
    {
        ProductSection.Categories => FilterCategories().Count,
        ProductSection.Brands => FilterBrands().Count,
        ProductSection.Units => FilterUnits().Count,
        ProductSection.Products => FilterProducts().Count,
        ProductSection.Variants => FilterVariants().Count,
        _ => 0
    };

    private string EditorTitle => _mode switch
    {
        EditorMode.Create => $"{SectionSingular} جديد",
        EditorMode.Edit => $"تعديل {SectionSingular}",
        EditorMode.View => $"تفاصيل {SectionSingular}",
        _ => $"تفاصيل {SectionSingular}"
    };

    private string SectionSingular => _section switch
    {
        ProductSection.Categories => "تصنيف",
        ProductSection.Brands => "علامة تجارية",
        ProductSection.Units => "وحدة",
        ProductSection.Products => "منتج",
        ProductSection.Variants => "متغير منتج",
        _ => "سجل"
    };

    private string? EditorSubtitle => _selectedId.HasValue ? _code : null;

    private IReadOnlyList<UiSelectOption> CategoryOptions =>
        [new UiSelectOption(string.Empty, "— بدون —"), .. _categories.Where(x => x.IsActive).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}"))];

    private IReadOnlyList<UiSelectOption> BrandOptions =>
        [new UiSelectOption(string.Empty, "— بدون —"), .. _brands.Where(x => x.IsActive).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.Code} - {x.Name}"))];

    private IReadOnlyList<UiSelectOption> UnitOptions =>
        [new UiSelectOption(string.Empty, "— بدون —"), .. _units.Where(x => x.IsActive).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}"))];

    private IReadOnlyList<UiSelectOption> ProductOptions =>
        _products.Where(x => x.IsActive).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.ProductCode} - {x.NameAr}")).ToArray();

    private static readonly IReadOnlyList<UiSelectOption> ProductTypeOptions =
    [
        new(((int)ProductType.Frame).ToString(), "إطار"),
        new(((int)ProductType.Lens).ToString(), "عدسة"),
        new(((int)ProductType.Sunglasses).ToString(), "نظارة شمسية"),
        new(((int)ProductType.Accessory).ToString(), "إكسسوار"),
        new(((int)ProductType.Other).ToString(), "أخرى"),
        new(((int)ProductType.Service).ToString(), "خدمة")
    ];

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            var request = new PageRequest { PageNumber = 1, PageSize = PageRequest.MaximumPageSize };
            var categoriesTask = Inventory.GetProductCategoriesAsync(request);
            var brandsTask = Inventory.GetBrandsAsync(request);
            var unitsTask = Inventory.GetUnitsAsync(request);
            var productsTask = Inventory.GetProductsAsync(request);
            var variantsTask = Inventory.GetProductVariantsAsync(request);
            var frameDetailsTask = Inventory.GetFrameDetailsAsync(request);
            var lensDetailsTask = Inventory.GetLensDetailsAsync(request);
            await Task.WhenAll(categoriesTask, brandsTask, unitsTask, productsTask, variantsTask, frameDetailsTask, lensDetailsTask);

            _categories = categoriesTask.Result.Items;
            _brands = brandsTask.Result.Items;
            _units = unitsTask.Result.Items;
            _products = productsTask.Result.Items;
            _variants = variantsTask.Result.Items;
            _frameDetails = frameDetailsTask.Result.Items;
            _lensDetails = lensDetailsTask.Result.Items;

            if (ProductIdFromQuery.HasValue && _products.Any(x => x.Id == ProductIdFromQuery.Value))
            {
                _section = ProductSection.Products;
                _selectedId = ProductIdFromQuery.Value;
                _mode = EditorMode.View;
                LoadSelectedIntoForm(ProductIdFromQuery.Value);
            }

            if (_selectedId.HasValue && !SelectionExists(_selectedId.Value))
            {
                _selectedId = null;
                _mode = EditorMode.Empty;
                ClearForm();
            }
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(ex.Error);
        }
        catch
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            _loading = false;
        }
    }

    private Task RefreshAsync(MouseEventArgs args) => LoadAsync();
    private Task SearchChangedAsync(string? _) => Task.CompletedTask;


    private Task ChangeSectionKeyAsync(string key) =>
        Enum.TryParse<ProductSection>(key, out var section) ? ChangeSectionAsync(section) : Task.CompletedTask;

    private async Task ChangeSectionAsync(ProductSection section)
    {
        if (IsEditing || _saving || _section == section) return;
        _section = section;
        ClearValidation();
        _selectedId = null;
        _mode = EditorMode.Empty;
        _search = null;
        ClearForm();
        await Task.CompletedTask;
    }

    private Task BeginCreateAsync(MouseEventArgs args)
    {
        if (IsEditing) return Task.CompletedTask;
        _selectedId = null;
        _mode = EditorMode.Create;
        ClearForm();
        _isActive = true;
        _isStockItem = true;
        return Task.CompletedTask;
    }

    private Task BeginEditAsync(MouseEventArgs args)
    {
        if (!CanEdit) return Task.CompletedTask;
        ClearValidation();
        _mode = EditorMode.Edit;
        return Task.CompletedTask;
    }

    private Task CancelEditAsync(MouseEventArgs args)
    {
        ClearValidation();
        if (_selectedId.HasValue)
        {
            _mode = EditorMode.View;
            LoadSelectedIntoForm(_selectedId.Value);
        }
        else
        {
            _mode = EditorMode.Empty;
            ClearForm();
        }
        return Task.CompletedTask;
    }

    private async Task SaveAsync(MouseEventArgs args)
    {
        if (!CanSave) return;
        if (!ValidateForm()) return;

        _saving = true;
        try
        {
            Guid? savedId = _section switch
            {
                ProductSection.Categories => await SaveCategoryAsync(),
                ProductSection.Brands => await SaveBrandAsync(),
                ProductSection.Units => await SaveUnitAsync(),
                ProductSection.Products => await SaveProductAsync(),
                ProductSection.Variants => await SaveVariantAsync(),
                _ => null
            };

            if (!savedId.HasValue) return;

            _selectedId = savedId;
            _mode = EditorMode.View;
            await LoadAsync();
            LoadSelectedIntoForm(savedId.Value);
            Snackbar.Success("تم حفظ البيانات بنجاح.");
        }
        catch (ApiClientException ex)
        {
            if (string.Equals(ex.Error.Code, "unique_constraint_conflict", StringComparison.OrdinalIgnoreCase))
            {
                await LoadAsync();
                if (ValidateForm())
                    Snackbar.Error("توجد قيمة مستخدمة مسبقًا. حدّث البيانات وحاول مرة أخرى.");
            }
            else
            {
                ApiFeedback.Show(ex.Error);
            }
        }
        catch
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task<Guid?> SaveCategoryAsync()
    {
        ProductCategoryDto? result;
        var parentId = ParseGuid(_parentCategoryId);
        if (_mode == EditorMode.Create)
            result = await Inventory.CreateProductCategoryAsync(new(_code.Trim(), _nameAr.Trim(), NullIfBlank(_nameEn), parentId));
        else
            result = await Inventory.UpdateProductCategoryAsync(_selectedId!.Value, new(_nameAr.Trim(), NullIfBlank(_nameEn), parentId, _isActive));
        return result?.Id;
    }

    private async Task<Guid?> SaveBrandAsync()
    {
        BrandDto? result;
        if (_mode == EditorMode.Create)
            result = await Inventory.CreateBrandAsync(new(_code.Trim(), _brandName.Trim()));
        else
            result = await Inventory.UpdateBrandAsync(_selectedId!.Value, new(_brandName.Trim(), _isActive));
        return result?.Id;
    }

    private async Task<Guid?> SaveUnitAsync()
    {
        UnitDto? result;
        if (_mode == EditorMode.Create)
            result = await Inventory.CreateUnitAsync(new(_code.Trim(), _nameAr.Trim(), NullIfBlank(_nameEn)));
        else
            result = await Inventory.UpdateUnitAsync(_selectedId!.Value, new(_nameAr.Trim(), NullIfBlank(_nameEn), _isActive));
        return result?.Id;
    }

    private async Task<Guid?> SaveProductAsync()
    {
        if (!Guid.TryParse(_categoryId, out var categoryId)) return null;
        var brandId = ParseGuid(_brandId);
        var type = CurrentProductType;
        ProductDto? result;
        if (_mode == EditorMode.Create)
            result = await Inventory.CreateProductAsync(new(_code.Trim(), _nameAr.Trim(), NullIfBlank(_nameEn), categoryId, brandId, type, NullIfBlank(_description), _isStockItem));
        else
            result = await Inventory.UpdateProductAsync(_selectedId!.Value, new(_nameAr.Trim(), NullIfBlank(_nameEn), categoryId, brandId, type, NullIfBlank(_description), _isStockItem, _isActive));

        if (result is not null)
            await SaveSpecializedDetailsAsync(result.Id, type);

        return result?.Id;
    }

    private async Task SaveSpecializedDetailsAsync(Guid productId, ProductType type)
    {
        if (type is ProductType.Frame or ProductType.Sunglasses)
        {
            var existing = _frameDetails.FirstOrDefault(x => x.ProductId == productId);
            var hasValues = existing is not null || !string.IsNullOrWhiteSpace(_frameModel) || !string.IsNullOrWhiteSpace(_frameMaterial) ||
                !string.IsNullOrWhiteSpace(_frameRimType) || !string.IsNullOrWhiteSpace(_frameGender) || !string.IsNullOrWhiteSpace(_frameShape);
            if (!hasValues) return;
            if (string.IsNullOrWhiteSpace(_frameModel)) return;

            if (existing is null)
                await Inventory.CreateFrameDetailsAsync(new CreateFrameDetailsRequest(productId, _frameModel.Trim(), NullIfBlank(_frameMaterial), NullIfBlank(_frameRimType), NullIfBlank(_frameGender), NullIfBlank(_frameShape), ParseNullableDecimal(_templeLength), ParseNullableDecimal(_bridgeSize), ParseNullableDecimal(_lensWidth)));
            else
                await Inventory.UpdateFrameDetailsAsync(existing.Id, new UpdateFrameDetailsRequest(_frameModel.Trim(), NullIfBlank(_frameMaterial), NullIfBlank(_frameRimType), NullIfBlank(_frameGender), NullIfBlank(_frameShape), ParseNullableDecimal(_templeLength), ParseNullableDecimal(_bridgeSize), ParseNullableDecimal(_lensWidth)));
        }
        else if (type == ProductType.Lens)
        {
            var existing = _lensDetails.FirstOrDefault(x => x.ProductId == productId);
            var hasValues = existing is not null || !string.IsNullOrWhiteSpace(_lensType) || !string.IsNullOrWhiteSpace(_lensMaterial) || !string.IsNullOrWhiteSpace(_lensCoating);
            if (!hasValues) return;
            if (string.IsNullOrWhiteSpace(_lensType)) return;

            if (existing is null)
                await Inventory.CreateLensDetailsAsync(new CreateLensDetailsRequest(productId, _lensType.Trim(), NullIfBlank(_lensMaterial), NullIfBlank(_lensCoating), ParseNullableDecimal(_refractiveIndex), ParseNullableDecimal(_sphereMin), ParseNullableDecimal(_sphereMax), ParseNullableDecimal(_cylinderMin), ParseNullableDecimal(_cylinderMax), ParseNullableDecimal(_addMin), ParseNullableDecimal(_addMax), _isPrescriptionLens));
            else
                await Inventory.UpdateLensDetailsAsync(existing.Id, new UpdateLensDetailsRequest(_lensType.Trim(), NullIfBlank(_lensMaterial), NullIfBlank(_lensCoating), ParseNullableDecimal(_refractiveIndex), ParseNullableDecimal(_sphereMin), ParseNullableDecimal(_sphereMax), ParseNullableDecimal(_cylinderMin), ParseNullableDecimal(_cylinderMax), ParseNullableDecimal(_addMin), ParseNullableDecimal(_addMax), _isPrescriptionLens));
        }
    }

    private async Task<Guid?> SaveVariantAsync()
    {
        if (!Guid.TryParse(_productId, out var productId)) return null;
        var unitId = ParseGuid(_unitId);
        if (!TryParseDecimalInput(_purchasePrice, out var purchasePrice) || !TryParseDecimalInput(_sellingPrice, out var sellingPrice)) return null;
        ProductVariantDto? result;
        if (_mode == EditorMode.Create)
            result = await Inventory.CreateProductVariantAsync(new(productId, _code.Trim(), NullIfBlank(_barcode), NullIfBlank(_variantName), NullIfBlank(_color), NullIfBlank(_size), unitId, purchasePrice, sellingPrice));
        else
            result = await Inventory.UpdateProductVariantAsync(_selectedId!.Value, new(NullIfBlank(_barcode), NullIfBlank(_variantName), NullIfBlank(_color), NullIfBlank(_size), unitId, purchasePrice, sellingPrice, _isActive));
        return result?.Id;
    }

    private bool ValidateForm()
    {
        ClearValidation();
        var missingRequired = false;

        void Required(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) return;
            SetError(key, "هذا الحقل إجباري.");
            missingRequired = true;
        }

        void MaxLength(string key, string? value, int max)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > max)
                SetError(key, $"الحد الأقصى {max} حرفًا.");
        }

        switch (_section)
        {
            case ProductSection.Categories:
                Required("الكود", _code);
                Required("الاسم بالعربية", _nameAr);
                MaxLength("الكود", _code, 32);
                MaxLength("الاسم بالعربية", _nameAr, 100);
                MaxLength("الاسم بالإنجليزية", _nameEn, 100);
                if (_mode == EditorMode.Create && _categories.Any(x => string.Equals(x.Code, _code.Trim(), StringComparison.OrdinalIgnoreCase)))
                    SetError("الكود", "هذا الكود مستخدم مسبقًا.");
                break;

            case ProductSection.Brands:
                Required("الكود", _code);
                Required("اسم العلامة", _brandName);
                MaxLength("الكود", _code, 32);
                MaxLength("اسم العلامة", _brandName, 100);
                if (_mode == EditorMode.Create && _brands.Any(x => string.Equals(x.Code, _code.Trim(), StringComparison.OrdinalIgnoreCase)))
                    SetError("الكود", "هذا الكود مستخدم مسبقًا.");
                if (_brands.Any(x => x.Id != _selectedId && string.Equals(x.Name, _brandName.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                    SetError("اسم العلامة", "اسم العلامة مستخدم مسبقًا.");
                break;

            case ProductSection.Units:
                Required("الكود", _code);
                Required("الاسم بالعربية", _nameAr);
                MaxLength("الكود", _code, 32);
                MaxLength("الاسم بالعربية", _nameAr, 100);
                MaxLength("الاسم بالإنجليزية", _nameEn, 100);
                if (_mode == EditorMode.Create && _units.Any(x => string.Equals(x.Code, _code.Trim(), StringComparison.OrdinalIgnoreCase)))
                    SetError("الكود", "هذا الكود مستخدم مسبقًا.");
                break;

            case ProductSection.Products:
                Required("كود المنتج", _code);
                Required("الاسم بالعربية", _nameAr);
                Required("التصنيف", _categoryId);
                Required("نوع المنتج", _productType);
                MaxLength("كود المنتج", _code, 32);
                MaxLength("الاسم بالعربية", _nameAr, 150);
                MaxLength("الاسم بالإنجليزية", _nameEn, 150);
                MaxLength("الوصف", _description, 500);

                if (_mode == EditorMode.Create && _products.Any(x => string.Equals(x.ProductCode, _code.Trim(), StringComparison.OrdinalIgnoreCase)))
                    SetError("كود المنتج", "كود المنتج مستخدم مسبقًا.");
                if (!string.IsNullOrWhiteSpace(_categoryId) && !Guid.TryParse(_categoryId, out _))
                    SetError("التصنيف", "قيمة التصنيف غير صحيحة.");
                if (!int.TryParse(_productType, out var productTypeValue) || !Enum.IsDefined(typeof(ProductType), productTypeValue))
                    SetError("نوع المنتج", "نوع المنتج غير صحيح.");

                ValidateSpecializedProductFields(ref missingRequired);
                break;

            case ProductSection.Variants:
                Required("المنتج", _productId);
                Required("SKU", _code);
                Required("سعر الشراء", _purchasePrice);
                Required("سعر البيع", _sellingPrice);
                MaxLength("SKU", _code, 64);
                MaxLength("الباركود", _barcode, 64);
                MaxLength("اسم المتغير", _variantName, 100);
                MaxLength("اللون", _color, 50);
                MaxLength("المقاس", _size, 50);

                if (!string.IsNullOrWhiteSpace(_productId) && !Guid.TryParse(_productId, out _))
                    SetError("المنتج", "قيمة المنتج غير صحيحة.");
                if (_mode == EditorMode.Create && _variants.Any(x => string.Equals(x.SKU, _code.Trim(), StringComparison.OrdinalIgnoreCase)))
                    SetError("SKU", "SKU مستخدم مسبقًا.");
                if (!string.IsNullOrWhiteSpace(_barcode) && _variants.Any(x => x.Id != _selectedId && string.Equals(x.Barcode, _barcode.Trim(), StringComparison.OrdinalIgnoreCase)))
                    SetError("الباركود", "الباركود مستخدم مسبقًا.");

                ValidateDecimalField("سعر الشراء", _purchasePrice, required: true, precision: 18, scale: 2, nonNegative: true, ref missingRequired);
                ValidateDecimalField("سعر البيع", _sellingPrice, required: true, precision: 18, scale: 2, nonNegative: true, ref missingRequired);
                break;
        }

        if (_fieldErrors.Count == 0) return true;

        Snackbar.Error(missingRequired
            ? "هناك بيانات إجبارية لم يتم إدخالها."
            : "توجد بيانات غير صحيحة. راجع الحقول المميزة باللون الأحمر.");
        return false;
    }

    private void ValidateSpecializedProductFields(ref bool missingRequired)
    {
        if (ShowsFrameDetails)
        {
            var hasValues = HasFrameDetailsInput();
            if (hasValues && string.IsNullOrWhiteSpace(_frameModel))
            {
                SetError("الموديل", "هذا الحقل إجباري عند إدخال تفاصيل الإطار.");
                missingRequired = true;
            }
            ValidateMaxLength("الموديل", _frameModel, 100);
            ValidateMaxLength("الخامة", _frameMaterial, 50);
            ValidateMaxLength("نوع الإطار / Rim", _frameRimType, 50);
            ValidateMaxLength("الجنس", _frameGender, 20);
            ValidateMaxLength("الشكل", _frameShape, 50);
            ValidateDecimalField("طول الذراع", _templeLength, false, 6, 2, false, ref missingRequired);
            ValidateDecimalField("مقاس الجسر", _bridgeSize, false, 6, 2, false, ref missingRequired);
            ValidateDecimalField("عرض العدسة", _lensWidth, false, 6, 2, false, ref missingRequired);
        }
        else if (ShowsLensDetails)
        {
            var hasValues = HasLensDetailsInput();
            if (hasValues && string.IsNullOrWhiteSpace(_lensType))
            {
                SetError("نوع العدسة", "هذا الحقل إجباري عند إدخال تفاصيل العدسة.");
                missingRequired = true;
            }
            ValidateMaxLength("نوع العدسة", _lensType, 50);
            ValidateMaxLength("خامة العدسة", _lensMaterial, 50);
            ValidateMaxLength("الطلاء / Coating", _lensCoating, 50);
            ValidateDecimalField("معامل الانكسار", _refractiveIndex, false, 5, 3, false, ref missingRequired);
            ValidateDecimalField("Sphere Min", _sphereMin, false, 6, 2, false, ref missingRequired);
            ValidateDecimalField("Sphere Max", _sphereMax, false, 6, 2, false, ref missingRequired);
            ValidateDecimalField("Cylinder Min", _cylinderMin, false, 6, 2, false, ref missingRequired);
            ValidateDecimalField("Cylinder Max", _cylinderMax, false, 6, 2, false, ref missingRequired);
            ValidateDecimalField("Add Min", _addMin, false, 6, 2, false, ref missingRequired);
            ValidateDecimalField("Add Max", _addMax, false, 6, 2, false, ref missingRequired);
        }
    }

    private void ValidateMaxLength(string key, string? value, int max)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > max)
            SetError(key, $"الحد الأقصى {max} حرفًا.");
    }

    private void ValidateDecimalField(string key, string? value, bool required, int precision, int scale, bool nonNegative, ref bool missingRequired)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                SetError(key, "هذا الحقل إجباري.");
                missingRequired = true;
            }
            return;
        }

        if (!TryParseDecimalInput(value, out var parsed))
        {
            SetError(key, "يجب إدخال قيمة رقمية صحيحة.");
            return;
        }

        if (nonNegative && parsed < 0)
        {
            SetError(key, "يجب أن تكون القيمة صفرًا أو أكبر.");
            return;
        }

        if (decimal.Round(parsed, scale, MidpointRounding.ToEven) != parsed)
        {
            SetError(key, $"يسمح بحد أقصى {scale} منازل عشرية.");
            return;
        }

        var integerDigits = precision - scale;
        var maxAbs = Pow10(integerDigits);
        if (parsed <= -maxAbs || parsed >= maxAbs)
            SetError(key, $"القيمة أكبر من الحد المسموح ({integerDigits} أرقام قبل الفاصلة)." );
    }

    private static decimal Pow10(int power)
    {
        decimal value = 1;
        for (var i = 0; i < power; i++) value *= 10;
        return value;
    }

    private bool HasFrameDetailsInput() =>
        _frameDetails.Any(x => x.ProductId == _selectedId) ||
        !string.IsNullOrWhiteSpace(_frameModel) || !string.IsNullOrWhiteSpace(_frameMaterial) ||
        !string.IsNullOrWhiteSpace(_frameRimType) || !string.IsNullOrWhiteSpace(_frameGender) ||
        !string.IsNullOrWhiteSpace(_frameShape) || !string.IsNullOrWhiteSpace(_templeLength) ||
        !string.IsNullOrWhiteSpace(_bridgeSize) || !string.IsNullOrWhiteSpace(_lensWidth);

    private bool HasLensDetailsInput() =>
        _lensDetails.Any(x => x.ProductId == _selectedId) ||
        !string.IsNullOrWhiteSpace(_lensType) || !string.IsNullOrWhiteSpace(_lensMaterial) ||
        !string.IsNullOrWhiteSpace(_lensCoating) || !string.IsNullOrWhiteSpace(_refractiveIndex) ||
        !string.IsNullOrWhiteSpace(_sphereMin) || !string.IsNullOrWhiteSpace(_sphereMax) ||
        !string.IsNullOrWhiteSpace(_cylinderMin) || !string.IsNullOrWhiteSpace(_cylinderMax) ||
        !string.IsNullOrWhiteSpace(_addMin) || !string.IsNullOrWhiteSpace(_addMax);

    private string? ErrorFor(string key) => _fieldErrors.TryGetValue(key, out var error) ? error : null;
    private static string? OptionalPlaceholder(bool required) => required ? null : "(اختياري)";
    private void SetError(string key, string message) => _fieldErrors.TryAdd(key, message);
    private void ClearFieldError(string key) => _fieldErrors.Remove(key);
    private void ClearValidation() => _fieldErrors.Clear();

    private void SetField(string key, Action setter)
    {
        setter();
        ClearFieldError(key);
    }

    private void Select(Guid id)
    {
        if (IsEditing || _saving) return;
        ClearValidation();
        _selectedId = id;
        _mode = EditorMode.View;
        LoadSelectedIntoForm(id);
    }

    private void LoadSelectedIntoForm(Guid id)
    {
        ClearForm();
        switch (_section)
        {
            case ProductSection.Categories:
                if (_categories.FirstOrDefault(x => x.Id == id) is { } category)
                {
                    _code = category.Code; _nameAr = category.NameAr; _nameEn = category.NameEn;
                    _parentCategoryId = category.ParentCategoryId?.ToString("D"); _isActive = category.IsActive;
                }
                break;
            case ProductSection.Brands:
                if (_brands.FirstOrDefault(x => x.Id == id) is { } brand)
                {
                    _code = brand.Code; _brandName = brand.Name; _isActive = brand.IsActive;
                }
                break;
            case ProductSection.Units:
                if (_units.FirstOrDefault(x => x.Id == id) is { } unit)
                {
                    _code = unit.Code; _nameAr = unit.NameAr; _nameEn = unit.NameEn; _isActive = unit.IsActive;
                }
                break;
            case ProductSection.Products:
                if (_products.FirstOrDefault(x => x.Id == id) is { } product)
                {
                    _code = product.ProductCode; _nameAr = product.NameAr; _nameEn = product.NameEn;
                    _categoryId = product.CategoryId.ToString("D"); _brandId = product.BrandId?.ToString("D");
                    _productType = ((int)product.ProductType).ToString(); _description = product.Description;
                    _isStockItem = product.IsStockItem; _isActive = product.IsActive;
                    LoadSpecializedDetails(product.Id);
                }
                break;
            case ProductSection.Variants:
                if (_variants.FirstOrDefault(x => x.Id == id) is { } variant)
                {
                    _code = variant.SKU; _productId = variant.ProductId.ToString("D"); _barcode = variant.Barcode;
                    _variantName = variant.VariantName; _color = variant.Color; _size = variant.Size;
                    _unitId = variant.UnitId?.ToString("D");
                    _purchasePrice = variant.PurchasePrice.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    _sellingPrice = variant.SellingPrice.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    _isActive = variant.IsActive;
                }
                break;
        }
    }

    private void ClearForm()
    {
        ClearValidation();
        _code = string.Empty; _nameAr = string.Empty; _nameEn = null; _isActive = true;
        _parentCategoryId = null; _brandName = string.Empty;
        _categoryId = null; _brandId = null; _productType = ((int)ProductType.Frame).ToString();
        _description = null; _isStockItem = true;
        _frameModel = string.Empty; _frameMaterial = null; _frameRimType = null; _frameGender = null; _frameShape = null;
        _templeLength = null; _bridgeSize = null; _lensWidth = null;
        _lensType = string.Empty; _lensMaterial = null; _lensCoating = null; _refractiveIndex = null;
        _sphereMin = null; _sphereMax = null; _cylinderMin = null; _cylinderMax = null; _addMin = null; _addMax = null; _isPrescriptionLens = false;
        _productId = null; _barcode = null; _variantName = null; _color = null; _size = null; _unitId = null;
        _purchasePrice = "0"; _sellingPrice = "0";
    }

    private void LoadSpecializedDetails(Guid productId)
    {
        if (_frameDetails.FirstOrDefault(x => x.ProductId == productId) is { } frame)
        {
            _frameModel = frame.Model;
            _frameMaterial = frame.Material;
            _frameRimType = frame.RimType;
            _frameGender = frame.Gender;
            _frameShape = frame.Shape;
            _templeLength = FormatNullableDecimal(frame.TempleLength);
            _bridgeSize = FormatNullableDecimal(frame.BridgeSize);
            _lensWidth = FormatNullableDecimal(frame.LensWidth);
        }

        if (_lensDetails.FirstOrDefault(x => x.ProductId == productId) is { } lens)
        {
            _lensType = lens.LensType;
            _lensMaterial = lens.Material;
            _lensCoating = lens.Coating;
            _refractiveIndex = FormatNullableDecimal(lens.RefractiveIndex);
            _sphereMin = FormatNullableDecimal(lens.SphereMin);
            _sphereMax = FormatNullableDecimal(lens.SphereMax);
            _cylinderMin = FormatNullableDecimal(lens.CylinderMin);
            _cylinderMax = FormatNullableDecimal(lens.CylinderMax);
            _addMin = FormatNullableDecimal(lens.AddMin);
            _addMax = FormatNullableDecimal(lens.AddMax);
            _isPrescriptionLens = lens.IsPrescriptionLens;
        }
    }

    private bool SelectionExists(Guid id) => _section switch
    {
        ProductSection.Categories => _categories.Any(x => x.Id == id),
        ProductSection.Brands => _brands.Any(x => x.Id == id),
        ProductSection.Units => _units.Any(x => x.Id == id),
        ProductSection.Products => _products.Any(x => x.Id == id),
        ProductSection.Variants => _variants.Any(x => x.Id == id),
        _ => false
    };

    private async Task ExportAsync()
    {
        if (_exporting) return;
        _exporting = true;
        try
        {
            var section = _section switch
            {
                ProductSection.Categories => "product-categories",
                ProductSection.Brands => "brands",
                ProductSection.Units => "units",
                ProductSection.Products => "products",
                ProductSection.Variants => "product-variants",
                _ => "products"
            };
            var bytes = await SpreadsheetClient.ExportAsync(section, new PageRequest { Search = _search });
            await SpreadsheetDownload.SaveAsync(bytes, $"inventory-{section}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(ex.Error);
        }
        catch (Exception)
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            _exporting = false;
        }
    }

    private IReadOnlyList<ProductCategoryDto> FilterCategories() => _categories.Where(x => Match(_search, x.Code, x.NameAr, x.NameEn)).ToArray();
    private IReadOnlyList<BrandDto> FilterBrands() => _brands.Where(x => Match(_search, x.Code, x.Name)).ToArray();
    private IReadOnlyList<UnitDto> FilterUnits() => _units.Where(x => Match(_search, x.Code, x.NameAr, x.NameEn)).ToArray();
    private IReadOnlyList<ProductDto> FilterProducts() => _products.Where(x => Match(_search, x.ProductCode, x.NameAr, x.NameEn, x.Description)).ToArray();
    private IReadOnlyList<ProductVariantDto> FilterVariants() => _variants.Where(x => Match(_search, x.SKU, x.Barcode, x.VariantName, x.Color, x.Size)).ToArray();

    private static bool Match(string? search, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var term = search.Trim();
        return values.Any(x => x?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true);
    }

    private RenderFragment RenderEditor() => builder =>
    {
        var seq = 0;
        builder.OpenElement(seq++, "div"); builder.AddAttribute(seq++, "class", "inventory-products__editor");
        switch (_section)
        {
            case ProductSection.Categories: RenderCategoryEditor(builder, ref seq); break;
            case ProductSection.Brands: RenderBrandEditor(builder, ref seq); break;
            case ProductSection.Units: RenderUnitEditor(builder, ref seq); break;
            case ProductSection.Products: RenderProductEditor(builder, ref seq); break;
            case ProductSection.Variants: RenderVariantEditor(builder, ref seq); break;
        }
        builder.CloseElement();
    };

    private void RenderCategoryEditor(RenderTreeBuilder b, ref int s)
    {
        AddText(b, ref s, "الكود", _code, v => _code = v, () => _code, _mode != EditorMode.Create, true, 32);
        AddText(b, ref s, "الاسم بالعربية", _nameAr, v => _nameAr = v, () => _nameAr, !IsEditing, true, 100);
        AddText(b, ref s, "الاسم بالإنجليزية", _nameEn, v => _nameEn = v, () => _nameEn, !IsEditing, false, 100);
        AddSelect(b, ref s, "التصنيف الأب", _parentCategoryId, v => _parentCategoryId = v, () => _parentCategoryId, CategoryOptions.Where(x => x.Value != _selectedId?.ToString("D")).ToArray());
        AddActive(b, ref s);
    }

    private void RenderBrandEditor(RenderTreeBuilder b, ref int s)
    {
        AddText(b, ref s, "الكود", _code, v => _code = v, () => _code, _mode != EditorMode.Create, true, 32);
        AddText(b, ref s, "اسم العلامة", _brandName, v => _brandName = v, () => _brandName, !IsEditing, true, 100);
        AddActive(b, ref s);
    }

    private void RenderUnitEditor(RenderTreeBuilder b, ref int s)
    {
        AddText(b, ref s, "الكود", _code, v => _code = v, () => _code, _mode != EditorMode.Create, true, 32);
        AddText(b, ref s, "الاسم بالعربية", _nameAr, v => _nameAr = v, () => _nameAr, !IsEditing, true, 100);
        AddText(b, ref s, "الاسم بالإنجليزية", _nameEn, v => _nameEn = v, () => _nameEn, !IsEditing, false, 100);
        AddActive(b, ref s);
    }

    private void RenderProductEditor(RenderTreeBuilder b, ref int s)
    {
        AddText(b, ref s, "كود المنتج", _code, v => _code = v, () => _code, _mode != EditorMode.Create, true, 32);
        AddText(b, ref s, "الاسم بالعربية", _nameAr, v => _nameAr = v, () => _nameAr, !IsEditing, true, 150);
        AddText(b, ref s, "الاسم بالإنجليزية", _nameEn, v => _nameEn = v, () => _nameEn, !IsEditing, false, 150);
        AddSelect(b, ref s, "التصنيف", _categoryId, v => _categoryId = v, () => _categoryId, CategoryOptions.Where(x => !string.IsNullOrEmpty(x.Value)).ToArray(), true);
        AddSelect(b, ref s, "العلامة التجارية", _brandId, v => _brandId = v, () => _brandId, BrandOptions);
        AddSelect(b, ref s, "نوع المنتج", _productType, v => _productType = v ?? ((int)ProductType.Frame).ToString(), () => _productType, ProductTypeOptions, true);
        AddText(b, ref s, "الوصف", _description, v => _description = v, () => _description, !IsEditing, false, 500);
        AddCheckbox(b, ref s, "صنف مخزني", _isStockItem, v => _isStockItem = v);
        AddActive(b, ref s);
    }

    private void RenderVariantEditor(RenderTreeBuilder b, ref int s)
    {
        AddSelect(b, ref s, "المنتج", _productId, v => _productId = v, () => _productId, ProductOptions, true, _mode != EditorMode.Create);
        AddText(b, ref s, "SKU", _code, v => _code = v, () => _code, _mode != EditorMode.Create, true, 64);
        AddText(b, ref s, "الباركود", _barcode, v => _barcode = v, () => _barcode, !IsEditing, false, 64);
        AddText(b, ref s, "اسم المتغير", _variantName, v => _variantName = v, () => _variantName, !IsEditing, false, 100);
        AddText(b, ref s, "اللون", _color, v => _color = v, () => _color, !IsEditing, false, 50);
        AddText(b, ref s, "المقاس", _size, v => _size = v, () => _size, !IsEditing, false, 50);
        AddSelect(b, ref s, "الوحدة", _unitId, v => _unitId = v, () => _unitId, UnitOptions);
        AddText(b, ref s, "سعر الشراء", _purchasePrice, v => _purchasePrice = v, () => _purchasePrice, !IsEditing, true);
        AddText(b, ref s, "سعر البيع", _sellingPrice, v => _sellingPrice = v, () => _sellingPrice, !IsEditing, true);
        AddActive(b, ref s);
    }

    private void AddActive(RenderTreeBuilder b, ref int s) => AddCheckbox(b, ref s, "نشط", _isActive, v => _isActive = v, _mode == EditorMode.Create);

    private void AddText(RenderTreeBuilder b, ref int s, string label, string? value, Action<string> changed, Expression<Func<string?>> expression, bool disabled, bool required = false, int? maxLength = null)
    {
        b.OpenComponent<OAS.UiLib.Components.Inputs.UiInputText>(s++);
        b.AddAttribute(s++, "Label", label); b.AddAttribute(s++, "Value", value ?? string.Empty);
        b.AddAttribute(s++, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => SetField(label, () => changed(v ?? string.Empty))));
        b.AddAttribute(s++, "ValueExpression", expression);
        b.AddAttribute(s++, "Disabled", disabled); b.AddAttribute(s++, "Required", required); b.AddAttribute(s++, "Size", ControlSize.Small);
        b.AddAttribute(s++, "ErrorText", ErrorFor(label));
        if (!required) b.AddAttribute(s++, "Placeholder", "(اختياري)");
        if (maxLength.HasValue) b.AddAttribute(s++, "MaxLength", maxLength.Value);
        b.CloseComponent();
    }

    private void AddSelect(RenderTreeBuilder b, ref int s, string label, string? value, Action<string?> changed, Expression<Func<string?>> expression, IReadOnlyList<UiSelectOption> options, bool required = false, bool forceDisabled = false)
    {
        b.OpenComponent<OAS.UiLib.Components.Inputs.UiSelect>(s++);
        b.AddAttribute(s++, "Label", label); b.AddAttribute(s++, "Value", value ?? string.Empty);
        b.AddAttribute(s++, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => SetField(label, () => changed(v))));
        b.AddAttribute(s++, "ValueExpression", expression);
        b.AddAttribute(s++, "Options", options.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToArray());
        b.AddAttribute(s++, "Placeholder", required ? "اختر..." : "(اختياري)");
        b.AddAttribute(s++, "Disabled", !IsEditing || forceDisabled);
        b.AddAttribute(s++, "Required", required); b.AddAttribute(s++, "Size", ControlSize.Small);
        b.AddAttribute(s++, "ErrorText", ErrorFor(label));
        b.CloseComponent();
    }

    private void AddCheckbox(RenderTreeBuilder b, ref int s, string label, bool value, Action<bool> changed, bool forceDisabled = false)
    {
        b.OpenComponent<OAS.UiLib.Components.Inputs.UiCheckbox>(s++);
        b.AddAttribute(s++, "Label", label); b.AddAttribute(s++, "Value", value);
        b.AddAttribute(s++, "ValueChanged", EventCallback.Factory.Create<bool>(this, changed));
        b.AddAttribute(s++, "Disabled", !IsEditing || forceDisabled); b.CloseComponent();
    }

    private string ProductName(Guid id) => _products.FirstOrDefault(x => x.Id == id)?.NameAr ?? "—";
    private static string StatusText(bool active) => active ? "نشط" : "غير نشط";
    private static string ProductTypeText(ProductType type) => type switch
    {
        ProductType.Frame => "إطار", ProductType.Lens => "عدسة", ProductType.Sunglasses => "نظارة شمسية",
        ProductType.Accessory => "إكسسوار", ProductType.Service => "خدمة", _ => "أخرى"
    };
    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return TryParseDecimalInput(value, out var parsed) ? parsed : null;
    }

    private static bool TryParseDecimalInput(string? value, out decimal parsed)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed)) return true;
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed);
    }

    private static string? FormatNullableDecimal(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);
    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
