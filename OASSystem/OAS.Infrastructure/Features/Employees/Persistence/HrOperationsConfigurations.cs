using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Adjustments;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Leave;
using OAS.Domain.Features.Employees.Loans;
using OAS.Domain.Features.Employees.Overtime;
using OAS.Domain.Features.Employees.Settings;
using OAS.Domain.Features.Employees.Time;

using OAS.Domain.Features.Employees.Payroll;
using OAS.Domain.Features.Employees.EndOfService;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class HrSettingsConfiguration : IEntityTypeConfiguration<HrSettings>
{
    public void Configure(EntityTypeBuilder<HrSettings> builder)
    {
        builder.ToTable("HrSettings", "hr", table =>
            table.HasCheckConstraint(
                "CK_HrSettings_LeaveYearStartMonth",
                "[LeaveYearStartMonth] BETWEEN 1 AND 12"));

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TimeZoneId).HasMaxLength(128);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}

public sealed class WorkShiftConfiguration : IEntityTypeConfiguration<WorkShift>
{
    public void Configure(EntityTypeBuilder<WorkShift> builder)
    {
        builder.ToTable("WorkShifts", "hr", table =>
        {
            table.HasCheckConstraint("CK_WorkShifts_Times", "[StartTime] <> [EndTime]");
            table.HasCheckConstraint(
                "CK_WorkShifts_Minutes",
                "[BreakMinutes] >= 0 AND [GraceLateMinutes] >= 0 AND [GraceEarlyLeaveMinutes] >= 0");
            table.HasCheckConstraint(
                "CK_WorkShifts_WorkingDaysMask",
                "[WorkingDaysMask] BETWEEN 1 AND 127");
        });

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ShiftCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.StartTime).HasColumnType("time");
        builder.Property(x => x.EndTime).HasColumnType("time");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.Ignore(x => x.CrossesMidnight);
        builder.Ignore(x => x.RawDurationMinutes);
        builder.Ignore(x => x.ScheduledMinutes);

        builder.HasIndex(x => x.ShiftCode)
            .IsUnique()
            .HasDatabaseName("UX_WorkShifts_ShiftCode");
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_WorkShifts_IsActive");
    }
}

public sealed class EmployeeShiftAssignmentConfiguration : IEntityTypeConfiguration<EmployeeShiftAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeShiftAssignment> builder)
    {
        builder.ToTable("EmployeeShiftAssignments", "hr", table =>
            table.HasCheckConstraint(
                "CK_EmployeeShiftAssignments_Dates",
                "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EffectiveFrom).HasColumnType("date");
        builder.Property(x => x.EffectiveTo).HasColumnType("date");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom, x.EffectiveTo })
            .HasDatabaseName("IX_ShiftAssignments_Employee_Dates");
        builder.HasIndex(x => x.WorkShiftId)
            .HasDatabaseName("IX_ShiftAssignments_ShiftId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeShiftAssignments_Employees");
        builder.HasOne<WorkShift>()
            .WithMany()
            .HasForeignKey(x => x.WorkShiftId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeShiftAssignments_WorkShifts");
    }
}

public sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays", "hr", table =>
            table.HasCheckConstraint("CK_Holidays_Dates", "[EndDate] >= [StartDate]"));

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.HolidayCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.StartDate).HasColumnType("date");
        builder.Property(x => x.EndDate).HasColumnType("date");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.HolidayCode)
            .IsUnique()
            .HasDatabaseName("UX_Holidays_HolidayCode");
        builder.HasIndex(x => new { x.StartDate, x.EndDate, x.IsActive })
            .HasDatabaseName("IX_Holidays_Dates");
    }
}

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords", "hr", table =>
        {
            table.HasCheckConstraint(
                "CK_Attendance_CheckTimes",
                "[CheckOutAtUtc] IS NULL OR [CheckInAtUtc] IS NULL OR [CheckOutAtUtc] >= [CheckInAtUtc]");
            table.HasCheckConstraint(
                "CK_Attendance_Minutes",
                "[ScheduledMinutes]>=0 AND [WorkedMinutes]>=0 AND [LateMinutes]>=0 AND [EarlyLeaveMinutes]>=0 AND [OvertimeMinutes]>=0 AND [BreakMinutesSnapshot]>=0 AND [GraceLateMinutesSnapshot]>=0 AND [GraceEarlyLeaveMinutesSnapshot]>=0");
        });

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AttendanceDate).HasColumnType("date");
        builder.Property(x => x.ShiftCodeSnapshot).HasMaxLength(32);
        builder.Property(x => x.ShiftNameSnapshot).HasMaxLength(150);
        builder.Property(x => x.TimeZoneIdSnapshot).HasMaxLength(128);
        builder.Property(x => x.Source).HasConversion<byte>();
        builder.Property(x => x.AttendanceStatus).HasConversion<byte>();
        builder.Property(x => x.ApprovalStatus).HasConversion<byte>();
        builder.Property(x => x.SubmittedBy).HasMaxLength(64);
        builder.Property(x => x.ApprovedBy).HasMaxLength(64);
        builder.Property(x => x.RejectedBy).HasMaxLength(64);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.LastCorrectedBy).HasMaxLength(64);
        builder.Property(x => x.LastCorrectionReason).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.EmployeeId, x.AttendanceDate })
            .IsUnique()
            .HasDatabaseName("UX_Attendance_Employee_Date");
        builder.HasIndex(x => x.AttendanceDate)
            .HasDatabaseName("IX_Attendance_Date");
        builder.HasIndex(x => x.AttendanceStatus)
            .HasDatabaseName("IX_Attendance_Status");
        builder.HasIndex(x => x.ApprovalStatus)
            .HasDatabaseName("IX_Attendance_ApprovalStatus");
        builder.HasIndex(x => x.WorkShiftId)
            .HasDatabaseName("IX_Attendance_WorkShiftId");

        builder.HasIndex(x => x.SourceLeaveRequestId)
            .HasDatabaseName("IX_Attendance_SourceLeaveRequestId");
        builder.HasIndex(x => x.SourceHolidayId)
            .HasDatabaseName("IX_Attendance_SourceHolidayId");
        builder.HasIndex(x => x.EmployeePayrollId)
            .HasDatabaseName("IX_Attendance_EmployeePayrollId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Attendance_Employees");
        builder.HasOne<WorkShift>()
            .WithMany()
            .HasForeignKey(x => x.WorkShiftId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Attendance_WorkShifts");
        builder.HasOne<LeaveRequest>()
            .WithMany()
            .HasForeignKey(x => x.SourceLeaveRequestId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Attendance_LeaveRequests");
        builder.HasOne<Holiday>()
            .WithMany()
            .HasForeignKey(x => x.SourceHolidayId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Attendance_Holidays");
        builder.HasOne<EmployeePayroll>()
            .WithMany()
            .HasForeignKey(x => x.EmployeePayrollId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Attendance_EmployeePayroll");
    }
}

public sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes", "hr", table =>
            table.HasCheckConstraint(
                "CK_LeaveTypes_Amounts",
                "[AnnualEntitlementDays]>=0 AND [MaximumCarryForwardDays]>=0"));

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LeaveTypeCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.AccrualMethod).HasConversion<byte>();
        builder.Property(x => x.DayCountingMethod).HasConversion<byte>();
        builder.Property(x => x.AnnualEntitlementDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.MaximumCarryForwardDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.IsEncashableOnTermination).HasDefaultValue(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.LeaveTypeCode)
            .IsUnique()
            .HasDatabaseName("UX_LeaveTypes_Code");
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_LeaveTypes_IsActive");
    }
}

