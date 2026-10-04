using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Application.Features.Employees.Services;
using OAS.Contracts.Features.Employees.Settings;
using OAS.Domain.Features.Employees.Settings;

namespace OAS.Application.Features.Employees.Settings;
public sealed record GetHrSettingsQuery : IQuery<HrSettingsDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.SettingsView];
}
public sealed record UpdateHrSettingsCommand(UpdateHrSettingsRequest Request):ICommand<HrSettingsDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.SettingsManage];
}

public sealed class UpdateHrSettingsValidator : AbstractValidator<UpdateHrSettingsCommand>
{
    public UpdateHrSettingsValidator()
    {
        RuleFor(x=>x.Request.LeaveYearStartMonth).InclusiveBetween((byte)1, (byte)12);
        RuleFor(x=>x.Request.TimeZoneId).MaximumLength(128).When(x=>!string.IsNullOrWhiteSpace(x.Request.TimeZoneId));
    }
}

public sealed class GetHrSettingsQueryHandler(IReadRepository<HrSettings, Guid> repo):IRequestHandler<GetHrSettingsQuery, HrSettingsDto>
{
    public async Task<HrSettingsDto> Handle(GetHrSettingsQuery r, CancellationToken ct)
    {
        var x=await repo.GetByIdAsync(HrSettings.SingletonId, ct);
        return x is null?new(HrSettings.SingletonId, null, 1, true, string.Empty):Map(x);
    }
    internal static HrSettingsDto Map(HrSettings x)=>new(x.Id, x.TimeZoneId, x.LeaveYearStartMonth, x.RequireAttendanceApproval, Convert.ToBase64String(x.RowVersion));
}

public sealed class UpdateHrSettingsCommandHandler(IRepository<HrSettings, Guid> repo, IHRTimeZoneService zones):IRequestHandler<UpdateHrSettingsCommand, HrSettingsDto>
{
    public async Task<HrSettingsDto> Handle(UpdateHrSettingsCommand r, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(r.Request.TimeZoneId))zones.Resolve(r.Request.TimeZoneId);
        var x=await repo.GetForUpdateAsync(HrSettings.SingletonId, ct);
        if (x is null)
        {
            x=HrSettings.Create(r.Request.TimeZoneId, r.Request.LeaveYearStartMonth, r.Request.RequireAttendanceApproval);
            await repo.AddAsync(x, ct);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(r.Request.RowVersion))HrOperationsHelpers.EnsureRowVersion(r.Request.RowVersion!, x.RowVersion, "HR settings");
            x.Update(r.Request.TimeZoneId, r.Request.LeaveYearStartMonth, r.Request.RequireAttendanceApproval);
            repo.Update(x);
        }
        return GetHrSettingsQueryHandler.Map(x);
    }
}
