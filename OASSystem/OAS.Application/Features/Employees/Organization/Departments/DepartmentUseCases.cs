using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Application.Features.Employees.Services;
using OAS.Contracts.Features.Employees.Departments;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Organization.Departments;

public sealed record GetDepartmentsQuery(bool ActiveOnly = false) : IQuery<IReadOnlyList<DepartmentDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.DepartmentsView];
}
public sealed record GetDepartmentByIdQuery(Guid Id) : IQuery<DepartmentDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.DepartmentsView];
}
public sealed record CreateDepartmentCommand(CreateDepartmentRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.DepartmentsManage];
}
public sealed record UpdateDepartmentCommand(Guid Id, UpdateDepartmentRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.DepartmentsManage];
}
public sealed record SetDepartmentStatusCommand(Guid Id, SetDepartmentStatusRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.DepartmentsManage];
}

public sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.NameEn).MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Request.NameEn));
        RuleFor(x => x.Request.Notes).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Request.Notes));
    }
}
public sealed class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.RowVersion).NotEmpty().Must(BeBase64).WithErrorCode("row_version_invalid");
    }
    private static bool BeBase64(string value) { try { Convert.FromBase64String(value); return true; } catch { return false; } }
}

public sealed class SetDepartmentStatusValidator : AbstractValidator<SetDepartmentStatusCommand>
{
    public SetDepartmentStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .Must(BeBase64)
            .WithErrorCode("row_version_invalid");
    }

    private static bool BeBase64(string value)
    {
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}

public sealed class GetDepartmentsQueryHandler(IReadRepository<Department, Guid> departments, IReadRepository<Employee, Guid> employees) : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    public async Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken ct)
    {
        var items = await departments.ListAsync(cancellationToken: ct);
        var employeesList = await employees.ListAsync(cancellationToken: ct);
        var depMap = items.ToDictionary(x => x.Id);
        var empMap = employeesList.ToDictionary(x => x.Id);
        return items.Where(x => !request.ActiveOnly || x.IsActive).OrderBy(x => x.NameAr).Select(x => Map(x, depMap, empMap)).ToArray();
    }
    internal static DepartmentDto Map(Department x, IReadOnlyDictionary<Guid, Department> deps, IReadOnlyDictionary<Guid, Employee> emps) => new(x.Id, x.DepartmentCode, x.NameAr, x.NameEn, x.ParentDepartmentId, x.ParentDepartmentId is Guid p && deps.TryGetValue(p, out var parent) ? parent.NameAr : null, x.ManagerEmployeeId, x.ManagerEmployeeId is Guid m && emps.TryGetValue(m, out var manager) ? manager.DisplayName : null, x.IsActive, x.Notes, Convert.ToBase64String(x.RowVersion), x.CreatedAtUtc, x.LastModifiedAtUtc);
}
public sealed class GetDepartmentByIdQueryHandler(IReadRepository<Department, Guid> departments, IReadRepository<Employee, Guid> employees) : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(GetDepartmentByIdQuery request, CancellationToken ct)
    {
        var item = await departments.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException(nameof(Department), request.Id);
        var all = await departments.ListAsync(cancellationToken: ct);
        var employeeList = await employees.ListAsync(cancellationToken: ct);
        return GetDepartmentsQueryHandler.Map(item, all.ToDictionary(x => x.Id), employeeList.ToDictionary(x => x.Id));
    }
}
public sealed class CreateDepartmentCommandHandler(IRepository<Department, Guid> departments, IReadRepository<Employee, Guid> employees, ISequenceNumberGenerator sequences, DepartmentHierarchyValidator hierarchy) : IRequestHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await hierarchy.ValidateParentAsync(id, request.Request.ParentDepartmentId, ct);
        await ValidateManager(request.Request.ManagerEmployeeId, employees, ct);
        var number = await sequences.NextAsync("DepartmentCodeSequence", ct);
        var item = Department.Create(id, $"DEP-{number:000000}", request.Request.NameAr, request.Request.NameEn, request.Request.ParentDepartmentId, request.Request.ManagerEmployeeId, request.Request.IsActive, request.Request.Notes);
        await departments.AddAsync(item, ct);
        return id;
    }
    internal static async Task ValidateManager(Guid? managerId, IReadRepository<Employee, Guid> employees, CancellationToken ct)
    {
        if (managerId is null) return;
        var manager = await employees.GetByIdAsync(managerId.Value, ct) ?? throw new ConflictException("department_manager_not_found", "The selected department manager does not exist.");
        if (!manager.IsActive) throw new ConflictException("department_manager_inactive", "The selected department manager is inactive.");
    }
}
public sealed class UpdateDepartmentCommandHandler(IRepository<Department, Guid> departments, IReadRepository<Employee, Guid> employees, DepartmentHierarchyValidator hierarchy) : IRequestHandler<UpdateDepartmentCommand, Guid>
{
    public async Task<Guid> Handle(UpdateDepartmentCommand request, CancellationToken ct)
    {
        var item = await departments.GetForUpdateAsync(request.Id, ct) ?? throw new NotFoundException(nameof(Department), request.Id);
        if (!Convert.FromBase64String(request.Request.RowVersion).SequenceEqual(item.RowVersion)) throw new ConcurrencyException("The department was changed by another operation. Reload it and try again.");
        if (request.Request.ParentDepartmentId != item.ParentDepartmentId)
            await hierarchy.ValidateParentAsync(item.Id, request.Request.ParentDepartmentId, ct);
        if (request.Request.ManagerEmployeeId != item.ManagerEmployeeId)
            await CreateDepartmentCommandHandler.ValidateManager(request.Request.ManagerEmployeeId, employees, ct);
        item.Update(request.Request.NameAr, request.Request.NameEn, request.Request.ParentDepartmentId, request.Request.ManagerEmployeeId, request.Request.IsActive, request.Request.Notes);
        departments.Update(item);
        return item.Id;
    }
}
public sealed class SetDepartmentStatusCommandHandler(IRepository<Department, Guid> departments) : IRequestHandler<SetDepartmentStatusCommand, Guid>
{
    public async Task<Guid> Handle(SetDepartmentStatusCommand request, CancellationToken ct)
    {
        var item = await departments.GetForUpdateAsync(request.Id, ct) ?? throw new NotFoundException(nameof(Department), request.Id);
        if (!Convert.FromBase64String(request.Request.RowVersion).SequenceEqual(item.RowVersion)) throw new ConcurrencyException("The department was changed by another operation. Reload it and try again.");
        item.SetActive(request.Request.IsActive);
        departments.Update(item);
        return item.Id;
    }
}