public sealed class EmployeeLeaveBalanceConfiguration : IEntityTypeConfiguration<EmployeeLeaveBalance>
{
    public void Configure(EntityTypeBuilder<EmployeeLeaveBalance> builder)
    {
        builder.ToTable("EmployeeLeaveBalances", "hr", table =>
            table.HasCheckConstraint(
                "CK_LeaveBalances_NonNegative",
                "[OpeningBalanceDays]>=0 AND [AccruedDays]>=0 AND [UsedDays]>=0 AND [SettledDays]>=0"));

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OpeningBalanceDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.AccruedDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.UsedDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.AdjustmentDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.SettledDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.Ignore(x => x.AvailableDays);

        builder.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.LeaveYear })
            .IsUnique()
            .HasDatabaseName("UX_LeaveBalances_Employee_Type_Year");
        builder.HasIndex(x => x.LeaveTypeId)
            .HasDatabaseName("IX_LeaveBalances_LeaveTypeId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LeaveBalances_Employees");
        builder.HasOne<LeaveType>()
            .WithMany()
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LeaveBalances_Types");
    }
}

public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests", "hr", table =>
        {
            table.HasCheckConstraint("CK_LeaveRequests_Dates", "[EndDate] >= [StartDate]");
            table.HasCheckConstraint(
                "CK_LeaveRequests_Days",
                "[RequestedDays]>0 AND ([ApprovedDays] IS NULL OR [ApprovedDays]>=0)");
        });

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LeaveRequestCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.LeaveTypeCodeSnapshot).HasMaxLength(32).IsRequired();
        builder.Property(x => x.LeaveTypeNameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(x => x.DayCountingMethodSnapshot).HasConversion<byte>();
        builder.Property(x => x.StartDate).HasColumnType("date");
        builder.Property(x => x.EndDate).HasColumnType("date");
        builder.Property(x => x.RequestedDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.ApprovedDays).HasColumnType("decimal(8,2)");
        builder.Property(x => x.Status).HasConversion<byte>();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.BalanceOverrideReason).HasMaxLength(500);
        builder.Property(x => x.SubmittedBy).HasMaxLength(64);
        builder.Property(x => x.ApprovedBy).HasMaxLength(64);
        builder.Property(x => x.RejectedBy).HasMaxLength(64);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.LeaveRequestCode)
            .IsUnique()
            .HasDatabaseName("UX_LeaveRequests_Code");
        builder.HasIndex(x => x.EmployeeId)
            .HasDatabaseName("IX_LeaveRequests_Employee");
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_LeaveRequests_Status");
        builder.HasIndex(x => new { x.StartDate, x.EndDate })
            .HasDatabaseName("IX_LeaveRequests_Dates");
        builder.HasIndex(x => x.LeaveTypeId)
            .HasDatabaseName("IX_LeaveRequests_LeaveTypeId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LeaveRequests_Employees");
        builder.HasOne<LeaveType>()
            .WithMany()
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LeaveRequests_Types");
    }
}

public sealed class OvertimeRecordConfiguration : IEntityTypeConfiguration<OvertimeRecord>
{
    public void Configure(EntityTypeBuilder<OvertimeRecord> builder)
    {
        builder.ToTable("OvertimeRecords", "hr", table =>
        {
            table.HasCheckConstraint(
                "CK_Overtime_Minutes",
                "[RequestedMinutes]>0 AND [ApprovedMinutes]>=0 AND [ApprovedMinutes]<=[RequestedMinutes]");
            table.HasCheckConstraint("CK_Overtime_Multiplier", "[RateMultiplier]>0");
        });

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OvertimeCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.WorkDate).HasColumnType("date");
        builder.Property(x => x.RateMultiplier).HasColumnType("decimal(9,4)");
        builder.Property(x => x.Status).HasConversion<byte>();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.SubmittedBy).HasMaxLength(64);
        builder.Property(x => x.ApprovedBy).HasMaxLength(64);
        builder.Property(x => x.RejectedBy).HasMaxLength(64);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.OvertimeCode)
            .IsUnique()
            .HasDatabaseName("UX_Overtime_Code");
        builder.HasIndex(x => new { x.EmployeeId, x.WorkDate })
            .HasDatabaseName("IX_Overtime_Employee_Date");
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Overtime_Status");
        builder.HasIndex(x => x.AttendanceRecordId)
            .IsUnique()
            .HasFilter("[AttendanceRecordId] IS NOT NULL")
            .HasDatabaseName("UX_Overtime_AttendanceRecord");
        builder.HasIndex(x => x.EmployeePayrollId).HasDatabaseName("IX_Overtime_EmployeePayrollId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Overtime_Employees");
        builder.HasOne<AttendanceRecord>()
            .WithMany()
            .HasForeignKey(x => x.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Overtime_Attendance");
        builder.HasOne<EmployeePayroll>().WithMany().HasForeignKey(x => x.EmployeePayrollId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Overtime_EmployeePayroll");
    }
}

