using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Accounts.Mapping;
using OAS.Application.Accounting.BankAccounts.Mapping;
using OAS.Application.Accounting.CashAccounts.Mapping;
using OAS.Application.Accounting.CashShifts.Mapping;
using OAS.Application.Accounting.CostCenters.Mapping;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Mapping;
using OAS.Application.Accounting.Expenses.Mapping;
using OAS.Application.Accounting.FiscalPeriods.Mapping;
using OAS.Application.Accounting.FiscalYears.Mapping;
using OAS.Application.Accounting.PaymentAllocations.Mapping;
using OAS.Application.Accounting.PaymentVouchers.Mapping;
using OAS.Application.Accounting.Posting;
using OAS.Application.Accounting.PostingProfiles.Mapping;
using OAS.Application.Accounting.Parties;
using OAS.Application.Accounting.MultiCurrency;
using OAS.Application.Accounting.ReceiptVouchers.Mapping;
using OAS.Application.Common.Behaviors;
using OAS.Application.Common.Security;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.CRUD.Commands;
using OAS.Application.CRUD.Handlers;
using OAS.Application.CRUD.Mapping;
using OAS.Application.CRUD.Queries;
using OAS.Application.CRUD.Services;
using OAS.Application.CRUD.Specifications;
using OAS.Application.CRUD.Validation;
using OAS.Application.Inventory;
using OAS.Domain.Common.Entities;

namespace OAS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddApplicationMappers(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddScoped<ICurrentUser, AnonymousCurrentUser>();
        services.TryAddScoped<IPermissionChecker, DenyAllPermissionChecker>();
        services.TryAddScoped<IAccountingDocumentPostingService, AccountingDocumentPostingService>();
        services.TryAddScoped<IPartyAccountProvisioningService, PartyAccountProvisioningService>();
        services.TryAddScoped<IManagedAccountGuard, ManagedAccountGuard>();
        services.TryAddScoped<ICurrencyRoundingService, CurrencyRoundingService>();
        services.TryAddScoped<IExchangeRateResolver, ExchangeRateResolver>();
        services.TryAddScoped<ICounterpartyAccountResolver, CounterpartyAccountResolver>();
        services.TryAddScoped<ISettlementAccountResolver, SettlementAccountResolver>();
        services.TryAddScoped<IVoucherSettlementResolver, VoucherSettlementResolver>();
        services.TryAddScoped<ILinkedAccountingAccountProvisioningService, LinkedAccountingAccountProvisioningService>();
        services.AddInventoryApplication();
        services.AddScoped<OAS.Application.Accounting.Spreadsheets.AccountingSpreadsheetService>();
        services.AddScoped<OAS.Application.Inventory.Spreadsheets.InventorySpreadsheetService>();
        return services;
    }

    public static IServiceCollection AddApplicationMappers(this IServiceCollection services, System.Reflection.Assembly assembly)
    {
        var mapperTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Mapper", StringComparison.Ordinal));

        foreach (var mapperType in mapperTypes)
        {
            services.TryAddScoped(mapperType);

            foreach (var iface in mapperType.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ICrudMapper<,,,,>))
                {
                    services.TryAddScoped(iface, mapperType);
                }
            }
        }

        var specFactoryTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("SpecificationFactory", StringComparison.Ordinal));

        foreach (var specFactoryType in specFactoryTypes)
        {
            services.TryAddScoped(specFactoryType);

            foreach (var iface in specFactoryType.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ICrudSpecificationFactory<>))
                {
                    services.TryAddScoped(iface, specFactoryType);
                }
            }
        }

        return services;
    }

    public static IServiceCollection AddCrudFeature<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>(this IServiceCollection services)
        where TEntity : Entity<TKey> where TKey : notnull
    {
        services.AddScoped<ICrudApplicationService<TKey, TReadDto, TCreateDto, TUpdateDto>, CrudApplicationService<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>>();
        services.TryAddScoped<ICrudSpecificationFactory<TEntity>, ConventionCrudSpecificationFactory<TEntity>>();
        services.TryAddScoped<ICrudMapper<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>, ConventionCrudMapper<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>>();

        services.AddTransient<IRequestHandler<CreateEntityCommand<TEntity, TKey, TCreateDto>, TEntity>, CreateEntityCommandHandler<TEntity, TKey, TCreateDto, TReadDto, TUpdateDto>>();
        services.AddTransient<IRequestHandler<UpdateEntityCommand<TEntity, TKey, TUpdateDto>, TEntity>, UpdateEntityCommandHandler<TEntity, TKey, TUpdateDto, TReadDto, TCreateDto>>();
        services.AddTransient<IRequestHandler<DeleteEntityCommand<TEntity, TKey>>, DeleteEntityCommandHandler<TEntity, TKey>>();
        services.AddTransient<IRequestHandler<GetEntityByIdQuery<TEntity, TKey, TReadDto>, TReadDto>, GetEntityByIdQueryHandler<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>>();
        services.AddTransient<IRequestHandler<GetEntityPageQuery<TEntity, TKey, TReadDto>, OAS.Contracts.Common.Pagination.PagedResult<TReadDto>>, GetEntityPageQueryHandler<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>>();

        services.AddTransient<IValidator<CreateEntityCommand<TEntity, TKey, TCreateDto>>, CreateEntityCommandValidator<TEntity, TKey, TCreateDto>>();
        services.AddTransient<IValidator<UpdateEntityCommand<TEntity, TKey, TUpdateDto>>, UpdateEntityCommandValidator<TEntity, TKey, TUpdateDto>>();
        return services;
    }
}
