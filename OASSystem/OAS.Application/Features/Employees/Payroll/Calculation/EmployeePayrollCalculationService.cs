using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Domain.Features.Employees;
using OAS.Domain.Features.Employees.Adjustments;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.Leave;
using OAS.Domain.Features.Employees.Loans;
using OAS.Domain.Features.Employees.Overtime;
using OAS.Domain.Features.Employees.Payroll;
using OAS.Domain.Features.Employees.Time;

namespace OAS.Application.Features.Employees.Payroll.Calculation;

public sealed class EmployeePayrollCalculationService(
    IReadRepository<Employee, Guid> employees,
    IReadRepository<JobTitle, Guid> jobTitles,
    IReadRepository<Department, Guid> departments,
    IReadRepository<EmployeeContract, Guid> contracts,
    IEmployeeSalaryStructureRepository salaryStructures,
    IReadRepository<SalaryComponent, Guid> salaryComponents,
    IReadRepository<AttendanceRecord, Guid> attendance,
    IReadRepository<LeaveRequest, Guid> leaveRequests,
    IReadRepository<OvertimeRecord, Guid> overtime,
    IReadRepository<EmployeeLoan, Guid> loans,
    IReadRepository<EmployeeLoanInstallment, Guid> installments,
    IReadRepository<EmployeeAdjustment, Guid> adjustments,
    IApprovedCommissionSource commissions)
    : IEmployeePayrollCalculationService
{
    public async Task<PayrollCalculationBatch> PrevalidateAndCalculateAsync(
        PayrollRun run,
        PayrollPeriod period,
        PayrollPolicy policy,
        bool calculate,
        CancellationToken cancellationToken = default)
    {
        var result = new List<PayrollCalculatedEmployee>();
        var issues = new List<PayrollCalculationIssue>();

        var allEmployees = await employees.ListAsync(cancellationToken: cancellationToken);
        var allContracts = await contracts.ListAsync(cancellationToken: cancellationToken);
        var allJobTitles = (await jobTitles.ListAsync(cancellationToken: cancellationToken)).ToDictionary(x => x.Id);
        var allDepartments = (await departments.ListAsync(cancellationToken: cancellationToken)).ToDictionary(x => x.Id);
        var allAttendance = await attendance.ListAsync(new Specification<AttendanceRecord>().Where(x => x.AttendanceDate >= period.StartDate && x.AttendanceDate <= period.EndDate), cancellationToken);
        var allLeaves = await leaveRequests.ListAsync(new Specification<LeaveRequest>().Where(x => x.StartDate <= period.EndDate && x.EndDate >= period.StartDate), cancellationToken);
        var allOvertime = await overtime.ListAsync(new Specification<OvertimeRecord>().Where(x => x.WorkDate >= period.StartDate && x.WorkDate <= period.EndDate), cancellationToken);
        var allLoans = await loans.ListAsync(cancellationToken: cancellationToken);
        var allInstallments = await installments.ListAsync(new Specification<EmployeeLoanInstallment>().Where(x => x.DueDate >= period.StartDate && x.DueDate <= period.EndDate), cancellationToken);
        var allAdjustments = await adjustments.ListAsync(new Specification<EmployeeAdjustment>().Where(x => x.EffectiveDate >= period.StartDate && x.EffectiveDate <= period.EndDate), cancellationToken);

        var policyComponents = await LoadPolicyComponentsAsync(policy, cancellationToken);

        foreach (var employee in allEmployees.OrderBy(x => x.EmployeeCode))
        {
            if (run.RunType == PayrollRunType.Regular && !employee.IsActive)
                continue;

            var employeeContracts = allContracts.Where(x => x.EmployeeId == employee.Id).ToArray();
            var eligibleContracts = employeeContracts.Where(x => IsContractEligibleForPeriod(x, period)).OrderBy(x => x.StartDate).ToArray();
            if (eligibleContracts.Length == 0) continue;

            if (run.RunType == PayrollRunType.Final)
            {
                eligibleContracts = eligibleContracts.Where(x => x.Status == EmploymentContractStatus.Terminated && x.TerminationEffectiveDate >= period.StartDate && x.TerminationEffectiveDate <= period.EndDate).ToArray();
                if (eligibleContracts.Length == 0) continue;
            }
            else if (eligibleContracts.Any(x => x.Status == EmploymentContractStatus.Terminated && x.TerminationEffectiveDate >= period.StartDate && x.TerminationEffectiveDate <= period.EndDate))
            {
                issues.Add(Issue(employee, "final_payroll_required", "Employee has a termination effective date in this period and is excluded from the regular run; process the employee in a Final payroll run.", PayrollIssueSeverity.Warning));
                continue;
            }

            var contractCurrencies = eligibleContracts.Select(x => x.CurrencyId).Distinct().ToArray();
            if (contractCurrencies.Length > 1)
            {
                if (!contractCurrencies.Contains(run.CurrencyId))
                    continue;

                issues.Add(Issue(
                    employee,
                    "payroll_contract_currency_change_unsupported",
                    "Employee contracts change currency inside the same payroll period. Split-period multi-currency payroll is not supported in V1.",
                    PayrollIssueSeverity.Blocking));
                continue;
            }

            var primaryContract = eligibleContracts.Last();
            if (primaryContract.CurrencyId != run.CurrencyId)
                continue;

            var coverageFrom = Max(period.StartDate, employee.HireDate ?? period.StartDate, eligibleContracts.Min(x => x.StartDate));
            var coverageTo = Min(period.EndDate, eligibleContracts.Max(ContractEffectiveEnd));
            if (coverageTo < coverageFrom) continue;

            var employeeIssues = new List<PayrollCalculationIssue>();
            var structures = (await salaryStructures.ListForEmployeeAsync(employee.Id, cancellationToken))
                .Where(x => x.CurrencyId == run.CurrencyId && x.Status is SalaryStructureStatus.Active or SalaryStructureStatus.Superseded && x.EffectiveFrom <= coverageTo && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= coverageFrom))
                .OrderBy(x => x.EffectiveFrom)
                .ToArray();

            if (structures.Length == 0)
            {
                employeeIssues.Add(Issue(employee, "salary_structure_missing", "No effective salary structure exists for the employee in this payroll coverage.", PayrollIssueSeverity.Blocking));
                issues.AddRange(employeeIssues);
                continue;
            }

            var segments = BuildSegments(employee, eligibleContracts, structures, period, coverageFrom, coverageTo, allAttendance, policy, employeeIssues);
            if (employeeIssues.Any(x => x.Severity == (byte)PayrollIssueSeverity.Blocking))
            {
                issues.AddRange(employeeIssues);
                continue;
            }

            if (policy.ProrationMethod == PayrollProrationMethod.None && segments.Count > 1)
            {
                employeeIssues.Add(Issue(
                    employee,
                    "payroll_proration_required_for_mid_period_change",
                    "The employee has more than one contract or salary structure segment in the period. A proration method is required to calculate recurring monthly components safely.",
                    PayrollIssueSeverity.Blocking));
            }

            var employeeAttendance = allAttendance.Where(x => x.EmployeeId == employee.Id && x.AttendanceDate >= coverageFrom && x.AttendanceDate <= coverageTo).OrderBy(x => x.AttendanceDate).ToArray();
            var scheduledAttendanceCount = employeeAttendance.Count(x => x.ScheduledMinutes > 0);
            var salaryStructureRequiresAttendance = structures
                .SelectMany(x => x.Lines)
                .Any(x => x.CalculationMethodSnapshot is SalaryCalculationMethod.Daily or SalaryCalculationMethod.Hourly);

            if (policy.RequireApprovedAttendance && employeeAttendance.Length == 0)
                employeeIssues.Add(Issue(employee, "attendance_missing", "No attendance records exist for this payroll coverage.", PayrollIssueSeverity.Blocking));

            if (scheduledAttendanceCount == 0 && (policy.ProrationMethod == PayrollProrationMethod.ScheduledWorkingDays || salaryStructureRequiresAttendance))
                employeeIssues.Add(Issue(employee, "attendance_required_for_payroll_calculation", "Attendance with scheduled work minutes is required by the payroll proration or salary calculation method.", PayrollIssueSeverity.Blocking));

            var unusableAttendance = employeeAttendance.Where(x => x.AttendanceStatus == AttendanceDayStatus.Incomplete || x.ApprovalStatus == AttendanceApprovalStatus.Rejected).ToArray();
            if (unusableAttendance.Length > 0)
                employeeIssues.Add(Issue(employee, "attendance_not_usable", $"{unusableAttendance.Length} incomplete or rejected attendance record(s) cannot be used by payroll.", PayrollIssueSeverity.Blocking));
            if (policy.RequireApprovedAttendance)
            {
                var invalidAttendance = employeeAttendance.Where(x => x.ScheduledMinutes > 0 && x.AttendanceStatus is not (AttendanceDayStatus.Weekend or AttendanceDayStatus.Holiday or AttendanceDayStatus.Unscheduled) && x.ApprovalStatus != AttendanceApprovalStatus.Approved).ToArray();
                if (invalidAttendance.Length > 0)
                    employeeIssues.Add(Issue(employee, "attendance_not_approved", $"{invalidAttendance.Length} attendance record(s) require approval before payroll calculation.", PayrollIssueSeverity.Blocking));
            }
            if (employeeAttendance.Any(x => x.EmployeePayrollId.HasValue))
                employeeIssues.Add(Issue(employee, "attendance_already_used", "Attendance in this period is already locked to a posted payroll.", PayrollIssueSeverity.Blocking));

            var lines = BuildRecurringLines(segments, structures, employeeAttendance);
            var configuredBasic = segments.Last().BasicRate;
            var calculatedBasic = lines.Where(x => x.SourceType == (byte)PayrollLineSourceType.SalaryStructure && x.ComponentType == (byte)SalaryComponentType.Earning && IsBasicLine(x, structures)).Sum(x => x.Amount);

            var attendanceLines = BuildAttendanceLines(employee, coverageFrom, coverageTo, employeeAttendance, allLeaves, segments, policy, policyComponents, employeeIssues);
            lines.AddRange(attendanceLines);

            var employeeOvertime = allOvertime.Where(x => x.EmployeeId == employee.Id && x.WorkDate >= coverageFrom && x.WorkDate <= coverageTo && x.Status == OvertimeStatus.Approved && !x.EmployeePayrollId.HasValue).ToArray();
            if (employeeOvertime.Length > 0 && policyComponents.Overtime is null)
                employeeIssues.Add(Issue(employee, "payroll_overtime_component_missing", "Payroll policy is missing the salary component required for approved overtime.", PayrollIssueSeverity.Blocking));
            else
                lines.AddRange(BuildOvertimeLines(employeeOvertime, segments, policy, employeeAttendance, policyComponents.Overtime));

            var employeeAdjustments = allAdjustments.Where(x => x.EmployeeId == employee.Id && x.EffectiveDate >= coverageFrom && x.EffectiveDate <= coverageTo && x.Status == EmployeeAdjustmentStatus.Approved && !x.EmployeePayrollId.HasValue && x.CurrencyId == run.CurrencyId).ToArray();
            lines.AddRange(employeeAdjustments.Select(x => new PayrollCalculatedLine(null, x.SalaryComponentId, x.ComponentCodeSnapshot, x.ComponentNameSnapshot, (byte)x.ComponentTypeSnapshot, (byte)PayrollLineSourceType.Adjustment, "HR", "EmployeeAdjustment", x.Id, x.EffectiveDate, null, null, x.Amount, x.DebitPostingRoleSnapshot, x.CreditPostingRoleSnapshot, x.Reason)));

            var activeLoanIds = allLoans.Where(x => x.EmployeeId == employee.Id && x.Status == EmployeeLoanStatus.Active && x.CurrencyId == run.CurrencyId && x.RepaymentMode is LoanRepaymentMode.Payroll or LoanRepaymentMode.Mixed).Select(x => x.Id).ToHashSet();
            var employeeInstallments = allInstallments.Where(x => activeLoanIds.Contains(x.EmployeeLoanId) && x.Status == LoanInstallmentStatus.Scheduled && !x.EmployeePayrollId.HasValue).ToArray();
            lines.AddRange(employeeInstallments.Select(x => new PayrollCalculatedLine(null, null, null, "قسط سلفة", (byte)SalaryComponentType.Deduction, (byte)PayrollLineSourceType.LoanInstallment, "HR", "EmployeeLoanInstallment", x.Id, x.DueDate, 1m, x.Amount, x.Amount, HRPostingRoles.SalariesPayable, HRPostingRoles.EmployeeLoansReceivable, "خصم قسط سلفة من الراتب")));

            var employeeCommissions = await commissions.GetApprovedAsync(employee.Id, coverageFrom, coverageTo, run.CurrencyId, cancellationToken);
            lines.AddRange(employeeCommissions.Select(x => new PayrollCalculatedLine(null, null, null, "عمولة", (byte)SalaryComponentType.Earning, (byte)PayrollLineSourceType.Commission, "Commissions", "CommissionSettlement", x.SourceDocumentId, x.Date, 1m, x.Amount, x.Amount, x.DebitPostingRole ?? HRPostingRoles.CommissionExpense, x.CreditPostingRole ?? HRPostingRoles.SalariesPayable, x.Description)));

            ValidateSourceUniqueness(lines, employee, employeeIssues);
            ValidatePostingRoles(lines, employee, employeeIssues);

            var gross = Round(lines.Where(x => x.ComponentType == (byte)SalaryComponentType.Earning).Sum(x => x.Amount), run.CurrencyDecimalPlacesSnapshot);
            var deductions = Round(lines.Where(x => x.ComponentType == (byte)SalaryComponentType.Deduction).Sum(x => x.Amount), run.CurrencyDecimalPlacesSnapshot);
            var employer = Round(lines.Where(x => x.ComponentType == (byte)SalaryComponentType.EmployerContribution).Sum(x => x.Amount), run.CurrencyDecimalPlacesSnapshot);
            var net = gross - deductions;
            if (net < 0)
                employeeIssues.Add(Issue(employee, "employee_payroll_negative_net", "Payroll deductions exceed gross earnings.", PayrollIssueSeverity.Blocking));

            issues.AddRange(employeeIssues);
            if (!calculate || employeeIssues.Any(x => x.Severity == (byte)PayrollIssueSeverity.Blocking)) continue;

            var segmentContractIds = segments
                .Where(x => x.ContractId.HasValue)
                .Select(x => x.ContractId!.Value)
                .Distinct()
                .ToArray();
            var payrollContractId = segmentContractIds.Length == 1 ? segmentContractIds[0] : (Guid?)null;
            var payrollContractCode = segmentContractIds.Length == 1
                ? segments.First(x => x.ContractId == payrollContractId).ContractCode
                : null;

            result.Add(new PayrollCalculatedEmployee(
                employee.Id, coverageFrom, coverageTo, employee.EmployeeCode, employee.DisplayName,
                allJobTitles.TryGetValue(employee.JobTitleId, out var jt) ? jt.Name : null,
                employee.DepartmentId.HasValue && allDepartments.TryGetValue(employee.DepartmentId.Value, out var dep) ? dep.NameAr : null,
                payrollContractId, payrollContractCode,
                run.CurrencyId, run.CurrencyCodeSnapshot, run.CurrencySymbolSnapshot, run.CurrencyDecimalPlacesSnapshot,
                configuredBasic, calculatedBasic, gross, deductions, employer, net, segments, lines));
        }

        return new PayrollCalculationBatch(result, issues);
    }

    private async Task<PolicyComponents> LoadPolicyComponentsAsync(PayrollPolicy policy, CancellationToken ct)
    {
        async Task<SalaryComponent?> Get(Guid? id)
        {
            if (!id.HasValue) return null;
            var x = await salaryComponents.GetByIdAsync(id.Value, ct);
            return x is { IsActive: true } ? x : null;
        }
        return new PolicyComponents(await Get(policy.AbsenceDeductionComponentId), await Get(policy.LateDeductionComponentId), await Get(policy.EarlyLeaveDeductionComponentId), await Get(policy.UnpaidLeaveComponentId), await Get(policy.OvertimeComponentId));
    }

    private static List<PayrollCalculatedSegment> BuildSegments(
        Employee employee,
        EmployeeContract[] eligibleContracts,
        EmployeeSalaryStructure[] structures,
        PayrollPeriod period,
        DateOnly coverageFrom,
        DateOnly coverageTo,
        IReadOnlyList<AttendanceRecord> allAttendance,
        PayrollPolicy policy,
        List<PayrollCalculationIssue> issues)
    {
        var dayAssignments = new List<(DateOnly Date, EmployeeSalaryStructure Structure, EmployeeContract Contract)>();

        for (var date = coverageFrom; date <= coverageTo; date = date.AddDays(1))
        {
            var matchingStructures = structures
                .Where(x => x.EffectiveFrom <= date && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= date))
                .ToArray();

            if (matchingStructures.Length != 1)
            {
                issues.Add(Issue(
                    employee,
                    matchingStructures.Length == 0 ? "salary_structure_gap" : "salary_structure_overlap",
                    matchingStructures.Length == 0
                        ? $"No salary structure covers {date:yyyy-MM-dd}."
                        : $"More than one salary structure covers {date:yyyy-MM-dd}.",
                    PayrollIssueSeverity.Blocking));
                continue;
            }

            var structure = matchingStructures[0];
            var matchingContracts = eligibleContracts
                .Where(x => x.StartDate <= date && ContractEffectiveEnd(x) >= date)
                .Where(x => !structure.ContractId.HasValue || x.Id == structure.ContractId.Value)
                .ToArray();

            if (matchingContracts.Length != 1)
            {
                issues.Add(Issue(
                    employee,
                    matchingContracts.Length == 0 ? "employment_contract_gap" : "employment_contract_overlap",
                    matchingContracts.Length == 0
                        ? $"No employment contract compatible with salary structure '{structure.StructureCode}' covers {date:yyyy-MM-dd}."
                        : $"More than one employment contract covers {date:yyyy-MM-dd}.",
                    PayrollIssueSeverity.Blocking));
                continue;
            }

            dayAssignments.Add((date, structure, matchingContracts[0]));
        }

        if (issues.Any(x => x.Severity == (byte)PayrollIssueSeverity.Blocking))
            return [];

        var result = new List<PayrollCalculatedSegment>();
        var start = dayAssignments[0].Date;
        var currentStructure = dayAssignments[0].Structure;
        var currentContract = dayAssignments[0].Contract;

        for (var i = 1; i <= dayAssignments.Count; i++)
        {
            var boundary = i == dayAssignments.Count
                || dayAssignments[i].Structure.Id != currentStructure.Id
                || dayAssignments[i].Contract.Id != currentContract.Id;

            if (!boundary)
                continue;

            var end = dayAssignments[i - 1].Date;
            var basic = currentStructure.Lines.SingleOrDefault(x => x.IsBasicSalarySnapshot);
            if (basic is null)
            {
                issues.Add(Issue(
                    employee,
                    "basic_salary_missing",
                    $"Salary structure {currentStructure.StructureCode} has no basic salary line.",
                    PayrollIssueSeverity.Blocking));
                return [];
            }

            var factor = CalculateProrationFactor(
                policy.ProrationMethod,
                period,
                start,
                end,
                employee.Id,
                allAttendance);

            result.Add(new PayrollCalculatedSegment(
                currentContract.Id,
                currentContract.ContractCode,
                currentStructure.Id,
                currentStructure.StructureCode,
                start,
                end,
                basic.Amount,
                factor));

            if (i < dayAssignments.Count)
            {
                start = dayAssignments[i].Date;
                currentStructure = dayAssignments[i].Structure;
                currentContract = dayAssignments[i].Contract;
            }
        }

        return result;
    }

    private static decimal CalculateProrationFactor(PayrollProrationMethod method, PayrollPeriod period, DateOnly from, DateOnly to, Guid employeeId, IReadOnlyList<AttendanceRecord> attendanceRows)
    {
        if (method == PayrollProrationMethod.None) return 1m;
        if (from == period.StartDate && to == period.EndDate) return 1m;
        if (method == PayrollProrationMethod.ScheduledWorkingDays)
        {
            var periodDays = attendanceRows.Count(x => x.EmployeeId == employeeId && x.AttendanceDate >= period.StartDate && x.AttendanceDate <= period.EndDate && x.ScheduledMinutes > 0 && x.AttendanceStatus is not (AttendanceDayStatus.Weekend or AttendanceDayStatus.Holiday or AttendanceDayStatus.Unscheduled));
            var segmentDays = attendanceRows.Count(x => x.EmployeeId == employeeId && x.AttendanceDate >= from && x.AttendanceDate <= to && x.ScheduledMinutes > 0 && x.AttendanceStatus is not (AttendanceDayStatus.Weekend or AttendanceDayStatus.Holiday or AttendanceDayStatus.Unscheduled));
            return periodDays <= 0 ? 0m : Math.Min(1m, (decimal)segmentDays / periodDays);
        }
        var denominator = period.EndDate.DayNumber - period.StartDate.DayNumber + 1;
        var numerator = to.DayNumber - from.DayNumber + 1;
        return denominator <= 0 ? 0m : Math.Min(1m, (decimal)numerator / denominator);
    }

    private static List<PayrollCalculatedLine> BuildRecurringLines(IReadOnlyList<PayrollCalculatedSegment> segments, EmployeeSalaryStructure[] structures, IReadOnlyList<AttendanceRecord> attendanceRows)
    {
        var lines = new List<PayrollCalculatedLine>();
        foreach (var segment in segments)
        {
            var structure = structures.Single(x => x.Id == segment.SalaryStructureId);
            var scheduledMinutes = attendanceRows.Where(x => x.AttendanceDate >= segment.From && x.AttendanceDate <= segment.To && x.ScheduledMinutes > 0).Sum(x => x.ScheduledMinutes);
            var eligibleDays = attendanceRows.Count(x => x.AttendanceDate >= segment.From && x.AttendanceDate <= segment.To && x.ScheduledMinutes > 0 && x.AttendanceStatus is not (AttendanceDayStatus.Weekend or AttendanceDayStatus.Holiday or AttendanceDayStatus.Unscheduled));
            foreach (var source in structure.Lines.Where(x => x.CalculationMethodSnapshot is not (SalaryCalculationMethod.Manual or SalaryCalculationMethod.ExternalSource)))
            {
                decimal amount = source.CalculationMethodSnapshot switch
                {
                    SalaryCalculationMethod.FixedAmount => source.Amount * segment.ProrationFactor,
                    SalaryCalculationMethod.PercentageOfBasic => segment.BasicRate * (source.Percentage ?? 0m) / 100m * segment.ProrationFactor,
                    SalaryCalculationMethod.Daily => source.Amount * eligibleDays,
                    SalaryCalculationMethod.Hourly => source.Amount * (scheduledMinutes / 60m),
                    _ => 0m
                };
                if (amount <= 0m) continue;
                lines.Add(new PayrollCalculatedLine(structure.Id, source.SalaryComponentId, source.ComponentCodeSnapshot, source.ComponentNameSnapshot, (byte)source.ComponentTypeSnapshot, (byte)PayrollLineSourceType.SalaryStructure, "HR", "SalaryStructure", structure.Id, segment.From, null, null, amount, source.DebitPostingRoleSnapshot, source.CreditPostingRoleSnapshot, $"{structure.StructureCode}: {segment.From:yyyy-MM-dd} - {segment.To:yyyy-MM-dd}"));
            }
        }
        return lines;
    }

    private static List<PayrollCalculatedLine> BuildAttendanceLines(Employee employee, DateOnly from, DateOnly to, IReadOnlyList<AttendanceRecord> employeeAttendance, IReadOnlyList<LeaveRequest> leaves, IReadOnlyList<PayrollCalculatedSegment> segments, PayrollPolicy policy, PolicyComponents components, List<PayrollCalculationIssue> issues)
    {
        var result = new List<PayrollCalculatedLine>();
        foreach (var row in employeeAttendance.Where(x => x.AttendanceDate >= from && x.AttendanceDate <= to))
        {
            var dailyRate = DailyRateForDate(row.AttendanceDate, segments, policy, employeeAttendance);
            var hourlyRate = HourlyRateForDate(row.AttendanceDate, segments, policy, employeeAttendance);
            if (row.AttendanceStatus == AttendanceDayStatus.Absent)
            {
                AddPolicyDeduction(result, components.Absence, row.Id, row.AttendanceDate, 1m, dailyRate, dailyRate, "خصم غياب", issues, employee, "absence_component_required");
            }
            if (row.LateMinutes > 0)
            {
                var amount = hourlyRate * row.LateMinutes / 60m;
                AddPolicyDeduction(result, components.Late, row.Id, row.AttendanceDate, row.LateMinutes / 60m, hourlyRate, amount, "خصم تأخير", issues, employee, "late_component_required");
            }
            if (row.EarlyLeaveMinutes > 0)
            {
                var amount = hourlyRate * row.EarlyLeaveMinutes / 60m;
                AddPolicyDeduction(result, components.Early, row.Id, row.AttendanceDate, row.EarlyLeaveMinutes / 60m, hourlyRate, amount, "خصم انصراف مبكر", issues, employee, "early_leave_component_required");
            }
            if (row.AttendanceStatus == AttendanceDayStatus.Leave && row.SourceLeaveRequestId.HasValue)
            {
                var leave = leaves.FirstOrDefault(x => x.Id == row.SourceLeaveRequestId.Value);
                if (leave is { IsPaidSnapshot: false, Status: LeaveRequestStatus.Approved })
                    AddPolicyDeduction(result, components.UnpaidLeave, leave.Id, row.AttendanceDate, 1m, dailyRate, dailyRate, "خصم إجازة غير مدفوعة", issues, employee, "unpaid_leave_component_required", "LeaveRequest");
            }
        }
        return result;
    }

    private static void AddPolicyDeduction(List<PayrollCalculatedLine> lines, SalaryComponent? component, Guid sourceId, DateOnly date, decimal quantity, decimal rate, decimal amount, string description, List<PayrollCalculationIssue> issues, Employee employee, string missingCode, string sourceType = "AttendanceRecord")
    {
        if (amount <= 0) return;
        if (component is null)
        {
            issues.Add(Issue(employee, missingCode, $"Payroll policy is missing the salary component required for {description}.", PayrollIssueSeverity.Blocking));
            return;
        }
        lines.Add(new PayrollCalculatedLine(null, component.Id, component.ComponentCode, component.NameAr, (byte)SalaryComponentType.Deduction, (byte)(sourceType == "LeaveRequest" ? PayrollLineSourceType.Leave : PayrollLineSourceType.Attendance), "HR", sourceType, sourceId, date, quantity, rate, amount, component.DebitPostingRole, component.CreditPostingRole, description));
    }

    private static IEnumerable<PayrollCalculatedLine> BuildOvertimeLines(
        IEnumerable<OvertimeRecord> items,
        IReadOnlyList<PayrollCalculatedSegment> segments,
        PayrollPolicy policy,
        IReadOnlyList<AttendanceRecord> attendanceRows,
        SalaryComponent? component)
    {
        foreach (var item in items)
        {
            if (component is null)
                continue;

            var hourlyRate = HourlyRateForDate(item.WorkDate, segments, policy, attendanceRows);
            var quantity = item.ApprovedMinutes / 60m;
            var effectiveRate = hourlyRate * item.RateMultiplier;
            var amount = effectiveRate * quantity;
            if (amount <= 0)
                continue;

            yield return new PayrollCalculatedLine(
                null,
                component.Id,
                component.ComponentCode,
                component.NameAr,
                (byte)SalaryComponentType.Earning,
                (byte)PayrollLineSourceType.Overtime,
                "HR",
                "OvertimeRecord",
                item.Id,
                item.WorkDate,
                quantity,
                effectiveRate,
                amount,
                component.DebitPostingRole ?? HRPostingRoles.OvertimeExpense,
                component.CreditPostingRole ?? HRPostingRoles.SalariesPayable,
                "بدل عمل إضافي");
        }
    }

    private static decimal DailyRateForDate(DateOnly date, IReadOnlyList<PayrollCalculatedSegment> segments, PayrollPolicy policy, IReadOnlyList<AttendanceRecord> attendanceRows)
    {
        var basic = BasicRateForDate(date, segments);
        return policy.DailyRateMethod switch
        {
            PayrollDailyRateMethod.Fixed30Days => basic / 30m,
            PayrollDailyRateMethod.CalendarDaysInPeriod => basic / Math.Max(1, DateTime.DaysInMonth(date.Year, date.Month)),
            PayrollDailyRateMethod.ScheduledWorkingDaysInPeriod => basic / Math.Max(1, attendanceRows.Count(x => x.ScheduledMinutes > 0 && x.AttendanceStatus is not (AttendanceDayStatus.Weekend or AttendanceDayStatus.Holiday or AttendanceDayStatus.Unscheduled))),
            _ => basic / 30m
        };
    }

    private static decimal HourlyRateForDate(DateOnly date, IReadOnlyList<PayrollCalculatedSegment> segments, PayrollPolicy policy, IReadOnlyList<AttendanceRecord> attendanceRows)
    {
        var daily = DailyRateForDate(date, segments, policy, attendanceRows);
        var row = attendanceRows.FirstOrDefault(x => x.AttendanceDate == date);
        var scheduledHours = row is { ScheduledMinutes: > 0 } ? row.ScheduledMinutes / 60m : 8m;
        return policy.HourlyRateMethod switch
        {
            PayrollHourlyRateMethod.BasicByScheduledMinutes => BasicRateForDate(date, segments) / Math.Max(1m, attendanceRows.Sum(x => x.ScheduledMinutes) / 60m),
            _ => daily / Math.Max(1m, scheduledHours)
        };
    }

    private static decimal BasicRateForDate(DateOnly date, IReadOnlyList<PayrollCalculatedSegment> segments) => segments.First(x => x.From <= date && x.To >= date).BasicRate;

    private static bool IsBasicLine(PayrollCalculatedLine line, EmployeeSalaryStructure[] structures) => line.SalaryComponentId.HasValue && structures.SelectMany(x => x.Lines).Any(x => x.SalaryComponentId == line.SalaryComponentId && x.IsBasicSalarySnapshot);

    private static bool IsContractEligibleForPeriod(EmployeeContract contract, PayrollPeriod period)
    {
        if (contract.Status is EmploymentContractStatus.Draft or EmploymentContractStatus.Cancelled) return false;
        var end = ContractEffectiveEnd(contract);
        return contract.StartDate <= period.EndDate && end >= period.StartDate;
    }
    private static DateOnly ContractEffectiveEnd(EmployeeContract x) => x.Status == EmploymentContractStatus.Terminated && x.TerminationEffectiveDate.HasValue ? x.TerminationEffectiveDate.Value : x.EndDate ?? DateOnly.MaxValue;

    private static void ValidateSourceUniqueness(IReadOnlyList<PayrollCalculatedLine> lines, Employee employee, List<PayrollCalculationIssue> issues)
    {
        var duplicates = lines.Where(x => x.SourceDocumentId.HasValue && x.SourceType is (byte)PayrollLineSourceType.Overtime or (byte)PayrollLineSourceType.LoanInstallment or (byte)PayrollLineSourceType.Commission or (byte)PayrollLineSourceType.Adjustment).GroupBy(x => new { x.SourceType, x.SourceDocumentId }).Where(x => x.Count() > 1).ToArray();
        if (duplicates.Length > 0) issues.Add(Issue(employee, "payroll_source_duplicate", "A payroll source is included more than once.", PayrollIssueSeverity.Blocking));
    }

    private static void ValidatePostingRoles(IReadOnlyList<PayrollCalculatedLine> lines, Employee employee, List<PayrollCalculationIssue> issues)
    {
        if (lines.Any(x => x.Amount > 0 && (string.IsNullOrWhiteSpace(x.DebitPostingRole) || string.IsNullOrWhiteSpace(x.CreditPostingRole))))
            issues.Add(Issue(employee, "payroll_posting_role_missing", "One or more payroll lines do not have complete accounting posting roles.", PayrollIssueSeverity.Blocking));
    }

    private static PayrollCalculationIssue Issue(Employee e, string code, string message, PayrollIssueSeverity severity) => new(e.Id, e.EmployeeCode, e.DisplayName, code, message, (byte)severity);
    private static DateOnly Max(params DateOnly[] values) => values.Max();
    private static DateOnly Min(params DateOnly[] values) => values.Min();
    private static decimal Round(decimal value, byte decimals) => Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    private sealed record PolicyComponents(SalaryComponent? Absence, SalaryComponent? Late, SalaryComponent? Early, SalaryComponent? UnpaidLeave, SalaryComponent? Overtime);
}
