using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Application.Features.Employees.Services;
using OAS.Contracts.Features.Employees.Loans;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.Loans;

namespace OAS.Application.Features.Employees.Loans;

public sealed record GetEmployeeLoansQuery(Guid? EmployeeId = null, byte? Status = null)
    : IQuery<IReadOnlyList<EmployeeLoanDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LoansView];
}

public sealed record GetEmployeeLoanByIdQuery(Guid Id)
    : IQuery<EmployeeLoanDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LoansView];
}

public sealed record CreateEmployeeLoanCommand(CreateEmployeeLoanRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LoansCreate];
}

public sealed record UpdateEmployeeLoanCommand(Guid Id, UpdateEmployeeLoanRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LoansCreate];
}

public sealed record SubmitEmployeeLoanCommand(Guid Id, EmployeeLoanTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LoansCreate];
}

public sealed record ApproveEmployeeLoanCommand(Guid Id, EmployeeLoanTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LoansApprove];
}

public sealed record RejectEmployeeLoanCommand(Guid Id, EmployeeLoanTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LoansApprove];
}

public sealed record CancelEmployeeLoanCommand(Guid Id, EmployeeLoanTransitionRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LoansCreate];
}

public sealed record DisburseEmployeeLoanCommand(Guid Id, DisburseEmployeeLoanRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LoansDisburse];
}

public sealed record PayLoanInstallmentExternallyCommand(
    Guid InstallmentId,
    PayLoanInstallmentExternallyRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [HrPermissions.LoansRepayment];
}

public sealed class CreateEmployeeLoanValidator : AbstractValidator<CreateEmployeeLoanCommand>
{
    public CreateEmployeeLoanValidator()
    {
        RuleFor(x => x.Request.EmployeeId).NotEmpty();
        RuleFor(x => x.Request.PrincipalAmount).GreaterThan(0);
        RuleFor(x => x.Request.InstallmentCount).GreaterThan(0);
        RuleFor(x => x.Request.RepaymentMode).InclusiveBetween((byte)1, (byte)3);
    }
}

public sealed class GetEmployeeLoansQueryHandler(
    IReadRepository<EmployeeLoan, Guid> loans,
    IReadRepository<EmployeeLoanInstallment, Guid> installments,
    IReadRepository<Employee, Guid> employees)
    : IRequestHandler<GetEmployeeLoansQuery, IReadOnlyList<EmployeeLoanDto>>
{
    public async Task<IReadOnlyList<EmployeeLoanDto>> Handle(
        GetEmployeeLoansQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await loans.ListAsync(
            new Specification<EmployeeLoan>().Where(x =>
                (!request.EmployeeId.HasValue || x.EmployeeId == request.EmployeeId.Value) &&
                (!request.Status.HasValue || (byte)x.Status == request.Status.Value)),
            cancellationToken);

        var allInstallments = await installments.ListAsync(cancellationToken: cancellationToken);
        var employeeMap = (await employees.ListAsync(cancellationToken: cancellationToken))
            .ToDictionary(x => x.Id);

        return rows
            .OrderByDescending(x => x.LoanDate)
            .Select(x => Map(
                x,
                allInstallments
                    .Where(i => i.EmployeeLoanId == x.Id)
                    .OrderBy(i => i.InstallmentSequence)
                    .ToArray(),
                employeeMap.TryGetValue(x.EmployeeId, out var employee) ? employee : null))
            .ToArray();
    }

    internal static EmployeeLoanDto Map(
        EmployeeLoan loan,
        IReadOnlyList<EmployeeLoanInstallment> installments,
        Employee? employee)
    {
        var paidAmount = installments
            .Where(x => x.Status is LoanInstallmentStatus.Deducted or LoanInstallmentStatus.PaidExternally)
            .Sum(x => x.Amount);

        var isDisbursed = loan.Status is EmployeeLoanStatus.Active or EmployeeLoanStatus.Completed;
        var outstandingAmount = isDisbursed
            ? Math.Max(0, loan.PrincipalAmount - paidAmount)
            : 0;

        return new EmployeeLoanDto(
            loan.Id,
            loan.LoanCode,
            loan.EmployeeId,
            employee?.EmployeeCode ?? string.Empty,
            employee?.DisplayName ?? string.Empty,
            loan.ContractId,
            loan.SalaryStructureId,
            loan.LoanDate,
            loan.CurrencyId,
            loan.CurrencyCodeSnapshot,
            loan.CurrencySymbolSnapshot,
            loan.CurrencyDecimalPlacesSnapshot,
            loan.PrincipalAmount,
            loan.InstallmentCount,
            loan.FirstInstallmentDate,
            (byte)loan.RepaymentMode,
            (byte)loan.Status,
            loan.Reason,
            loan.PaymentVoucherId,
            isDisbursed ? paidAmount : 0,
            outstandingAmount,
            installments.Select(x => new EmployeeLoanInstallmentDto(
                    x.Id,
                    x.InstallmentSequence,
                    x.DueDate,
                    x.Amount,
                    (byte)x.Status,
                    x.EmployeePayrollId,
                    x.ReceiptVoucherId,
                    Convert.ToBase64String(x.RowVersion)))
                .ToArray(),
            Convert.ToBase64String(loan.RowVersion));
    }
}

