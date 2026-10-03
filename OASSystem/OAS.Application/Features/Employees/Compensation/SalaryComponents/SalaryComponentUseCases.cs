using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees.Compensation;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Application.Features.Employees.Compensation.SalaryComponents;

public sealed record GetSalaryComponentsQuery(bool ActiveOnly = false) : IQuery<IReadOnlyList<SalaryComponentDto>>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.SalaryComponentsView]; }
public sealed record GetSalaryComponentByIdQuery(Guid Id) : IQuery<SalaryComponentDto>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.SalaryComponentsView]; }
public sealed record CreateSalaryComponentCommand(CreateSalaryComponentRequest Request) : ICommand<Guid>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.SalaryComponentsManage]; }
public sealed record UpdateSalaryComponentCommand(Guid Id, UpdateSalaryComponentRequest Request) : ICommand<Guid>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.SalaryComponentsManage]; }

public sealed class CreateSalaryComponentValidator : AbstractValidator<CreateSalaryComponentCommand>
{
    public CreateSalaryComponentValidator()
    {
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.NameEn).MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Request.NameEn));
        RuleFor(x => x.Request.ComponentType).Must(v => Enum.IsDefined(typeof(SalaryComponentType), v));
        RuleFor(x => x.Request.CalculationMethod).Must(v => Enum.IsDefined(typeof(SalaryCalculationMethod), v));
        RuleFor(x => x.Request.DebitPostingRole).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Request.DebitPostingRole));
        RuleFor(x => x.Request.CreditPostingRole).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Request.CreditPostingRole));
        RuleFor(x => x.Request.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.Notes).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Request.Notes));
    }
}
public sealed class UpdateSalaryComponentValidator : AbstractValidator<UpdateSalaryComponentCommand>
{
    public UpdateSalaryComponentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.NameEn).MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Request.NameEn));
        RuleFor(x => x.Request.ComponentType).Must(v => Enum.IsDefined(typeof(SalaryComponentType), v));
        RuleFor(x => x.Request.CalculationMethod).Must(v => Enum.IsDefined(typeof(SalaryCalculationMethod), v));
        RuleFor(x => x.Request.DebitPostingRole).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Request.DebitPostingRole));
        RuleFor(x => x.Request.CreditPostingRole).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Request.CreditPostingRole));
        RuleFor(x => x.Request.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.Notes).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Request.Notes));
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }
    private static bool BeBase64(string value) { try { Convert.FromBase64String(value); return true; } catch { return false; } }
}

internal static class SalaryComponentMapping
{
    public static SalaryComponentDto ToDto(SalaryComponent x) => new(x.Id, x.ComponentCode, x.NameAr, x.NameEn, (byte)x.ComponentType, (byte)x.CalculationMethod, x.IsBasicSalary, x.IsRecurring, x.IsTaxable, x.IsActive, x.DebitPostingRole, x.CreditPostingRole, x.DisplayOrder, x.Notes, Convert.ToBase64String(x.RowVersion));
}
public sealed class GetSalaryComponentsQueryHandler(IReadRepository<SalaryComponent, Guid> repository) : IRequestHandler<GetSalaryComponentsQuery, IReadOnlyList<SalaryComponentDto>>
{
    public async Task<IReadOnlyList<SalaryComponentDto>> Handle(GetSalaryComponentsQuery request, CancellationToken ct) => (await repository.ListAsync(cancellationToken: ct)).Where(x => !request.ActiveOnly || x.IsActive).OrderBy(x => x.DisplayOrder).ThenBy(x => x.NameAr).Select(SalaryComponentMapping.ToDto).ToArray();
}
public sealed class GetSalaryComponentByIdQueryHandler(IReadRepository<SalaryComponent, Guid> repository) : IRequestHandler<GetSalaryComponentByIdQuery, SalaryComponentDto>
{
    public async Task<SalaryComponentDto> Handle(GetSalaryComponentByIdQuery request, CancellationToken ct) => SalaryComponentMapping.ToDto(await repository.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException(nameof(SalaryComponent), request.Id));
}
public sealed class CreateSalaryComponentCommandHandler(IRepository<SalaryComponent, Guid> repository, ISequenceNumberGenerator sequences) : IRequestHandler<CreateSalaryComponentCommand, Guid>
{
    public async Task<Guid> Handle(CreateSalaryComponentCommand request, CancellationToken ct)
    {
        await EnsureBasicUnique(request.Request.IsBasicSalary && request.Request.IsActive, null, repository, ct);
        var number = await sequences.NextAsync("SalaryComponentCodeSequence", ct);
        var item = SalaryComponent.Create(Guid.NewGuid(), $"SAL-{number:000000}", request.Request.NameAr, request.Request.NameEn, (SalaryComponentType)request.Request.ComponentType, (SalaryCalculationMethod)request.Request.CalculationMethod, request.Request.IsBasicSalary, request.Request.IsRecurring, request.Request.IsTaxable, request.Request.IsActive, request.Request.DebitPostingRole, request.Request.CreditPostingRole, request.Request.DisplayOrder, request.Request.Notes);
        await repository.AddAsync(item, ct);
        return item.Id;
    }
    internal static async Task EnsureBasicUnique(bool isBasic, Guid? currentId, IReadRepository<SalaryComponent, Guid> repository, CancellationToken ct)
    {
        if (!isBasic) return;
        if (await repository.CountAsync(new Specification<SalaryComponent>().Where(x => x.IsBasicSalary && x.IsActive && (!currentId.HasValue || x.Id != currentId.Value)), ct) > 0)
            throw new ConflictException("salary_component_basic_exists", "An active basic salary component already exists.");
    }
}
public sealed class UpdateSalaryComponentCommandHandler(IRepository<SalaryComponent, Guid> repository) : IRequestHandler<UpdateSalaryComponentCommand, Guid>
{
    public async Task<Guid> Handle(UpdateSalaryComponentCommand request, CancellationToken ct)
    {
        var item = await repository.GetForUpdateAsync(request.Id, ct) ?? throw new NotFoundException(nameof(SalaryComponent), request.Id);
        if (!Convert.FromBase64String(request.Request.RowVersion).SequenceEqual(item.RowVersion)) throw new ConcurrencyException("The salary component was changed by another operation. Reload it and try again.");
        await CreateSalaryComponentCommandHandler.EnsureBasicUnique(request.Request.IsBasicSalary && request.Request.IsActive, item.Id, repository, ct);
        item.Update(request.Request.NameAr, request.Request.NameEn, (SalaryComponentType)request.Request.ComponentType, (SalaryCalculationMethod)request.Request.CalculationMethod, request.Request.IsBasicSalary, request.Request.IsRecurring, request.Request.IsTaxable, request.Request.IsActive, request.Request.DebitPostingRole, request.Request.CreditPostingRole, request.Request.DisplayOrder, request.Request.Notes);
        repository.Update(item);
        return item.Id;
    }
}
