using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.Time;

public sealed class Holiday : AuditableEntity<Guid>
{
    private Holiday()
    {
    }
    private Holiday(Guid id, string code, string nameAr, string? nameEn, DateOnly start, DateOnly end, bool isPaid, bool isActive, string? notes)
    {
        Id=id;
        HolidayCode=code.Trim();
        Update(nameAr, nameEn, start, end, isPaid, isActive, notes);
    }
    public string HolidayCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsPaid { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static Holiday Create(Guid id, string code, string nameAr, string? nameEn, DateOnly start, DateOnly end, bool isPaid, bool isActive, string? notes)
    {
        if (id==Guid.Empty||string.IsNullOrWhiteSpace(code))throw new DomainException("Holiday identity is required.");
        return new(id, code, nameAr, nameEn, start, end, isPaid, isActive, notes);
    }
    public void Update(string nameAr, string? nameEn, DateOnly start, DateOnly end, bool isPaid, bool isActive, string? notes)
    {
        if (string.IsNullOrWhiteSpace(nameAr))throw new DomainException("Holiday name is required.");
        if (end<start)throw new DomainException("Holiday end date cannot be before start date.");
        NameAr=nameAr.Trim();
        NameEn=N(nameEn);
        StartDate=start;
        EndDate=end;
        IsPaid=isPaid;
        IsActive=isActive;
        Notes=N(notes);
        if (NameAr.Length>150||NameEn is
        {
            Length:>150
        }
        ||Notes is
        {
            Length:>500
        }
        )throw new DomainException("Holiday text is too long.");
    }
    public void SetActive(bool active)=>IsActive=active;
    public bool Contains(DateOnly date)=>date>=StartDate&&date<=EndDate;
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
