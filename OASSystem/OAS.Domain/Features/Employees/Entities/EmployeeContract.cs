using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Entities;

public sealed class EmployeeContract : AuditableEntity<Guid>
{
    private EmployeeContract() { }

    private EmployeeContract(Guid id, string contractCode, Guid employeeId, EmploymentContractType contractType, DateOnly startDate, DateOnly? endDate, DateOnly? probationEndDate, decimal? workingHoursPerDay, decimal? workingDaysPerWeek, Guid currencyId, string? notes)
    {
        if (id == Guid.Empty) throw new DomainException("Contract id is required.");
        if (string.IsNullOrWhiteSpace(contractCode)) throw new DomainException("Contract code is required.");
        if (employeeId == Guid.Empty) throw new DomainException("Employee is required.");
        if (currencyId == Guid.Empty) throw new DomainException("Currency is required.");
        Id = id;
        ContractCode = contractCode.Trim();
        if (ContractCode.Length > 40) throw new DomainException("Contract code cannot exceed 40 characters.");
        EmployeeId = employeeId;
        Status = EmploymentContractStatus.Draft;
        UpdateDraft(contractType, startDate, endDate, probationEndDate, workingHoursPerDay, workingDaysPerWeek, currencyId, notes);
    }

    public string ContractCode { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public EmploymentContractType ContractType { get; private set; }
    public EmploymentContractStatus Status { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly? ProbationEndDate { get; private set; }
    public decimal? WorkingHoursPerDay { get; private set; }
    public decimal? WorkingDaysPerWeek { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string? Notes { get; private set; }
    public string? ActivatedBy { get; private set; }
    public DateTimeOffset? ActivatedAtUtc { get; private set; }
    public string? TerminatedBy { get; private set; }
    public DateTimeOffset? TerminatedAtUtc { get; private set; }
    public DateOnly? TerminationEffectiveDate { get; private set; }
    public string? TerminationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static EmployeeContract Create(Guid id, string contractCode, Guid employeeId, EmploymentContractType contractType, DateOnly startDate, DateOnly? endDate, DateOnly? probationEndDate, decimal? workingHoursPerDay, decimal? workingDaysPerWeek, Guid currencyId, string? notes)
        => new(id, contractCode, employeeId, contractType, startDate, endDate, probationEndDate, workingHoursPerDay, workingDaysPerWeek, currencyId, notes);

    public void UpdateDraft(EmploymentContractType contractType, DateOnly startDate, DateOnly? endDate, DateOnly? probationEndDate, decimal? workingHoursPerDay, decimal? workingDaysPerWeek, Guid currencyId, string? notes)
    {
        if (Status != EmploymentContractStatus.Draft) throw new DomainException("Only draft contracts can be edited.");
        if (!Enum.IsDefined(contractType)) throw new DomainException("Contract type is invalid.");
        if (contractType == EmploymentContractType.FixedTerm && endDate is null) throw new DomainException("Fixed-term contracts require an end date.");
        if (endDate.HasValue && endDate.Value < startDate) throw new DomainException("Contract end date cannot be before start date.");
        if (probationEndDate.HasValue && probationEndDate.Value < startDate) throw new DomainException("Probation end date cannot be before start date.");
        if (probationEndDate.HasValue && endDate.HasValue && probationEndDate.Value > endDate.Value) throw new DomainException("Probation end date cannot be after contract end date.");
        if (workingHoursPerDay.HasValue && (workingHoursPerDay.Value <= 0 || workingHoursPerDay.Value > 24)) throw new DomainException("Working hours per day must be greater than zero and not exceed 24.");
        if (workingDaysPerWeek.HasValue && (workingDaysPerWeek.Value <= 0 || workingDaysPerWeek.Value > 7)) throw new DomainException("Working days per week must be greater than zero and not exceed 7.");
        if (currencyId == Guid.Empty) throw new DomainException("Currency is required.");
        notes = Normalize(notes);
        if (notes is { Length: > 1000 }) throw new DomainException("Contract notes cannot exceed 1000 characters.");

        ContractType = contractType;
        StartDate = startDate;
        EndDate = endDate;
        ProbationEndDate = probationEndDate;
        WorkingHoursPerDay = workingHoursPerDay;
        WorkingDaysPerWeek = workingDaysPerWeek;
        CurrencyId = currencyId;
        Notes = notes;
    }

    public void Activate(string? actor, DateTimeOffset atUtc)
    {
        if (Status != EmploymentContractStatus.Draft) throw new DomainException("Only draft contracts can be activated.");
        Status = EmploymentContractStatus.Active;
        ActivatedBy = Normalize(actor);
        ActivatedAtUtc = atUtc;
    }

    public void Terminate(string reason, string? actor, DateTimeOffset atUtc, DateOnly? effectiveDate = null)
    {
        if (Status != EmploymentContractStatus.Active) throw new DomainException("Only active contracts can be terminated.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Termination reason is required.");
        var terminationDate = effectiveDate ?? DateOnly.FromDateTime(atUtc.UtcDateTime);
        if (terminationDate < StartDate) throw new DomainException("Termination effective date cannot be before contract start date.");
        if (EndDate.HasValue && terminationDate > EndDate.Value) throw new DomainException("Termination effective date cannot be after contract end date.");
        reason = reason.Trim();
        if (reason.Length > 500) throw new DomainException("Termination reason cannot exceed 500 characters.");
        Status = EmploymentContractStatus.Terminated;
        TerminationReason = reason;
        TerminatedBy = Normalize(actor);
        TerminatedAtUtc = atUtc;
        TerminationEffectiveDate = terminationDate;
    }

    public void Cancel()
    {
        if (Status != EmploymentContractStatus.Draft) throw new DomainException("Only draft contracts can be cancelled.");
        Status = EmploymentContractStatus.Cancelled;
    }

    public void MarkExpired()
    {
        if (Status != EmploymentContractStatus.Active) throw new DomainException("Only active contracts can expire.");
        Status = EmploymentContractStatus.Expired;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
