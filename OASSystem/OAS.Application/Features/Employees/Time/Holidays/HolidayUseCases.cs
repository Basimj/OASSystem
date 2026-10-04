using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Application.Features.Employees.Services;
using OAS.Contracts.Features.Employees.Time;
using OAS.Domain.Features.Employees.Time;

namespace OAS.Application.Features.Employees.Time.Holidays;
public sealed record GetHolidaysQuery(bool ActiveOnly=false):IQuery<IReadOnlyList<HolidayDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.HolidaysView];
}
public sealed record CreateHolidayCommand(CreateHolidayRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.HolidaysManage];
}
public sealed record UpdateHolidayCommand(Guid Id, UpdateHolidayRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.HolidaysManage];
}
public sealed record SetHolidayStatusCommand(Guid Id, SetHolidayStatusRequest Request):ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get;
    }
    =[HrPermissions.HolidaysManage];
}

public sealed class CreateHolidayValidator : AbstractValidator<CreateHolidayCommand>
{
    public CreateHolidayValidator()
    {
        RuleFor(x=>x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x=>x.Request.EndDate).GreaterThanOrEqualTo(x=>x.Request.StartDate);
    }
}

public sealed class GetHolidaysQueryHandler(IReadRepository<Holiday, Guid> repo):IRequestHandler<GetHolidaysQuery, IReadOnlyList<HolidayDto>>
{
    public async Task<IReadOnlyList<HolidayDto>> Handle(GetHolidaysQuery r, CancellationToken ct)=>(await repo.ListAsync(cancellationToken:ct)).Where(x=>!r.ActiveOnly||x.IsActive).OrderBy(x=>x.StartDate).Select(Map).ToArray();
    internal static HolidayDto Map(Holiday x)=>new(x.Id, x.HolidayCode, x.NameAr, x.NameEn, x.StartDate, x.EndDate, x.IsPaid, x.IsActive, x.Notes, Convert.ToBase64String(x.RowVersion));
}

public sealed class CreateHolidayCommandHandler(IRepository<Holiday, Guid> repo, ISequenceNumberGenerator seq):IRequestHandler<CreateHolidayCommand, Guid>
{
    public async Task<Guid> Handle(CreateHolidayCommand r, CancellationToken ct)
    {
        var id=Guid.NewGuid();
        var n=await seq.NextAsync("HolidayCodeSequence", ct);
        await repo.AddAsync(Holiday.Create(id, $"HOL-{n:000000}", r.Request.NameAr, r.Request.NameEn, r.Request.StartDate, r.Request.EndDate, r.Request.IsPaid, r.Request.IsActive, r.Request.Notes), ct);
        return id;
    }
}

public sealed class UpdateHolidayCommandHandler(IRepository<Holiday, Guid> repo):IRequestHandler<UpdateHolidayCommand, Guid>
{
    public async Task<Guid> Handle(UpdateHolidayCommand r, CancellationToken ct)
    {
        var x=await repo.GetForUpdateAsync(r.Id, ct)??throw new NotFoundException(nameof(Holiday), r.Id);
        HrOperationsHelpers.EnsureRowVersion(r.Request.RowVersion, x.RowVersion, "holiday");
        x.Update(r.Request.NameAr, r.Request.NameEn, r.Request.StartDate, r.Request.EndDate, r.Request.IsPaid, r.Request.IsActive, r.Request.Notes);
        repo.Update(x);
        return x.Id;
    }
}

public sealed class SetHolidayStatusCommandHandler(IRepository<Holiday, Guid> repo):IRequestHandler<SetHolidayStatusCommand, Guid>
{
    public async Task<Guid> Handle(SetHolidayStatusCommand r, CancellationToken ct)
    {
        var x=await repo.GetForUpdateAsync(r.Id, ct)??throw new NotFoundException(nameof(Holiday), r.Id);
        HrOperationsHelpers.EnsureRowVersion(r.Request.RowVersion, x.RowVersion, "holiday");
        x.SetActive(r.Request.IsActive);
        repo.Update(x);
        return x.Id;
    }
}