public sealed class GetEmployeeLoanByIdQueryHandler(
    IReadRepository<EmployeeLoan, Guid> loans,
    IReadRepository<EmployeeLoanInstallment, Guid> installments,
    IReadRepository<Employee, Guid> employees)
    : IRequestHandler<GetEmployeeLoanByIdQuery, EmployeeLoanDto>
{
    public async Task<EmployeeLoanDto> Handle(
        GetEmployeeLoanByIdQuery request,
        CancellationToken cancellationToken)
    {
        var loan = await loans.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeLoan), request.Id);

        var rows = await installments.ListAsync(
            new Specification<EmployeeLoanInstallment>().Where(x => x.EmployeeLoanId == loan.Id),
            cancellationToken);

        var employee = await employees.GetByIdAsync(loan.EmployeeId, cancellationToken);

        return GetEmployeeLoansQueryHandler.Map(
            loan,
            rows.OrderBy(x => x.InstallmentSequence).ToArray(),
            employee);
    }
}

public sealed class CreateEmployeeLoanCommandHandler(
    IRepository<EmployeeLoan, Guid> loans,
    IRepository<EmployeeLoanInstallment, Guid> installments,
    IReadRepository<Employee, Guid> employees,
    IEmployeeSalaryStructureRepository salaryStructures,
    IReadRepository<EmployeeContract, Guid> contracts,
    IHRAccountingReferencePort accounting,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateEmployeeLoanCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateEmployeeLoanCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await employees.GetByIdAsync(request.Request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.Request.EmployeeId);

        if (!employee.IsActive)
        {
            throw new ConflictException(
                "employee_inactive",
                "Inactive employees cannot receive new loans.");
        }

        var salaryStructure = await salaryStructures.GetActiveForEmployeeAsync(
                employee.Id,
                false,
                cancellationToken)
            ?? throw new ConflictException(
                "salary_structure_active_required",
                "An active salary structure is required before creating an employee loan.");

        var contract = salaryStructure.ContractId.HasValue
            ? await contracts.GetByIdAsync(salaryStructure.ContractId.Value, cancellationToken)
            : null;

        var currency = await accounting.GetCurrencyAsync(
                salaryStructure.CurrencyId,
                cancellationToken)
            ?? throw new ConflictException("loan_currency_missing", "Salary currency does not exist.");

        if (!currency.IsActive)
        {
            throw new ConflictException("loan_currency_inactive", "Salary currency is inactive.");
        }

        ValidatePrincipalPrecision(request.Request.PrincipalAmount, currency.DecimalPlaces);

        var sequence = await sequenceNumberGenerator.NextAsync(
            "EmployeeLoanCodeSequence",
            cancellationToken);

        var id = Guid.NewGuid();
        var loan = EmployeeLoan.Create(
            id,
            $"ELN-{request.Request.LoanDate.Year:0000}-{sequence:000000}",
            employee.Id,
            contract?.Id,
            salaryStructure.Id,
            request.Request.LoanDate,
            currency.Id,
            currency.Code,
            currency.Symbol,
            currency.DecimalPlaces,
            request.Request.PrincipalAmount,
            request.Request.InstallmentCount,
            request.Request.FirstInstallmentDate,
            (LoanRepaymentMode)request.Request.RepaymentMode,
            request.Request.Reason);

        await loans.AddAsync(loan, cancellationToken);
        await installments.AddRangeAsync(BuildInstallments(loan), cancellationToken);
        return id;
    }

    internal static IReadOnlyList<EmployeeLoanInstallment> BuildInstallments(EmployeeLoan loan)
    {
        ValidatePrincipalPrecision(loan.PrincipalAmount, loan.CurrencyDecimalPlacesSnapshot);

        var factor = DecimalFactor(loan.CurrencyDecimalPlacesSnapshot);
        var totalMinorUnits = decimal.Truncate(loan.PrincipalAmount * factor);
        var baseMinorUnits = decimal.Floor(totalMinorUnits / loan.InstallmentCount);

        if (baseMinorUnits <= 0)
        {
            throw new ConflictException(
                "loan_installment_amount_too_small",
                "The principal is too small for the requested installment count and currency precision.");
        }

        var remainder = checked((int)(totalMinorUnits - baseMinorUnits * loan.InstallmentCount));
        var result = new List<EmployeeLoanInstallment>(loan.InstallmentCount);

        for (var sequence = 1; sequence <= loan.InstallmentCount; sequence++)
        {
            var minorUnits = baseMinorUnits + (sequence <= remainder ? 1 : 0);
            var amount = minorUnits / factor;
            var dueDate = loan.FirstInstallmentDate.AddMonths(sequence - 1);

            result.Add(EmployeeLoanInstallment.Create(
                Guid.NewGuid(),
                loan.Id,
                sequence,
                dueDate,
                amount));
        }

        if (result.Sum(x => x.Amount) != loan.PrincipalAmount)
        {
            throw new ConflictException(
                "loan_installment_rounding_error",
                "Installment schedule does not equal loan principal.");
        }

        return result;
    }

    internal static void ValidatePrincipalPrecision(decimal principalAmount, byte decimalPlaces)
    {
        if (decimalPlaces > 4)
        {
            throw new ConflictException(
                "loan_currency_precision_unsupported",
                "Employee loans support currencies with up to four decimal places.");
        }

        if (Math.Round(principalAmount, decimalPlaces, MidpointRounding.AwayFromZero) != principalAmount)
        {
            throw new ConflictException(
                "loan_amount_precision_invalid",
                "Loan amount contains more decimal places than the selected currency allows.");
        }
    }

    private static decimal DecimalFactor(byte decimalPlaces)
    {
        decimal factor = 1;
        for (var i = 0; i < decimalPlaces; i++)
        {
            factor *= 10;
        }

        return factor;
    }
}