public sealed class EmployeeLoanConfiguration : IEntityTypeConfiguration<EmployeeLoan>
{
    public void Configure(EntityTypeBuilder<EmployeeLoan> builder)
    {
        builder.ToTable("EmployeeLoans", "hr", table =>
        {
            table.HasCheckConstraint("CK_EmployeeLoans_Principal", "[PrincipalAmount]>0");
            table.HasCheckConstraint("CK_EmployeeLoans_Count", "[InstallmentCount]>0");
        });

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LoanCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.LoanDate).HasColumnType("date");
        builder.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(8).IsRequired();
        builder.Property(x => x.CurrencySymbolSnapshot).HasMaxLength(8);
        builder.Property(x => x.PrincipalAmount).HasColumnType("decimal(19,4)");
        builder.Property(x => x.FirstInstallmentDate).HasColumnType("date");
        builder.Property(x => x.RepaymentMode).HasConversion<byte>();
        builder.Property(x => x.Status).HasConversion<byte>();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.SubmittedBy).HasMaxLength(64);
        builder.Property(x => x.ApprovedBy).HasMaxLength(64);
        builder.Property(x => x.DisbursedBy).HasMaxLength(64);
        builder.Property(x => x.RejectedBy).HasMaxLength(64);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.LoanCode)
            .IsUnique()
            .HasDatabaseName("UX_EmployeeLoans_LoanCode");
        builder.HasIndex(x => new { x.EmployeeId, x.Status })
            .HasDatabaseName("IX_EmployeeLoans_Employee_Status");
        builder.HasIndex(x => x.PaymentVoucherId)
            .IsUnique()
            .HasFilter("[PaymentVoucherId] IS NOT NULL")
            .HasDatabaseName("UX_EmployeeLoans_PaymentVoucher");
        builder.HasIndex(x => x.ContractId).HasDatabaseName("IX_EmployeeLoans_ContractId");
        builder.HasIndex(x => x.SalaryStructureId).HasDatabaseName("IX_EmployeeLoans_SalaryStructureId");
        builder.HasIndex(x => x.CurrencyId).HasDatabaseName("IX_EmployeeLoans_CurrencyId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeLoans_Employees");
        builder.HasOne<EmployeeContract>()
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeLoans_Contracts");
        builder.HasOne<EmployeeSalaryStructure>()
            .WithMany()
            .HasForeignKey(x => x.SalaryStructureId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeLoans_SalaryStructures");
        builder.HasOne<Currency>()
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeLoans_Currencies");
        builder.HasOne<PaymentVoucher>()
            .WithMany()
            .HasForeignKey(x => x.PaymentVoucherId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeLoans_PaymentVouchers");
    }
}

