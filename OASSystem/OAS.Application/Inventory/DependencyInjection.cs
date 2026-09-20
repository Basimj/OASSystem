using Microsoft.Extensions.DependencyInjection;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.CRUD.Mapping;
using OAS.Application.Inventory.Balances.Mapping;
using OAS.Application.Inventory.Balances.Services;
using OAS.Application.Inventory.Ledger.Mapping;
using OAS.Application.Inventory.Ledger.Services;
using OAS.Application.Inventory.Products.Brands;
using OAS.Application.Inventory.Products.Categories;
using OAS.Application.Inventory.Products.FrameDetails;
using OAS.Application.Inventory.Products.LensDetails;
using OAS.Application.Inventory.Products.Products;
using OAS.Application.Inventory.Products.Units;
using OAS.Application.Inventory.Products.Variants;
using OAS.Application.Inventory.Services;
using OAS.Application.Inventory.StockCounts.Mapping;
using OAS.Application.Inventory.StockCounts.Services;
using OAS.Application.Inventory.Transactions.Mapping;
using OAS.Application.Inventory.Transactions.Services;
using OAS.Application.Inventory.Warehouses;
using OAS.Contracts.Inventory.Products;
using OAS.Contracts.Inventory.Warehouses;
using OAS.Domain.Entities.Inventory;
using DomainFrameDetails = OAS.Domain.Entities.Inventory.FrameDetails;
using DomainLensDetails = OAS.Domain.Entities.Inventory.LensDetails;

namespace OAS.Application.Inventory;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        // 1. Master Data CRUD Features
        services.AddCrudFeature<ProductCategory, Guid, ProductCategoryDto, CreateProductCategoryRequest, UpdateProductCategoryRequest>();
        services.AddScoped<ICrudSpecificationFactory<ProductCategory>, ProductCategorySpecificationFactory>();
        services.AddScoped<ICrudMapper<ProductCategory, Guid, ProductCategoryDto, CreateProductCategoryRequest, UpdateProductCategoryRequest>, ProductCategoryMapper>();
        services.AddScoped<ProductCategoryMapper>();

        services.AddCrudFeature<Brand, Guid, BrandDto, CreateBrandRequest, UpdateBrandRequest>();
        services.AddScoped<ICrudSpecificationFactory<Brand>, BrandSpecificationFactory>();
        services.AddScoped<ICrudMapper<Brand, Guid, BrandDto, CreateBrandRequest, UpdateBrandRequest>, BrandMapper>();
        services.AddScoped<BrandMapper>();

        services.AddCrudFeature<Unit, Guid, UnitDto, CreateUnitRequest, UpdateUnitRequest>();
        services.AddScoped<ICrudSpecificationFactory<Unit>, UnitSpecificationFactory>();
        services.AddScoped<ICrudMapper<Unit, Guid, UnitDto, CreateUnitRequest, UpdateUnitRequest>, UnitMapper>();
        services.AddScoped<UnitMapper>();

        services.AddCrudFeature<Warehouse, Guid, WarehouseDto, CreateWarehouseRequest, UpdateWarehouseRequest>();
        services.AddScoped<ICrudSpecificationFactory<Warehouse>, WarehouseSpecificationFactory>();
        services.AddScoped<ICrudMapper<Warehouse, Guid, WarehouseDto, CreateWarehouseRequest, UpdateWarehouseRequest>, WarehouseMapper>();
        services.AddScoped<WarehouseMapper>();

        services.AddCrudFeature<Product, Guid, ProductDto, CreateProductRequest, UpdateProductRequest>();
        services.AddScoped<ICrudSpecificationFactory<Product>, ProductSpecificationFactory>();
        services.AddScoped<ICrudMapper<Product, Guid, ProductDto, CreateProductRequest, UpdateProductRequest>, ProductMapper>();
        services.AddScoped<ProductMapper>();

        services.AddCrudFeature<ProductVariant, Guid, ProductVariantDto, CreateProductVariantRequest, UpdateProductVariantRequest>();
        services.AddScoped<ICrudSpecificationFactory<ProductVariant>, ProductVariantSpecificationFactory>();
        services.AddScoped<ICrudMapper<ProductVariant, Guid, ProductVariantDto, CreateProductVariantRequest, UpdateProductVariantRequest>, ProductVariantMapper>();
        services.AddScoped<ProductVariantMapper>();

        services.AddCrudFeature<DomainFrameDetails, Guid, FrameDetailsDto, CreateFrameDetailsRequest, UpdateFrameDetailsRequest>();
        services.AddScoped<ICrudSpecificationFactory<DomainFrameDetails>, FrameDetailsSpecificationFactory>();
        services.AddScoped<ICrudMapper<DomainFrameDetails, Guid, FrameDetailsDto, CreateFrameDetailsRequest, UpdateFrameDetailsRequest>, FrameDetailsMapper>();
        services.AddScoped<FrameDetailsMapper>();

        services.AddCrudFeature<DomainLensDetails, Guid, LensDetailsDto, CreateLensDetailsRequest, UpdateLensDetailsRequest>();
        services.AddScoped<ICrudSpecificationFactory<DomainLensDetails>, LensDetailsSpecificationFactory>();
        services.AddScoped<ICrudMapper<DomainLensDetails, Guid, LensDetailsDto, CreateLensDetailsRequest, UpdateLensDetailsRequest>, LensDetailsMapper>();
        services.AddScoped<LensDetailsMapper>();

        // 2. Balances & Ledger
        services.AddScoped<InventoryBalanceMapper>();
        services.AddScoped<IInventoryBalanceService, InventoryBalanceService>();

        services.AddScoped<InventoryLedgerMapper>();
        services.AddScoped<IInventoryLedgerService, InventoryLedgerService>();

        // 3. Posting & Costing Service
        services.AddScoped<IInventoryPostingService, InventoryPostingService>();

        // 4. Transactions
        services.AddScoped<InventoryTransactionMapper>();
        services.AddScoped<InventoryTransactionLineMapper>();
        services.AddScoped<IInventoryTransactionService, InventoryTransactionService>();

        // 5. Stock Counts
        services.AddScoped<StockCountMapper>();
        services.AddScoped<StockCountLineMapper>();
        services.AddScoped<IStockCountService, StockCountService>();

        return services;
    }
}