public sealed class UpdateEmployeeLoanCommandHandler(
    IRepository<EmployeeLoan, Guid> loans,
    IRepository<EmployeeLoanInstallment, Guid> installments)
    : IRequestHandler<UpdateEmployeeLoanCommand, Guid>
{
    public async Task<Guid> Handle(
        UpdateEmployeeLoanCommand request,
        CancellationToken cancellationToken)
    {
        var loan = await loans.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeLoan), request.Id);

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            loan.RowVersion,
            "employee loan");

        var existingInstallments = await installments.ListAsync(
            new Specification<EmployeeLoanInstallment>()
                .Where(x => x.EmployeeLoanId == loan.Id)
                .Tracking(),
            cancellationToken);

        if (existingInstallments.Any(x => x.Status != LoanInstallmentStatus.Pending))
        {
            throw new ConflictException(
                "loan_installments_locked",
                "Loan installments can no longer be regenerated.");
        }

        CreateEmployeeLoanCommandHandler.ValidatePrincipalPrecision(
            request.Request.PrincipalAmount,
            loan.CurrencyDecimalPlacesSnapshot);

        loan.UpdateDraft(
            request.Request.PrincipalAmount,
            request.Request.InstallmentCount,
            request.Request.FirstInstallmentDate,
            (LoanRepaymentMode)request.Request.RepaymentMode,
            request.Request.Reason);

        loans.Update(loan);
        installments.DeleteRange(existingInstallments);
        await installments.AddRangeAsync(
            CreateEmployeeLoanCommandHandler.BuildInstallments(loan),
            cancellationToken);

        return loan.Id;
    }
}

public abstract class EmployeeLoanTransitionBase(
    IRepository<EmployeeLoan, Guid> loans,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    protected async Task<EmployeeLoan> LoadAsync(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        var loan = await loans.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeLoan), id);

        HrOperationsHelpers.EnsureRowVersion(rowVersion, loan.RowVersion, "employee loan");
        return loan;
    }

    protected string? Actor => HrOperationsHelpers.Actor(currentUser);
    protected DateTimeOffset Now => timeProvider.GetUtcNow();
    protected void Save(EmployeeLoan loan) => loans.Update(loan);
}