public sealed class EmployeeLoanInstallmentConfiguration : IEntityTypeConfiguration<EmployeeLoanInstallment>
{
    public void Configure(EntityTypeBuilder<EmployeeLoanInstallment> builder)
    {
        builder.ToTable("EmployeeLoanInstallments", "hr", table =>
            table.HasCheckConstraint("CK_LoanInstallments_Amount", "[Amount]>0"));

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DueDate).HasColumnType("date");
        builder.Property(x => x.Amount).HasColumnType("decimal(19,4)");
        builder.Property(x => x.Status).HasConversion<byte>();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.EmployeeLoanId, x.InstallmentSequence })
            .IsUnique()
            .HasDatabaseName("UX_LoanInstallments_Loan_Sequence");
        builder.HasIndex(x => new { x.DueDate, x.Status })
            .HasDatabaseName("IX_LoanInstallments_DueDate_Status");
        builder.HasIndex(x => x.ReceiptVoucherId)
            .HasDatabaseName("IX_LoanInstallments_ReceiptVoucherId");
        builder.HasIndex(x => x.EmployeePayrollId).HasDatabaseName("IX_LoanInstallments_EmployeePayrollId");
        builder.HasIndex(x => x.EndOfServiceSettlementId).HasDatabaseName("IX_LoanInstallments_EndOfServiceSettlementId");

        builder.HasOne<EmployeeLoan>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeLoanId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LoanInstallments_Loans");
        builder.HasOne<ReceiptVoucher>()
            .WithMany()
            .HasForeignKey(x => x.ReceiptVoucherId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LoanInstallments_ReceiptVouchers");
        builder.HasOne<EmployeePayroll>().WithMany().HasForeignKey(x => x.EmployeePayrollId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LoanInstallments_EmployeePayroll");
        builder.HasOne<EndOfServiceSettlement>().WithMany().HasForeignKey(x => x.EndOfServiceSettlementId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LoanInstallments_EndOfServiceSettlement");
    }
}

public sealed class EmployeeAdjustmentConfiguration : IEntityTypeConfiguration<EmployeeAdjustment>
{
    public void Configure(EntityTypeBuilder<EmployeeAdjustment> builder)
    {
        builder.ToTable("EmployeeAdjustments", "hr", table =>
            table.HasCheckConstraint("CK_EmployeeAdjustments_Amount", "[Amount]>0"));

        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AdjustmentCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ComponentCodeSnapshot).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ComponentNameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ComponentTypeSnapshot).HasConversion<byte>();
        builder.Property(x => x.DebitPostingRoleSnapshot).HasMaxLength(50);
        builder.Property(x => x.CreditPostingRoleSnapshot).HasMaxLength(50);
        builder.Property(x => x.AdjustmentType).HasConversion<byte>();
        builder.Property(x => x.EffectiveDate).HasColumnType("date");
        builder.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(8).IsRequired();
        builder.Property(x => x.CurrencySymbolSnapshot).HasMaxLength(8);
        builder.Property(x => x.Amount).HasColumnType("decimal(19,4)");
        builder.Property(x => x.Status).HasConversion<byte>();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SubmittedBy).HasMaxLength(64);
        builder.Property(x => x.ApprovedBy).HasMaxLength(64);
        builder.Property(x => x.RejectedBy).HasMaxLength(64);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.AdjustmentCode)
            .IsUnique()
            .HasDatabaseName("UX_EmployeeAdjustments_Code");
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveDate })
            .HasDatabaseName("IX_Adjustments_Employee_EffectiveDate");
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Adjustments_Status");
        builder.HasIndex(x => x.SalaryComponentId)
            .HasDatabaseName("IX_Adjustments_SalaryComponentId");
        builder.HasIndex(x => x.CurrencyId)
            .HasDatabaseName("IX_Adjustments_CurrencyId");
        builder.HasIndex(x => x.EmployeePayrollId).HasDatabaseName("IX_Adjustments_EmployeePayrollId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeAdjustments_Employees");
        builder.HasOne<SalaryComponent>()
            .WithMany()
            .HasForeignKey(x => x.SalaryComponentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeAdjustments_Components");
        builder.HasOne<Currency>()
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeAdjustments_Currencies");
        builder.HasOne<EmployeePayroll>().WithMany().HasForeignKey(x => x.EmployeePayrollId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeAdjustments_EmployeePayroll");
    }
}