public sealed class SubmitEmployeeLoanCommandHandler(
    IRepository<EmployeeLoan, Guid> loans,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : EmployeeLoanTransitionBase(loans, currentUser, timeProvider),
      IRequestHandler<SubmitEmployeeLoanCommand, Guid>
{
    public async Task<Guid> Handle(
        SubmitEmployeeLoanCommand request,
        CancellationToken cancellationToken)
    {
        var loan = await LoadAsync(request.Id, request.Request.RowVersion, cancellationToken);
        loan.Submit(Actor, Now);
        Save(loan);
        return loan.Id;
    }
}

public sealed class ApproveEmployeeLoanCommandHandler(
    IRepository<EmployeeLoan, Guid> loans,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : EmployeeLoanTransitionBase(loans, currentUser, timeProvider),
      IRequestHandler<ApproveEmployeeLoanCommand, Guid>
{
    public async Task<Guid> Handle(
        ApproveEmployeeLoanCommand request,
        CancellationToken cancellationToken)
    {
        var loan = await LoadAsync(request.Id, request.Request.RowVersion, cancellationToken);
        loan.Approve(Actor, Now);
        Save(loan);
        return loan.Id;
    }
}

public sealed class RejectEmployeeLoanCommandHandler(
    IRepository<EmployeeLoan, Guid> loans,
    IRepository<EmployeeLoanInstallment, Guid> installments,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : EmployeeLoanTransitionBase(loans, currentUser, timeProvider),
      IRequestHandler<RejectEmployeeLoanCommand, Guid>
{
    public async Task<Guid> Handle(
        RejectEmployeeLoanCommand request,
        CancellationToken cancellationToken)
    {
        var loan = await LoadAsync(request.Id, request.Request.RowVersion, cancellationToken);
        loan.Reject(request.Request.Reason ?? string.Empty, Actor, Now);
        Save(loan);
        await EmployeeLoanInstallmentLifecycle.CancelUnpaidInstallmentsAsync(installments, loan.Id, cancellationToken);
        return loan.Id;
    }
}

public sealed class CancelEmployeeLoanCommandHandler(
    IRepository<EmployeeLoan, Guid> loans,
    IRepository<EmployeeLoanInstallment, Guid> installments,
    IEmployeeHrOperationLock operationLock,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : EmployeeLoanTransitionBase(loans, currentUser, timeProvider),
      IRequestHandler<CancelEmployeeLoanCommand, Guid>
{
    public async Task<Guid> Handle(
        CancelEmployeeLoanCommand request,
        CancellationToken cancellationToken)
    {
        var loan = await LoadAsync(request.Id, request.Request.RowVersion, cancellationToken);
        await operationLock.AcquireAsync(loan.EmployeeId, cancellationToken);

        loan.Cancel(request.Request.Reason ?? "Cancelled", Actor, Now);
        Save(loan);
        await EmployeeLoanInstallmentLifecycle.CancelUnpaidInstallmentsAsync(installments, loan.Id, cancellationToken);
        return loan.Id;
    }
}

public sealed class DisburseEmployeeLoanCommandHandler(
    IRepository<EmployeeLoan, Guid> loans,
    IRepository<EmployeeLoanInstallment, Guid> installments,
    IHREmployeeLoanAccountingPort accounting,
    IEmployeeHrOperationLock operationLock,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<DisburseEmployeeLoanCommand, Guid>
{
    public async Task<Guid> Handle(
        DisburseEmployeeLoanCommand request,
        CancellationToken cancellationToken)
    {
        var loan = await loans.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeLoan), request.Id);

        if (string.IsNullOrWhiteSpace(request.Request.RowVersion))
        {
            throw new ConflictException("row_version_required", "Row version is required.");
        }

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            loan.RowVersion,
            "employee loan");

        if (loan.Status != EmployeeLoanStatus.Approved)
        {
            throw new ConflictException(
                "loan_not_approved",
                "Only approved loans can be disbursed.");
        }

        await operationLock.AcquireAsync(loan.EmployeeId, cancellationToken);

        var installmentRows = await installments.ListAsync(
            new Specification<EmployeeLoanInstallment>()
                .Where(x => x.EmployeeLoanId == loan.Id)
                .Tracking(),
            cancellationToken);

        if (installmentRows.Count != loan.InstallmentCount ||
            installmentRows.Any(x => x.Status != LoanInstallmentStatus.Pending))
        {
            throw new ConflictException(
                "loan_installments_invalid",
                "Loan installment schedule is incomplete or no longer pending.");
        }

        var voucherId = await accounting.DisburseAsync(
            new LoanSettlementRequest(
                request.Request.VoucherDate,
                loan.EmployeeId,
                loan.CurrencyId,
                loan.PrincipalAmount,
                request.Request.PaymentMethod,
                request.Request.CashAccountId,
                request.Request.BankAccountId,
                request.Request.SettlementAccountId,
                request.Request.ExchangeRate,
                request.Request.ExchangeRateType,
                request.Request.ReferenceNumber,
                "EmployeeLoan",
                loan.Id,
                request.Request.Description ?? $"Disbursement of employee loan {loan.LoanCode}"),
            cancellationToken);

        foreach (var installment in installmentRows)
        {
            installment.Schedule();
            installments.Update(installment);
        }

        loan.MarkDisbursed(
            voucherId,
            HrOperationsHelpers.Actor(currentUser),
            timeProvider.GetUtcNow());

        loans.Update(loan);
        return voucherId;
    }
}

public sealed class PayLoanInstallmentExternallyCommandHandler(
    IRepository<EmployeeLoanInstallment, Guid> installments,
    IRepository<EmployeeLoan, Guid> loans,
    IHREmployeeLoanAccountingPort accounting,
    IEmployeeHrOperationLock operationLock,
    TimeProvider timeProvider)
    : IRequestHandler<PayLoanInstallmentExternallyCommand, Guid>
{
    public async Task<Guid> Handle(
        PayLoanInstallmentExternallyCommand request,
        CancellationToken cancellationToken)
    {
        var installment = await installments.GetForUpdateAsync(
                request.InstallmentId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeLoanInstallment), request.InstallmentId);

        if (string.IsNullOrWhiteSpace(request.Request.RowVersion))
        {
            throw new ConflictException("row_version_required", "Row version is required.");
        }

        HrOperationsHelpers.EnsureRowVersion(
            request.Request.RowVersion,
            installment.RowVersion,
            "loan installment");

        if (installment.Status != LoanInstallmentStatus.Scheduled)
        {
            throw new ConflictException(
                "loan_installment_not_scheduled",
                "Only scheduled installments can be paid externally.");
        }

        var loan = await loans.GetForUpdateAsync(installment.EmployeeLoanId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeLoan), installment.EmployeeLoanId);

        if (loan.Status != EmployeeLoanStatus.Active)
        {
            throw new ConflictException("loan_not_active", "Loan is not active.");
        }

        await operationLock.AcquireAsync(loan.EmployeeId, cancellationToken);

        var receiptId = await accounting.ReceiveRepaymentAsync(
            new LoanSettlementRequest(
                request.Request.PaymentDate,
                loan.EmployeeId,
                loan.CurrencyId,
                installment.Amount,
                request.Request.PaymentMethod,
                request.Request.CashAccountId,
                request.Request.BankAccountId,
                request.Request.SettlementAccountId,
                request.Request.ExchangeRate,
                request.Request.ExchangeRateType,
                request.Request.ReferenceNumber,
                "EmployeeLoanInstallment",
                installment.Id,
                request.Request.Description ??
                $"External repayment of {loan.LoanCode} installment {installment.InstallmentSequence}"),
            cancellationToken);

        installment.MarkPaidExternally(receiptId, timeProvider.GetUtcNow());
        installments.Update(installment);

        var remaining = await installments.ListAsync(
            new Specification<EmployeeLoanInstallment>().Where(x =>
                x.EmployeeLoanId == loan.Id &&
                x.Id != installment.Id),
            cancellationToken);

        if (remaining.All(x =>
                x.Status is LoanInstallmentStatus.Deducted or
                    LoanInstallmentStatus.PaidExternally or
                    LoanInstallmentStatus.Cancelled))
        {
            loan.MarkCompleted();
        }

        loans.Update(loan);
        return receiptId;
    }
}

internal static class EmployeeLoanInstallmentLifecycle
{
    public static async Task CancelUnpaidInstallmentsAsync(
        IRepository<EmployeeLoanInstallment, Guid> installments,
        Guid loanId,
        CancellationToken cancellationToken)
    {
        var rows = await installments.ListAsync(
            new Specification<EmployeeLoanInstallment>()
                .Where(x => x.EmployeeLoanId == loanId)
                .Tracking(),
            cancellationToken);

        foreach (var installment in rows.Where(x =>
                     x.Status is LoanInstallmentStatus.Pending or LoanInstallmentStatus.Scheduled))
        {
            installment.Cancel();
            installments.Update(installment);
        }
    }
}
