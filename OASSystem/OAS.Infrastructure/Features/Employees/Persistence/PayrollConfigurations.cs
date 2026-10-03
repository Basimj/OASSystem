using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.EndOfService;
using OAS.Domain.Features.Employees.Payroll;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class PayrollPolicyConfiguration : IEntityTypeConfiguration<PayrollPolicy>
{
    public void Configure(EntityTypeBuilder<PayrollPolicy> b)
    {
        b.ToTable("PayrollPolicies", "hr", t =>
        {
            t.HasCheckConstraint("CK_PayrollPolicies_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });
        b.HasKey(x => x.Id); b.ConfigureHrAudit(); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PolicyCode).HasMaxLength(32).IsRequired(); b.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        b.Property(x => x.EffectiveFrom).HasColumnType("date"); b.Property(x => x.EffectiveTo).HasColumnType("date");
        b.Property(x => x.Status).HasConversion<byte>(); b.Property(x => x.ProrationMethod).HasConversion<byte>();
        b.Property(x => x.DailyRateMethod).HasConversion<byte>(); b.Property(x => x.HourlyRateMethod).HasConversion<byte>();
        b.Property(x => x.Notes).HasMaxLength(500); b.Property(x => x.ActivatedBy).HasMaxLength(64);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.HasIndex(x => x.PolicyCode).IsUnique().HasDatabaseName("UX_PayrollPolicies_Code");
        b.HasIndex(x => x.Status).IsUnique().HasFilter("[Status] = 2").HasDatabaseName("UX_PayrollPolicies_OneActive");
        b.HasIndex(x => new { x.Status, x.EffectiveFrom, x.EffectiveTo }).HasDatabaseName("IX_PayrollPolicies_Status_Dates");
        ConfigureComponent(b, nameof(PayrollPolicy.AbsenceDeductionComponentId), "FK_PayrollPolicies_AbsenceComponent");
        ConfigureComponent(b, nameof(PayrollPolicy.LateDeductionComponentId), "FK_PayrollPolicies_LateComponent");
        ConfigureComponent(b, nameof(PayrollPolicy.EarlyLeaveDeductionComponentId), "FK_PayrollPolicies_EarlyLeaveComponent");
        ConfigureComponent(b, nameof(PayrollPolicy.UnpaidLeaveComponentId), "FK_PayrollPolicies_UnpaidLeaveComponent");
        ConfigureComponent(b, nameof(PayrollPolicy.OvertimeComponentId), "FK_PayrollPolicies_OvertimeComponent");
    }

    private static void ConfigureComponent(
        EntityTypeBuilder<PayrollPolicy> builder,
        string foreignKeyPropertyName,
        string constraintName)
    {
        builder.HasOne<SalaryComponent>()
            .WithMany()
            .HasForeignKey(foreignKeyPropertyName)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(constraintName);
    }
}

public sealed class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> b)
    {
        b.ToTable("PayrollPeriods", "hr", t => t.HasCheckConstraint("CK_PayrollPeriods_Dates", "[EndDate] >= [StartDate] AND [Month] BETWEEN 1 AND 12"));
        b.HasKey(x => x.Id); b.ConfigureHrAudit(); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PeriodCode).HasMaxLength(20).IsRequired(); b.Property(x => x.StartDate).HasColumnType("date"); b.Property(x => x.EndDate).HasColumnType("date");
        b.Property(x => x.Status).HasConversion<byte>(); b.Property(x => x.LockedBy).HasMaxLength(64); b.Property(x => x.ClosedBy).HasMaxLength(64);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.HasIndex(x => x.PeriodCode).IsUnique().HasDatabaseName("UX_PayrollPeriods_Code");
        b.HasIndex(x => new { x.Year, x.Month }).IsUnique().HasDatabaseName("UX_PayrollPeriods_Year_Month");
    }
}

public sealed class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> b)
    {
        b.ToTable("PayrollRuns", "hr", t =>
        {
            t.HasCheckConstraint("CK_PayrollRuns_Totals", "[TotalGrossEarnings]>=0 AND [TotalDeductions]>=0 AND [TotalEmployerContributions]>=0 AND [TotalNetPay]>=0 AND [BaseGrossEarnings]>=0 AND [BaseDeductions]>=0 AND [BaseEmployerContributions]>=0 AND [BaseNetPay]>=0");
            t.HasCheckConstraint("CK_PayrollRuns_PostingRate", "[PostingExchangeRate] IS NULL OR [PostingExchangeRate] > 0");
        });
        b.HasKey(x => x.Id); b.ConfigureHrAudit(); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PayrollRunCode).HasMaxLength(40).IsRequired(); b.Property(x => x.RunType).HasConversion<byte>(); b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.CalculationDate).HasColumnType("date"); b.Property(x => x.PostingDate).HasColumnType("date");
        b.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(8).IsRequired(); b.Property(x => x.CurrencySymbolSnapshot).HasMaxLength(12);
        b.Property(x => x.BaseCurrencyCodeSnapshot).HasMaxLength(8); b.Property(x => x.PostingExchangeRate).HasPrecision(19,8); b.Property(x => x.PostingExchangeRateDate).HasColumnType("date");
        foreach (var name in new[]{nameof(PayrollRun.TotalGrossEarnings),nameof(PayrollRun.TotalDeductions),nameof(PayrollRun.TotalEmployerContributions),nameof(PayrollRun.TotalNetPay),nameof(PayrollRun.BaseGrossEarnings),nameof(PayrollRun.BaseDeductions),nameof(PayrollRun.BaseEmployerContributions),nameof(PayrollRun.BaseNetPay)}) b.Property(name).HasPrecision(19,4);
        b.Property(x => x.CalculatedBy).HasMaxLength(64); b.Property(x => x.ReviewedBy).HasMaxLength(64); b.Property(x => x.ApprovedBy).HasMaxLength(64); b.Property(x => x.PostedBy).HasMaxLength(64); b.Property(x => x.ClosedBy).HasMaxLength(64); b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.HasIndex(x => x.PayrollRunCode).IsUnique().HasDatabaseName("UX_PayrollRuns_Code");
        b.HasIndex(x => new { x.PayrollPeriodId, x.CurrencyId, x.RunType }).IsUnique().HasFilter("[Status] <> 7").HasDatabaseName("UX_PayrollRuns_Period_Currency_Type_Active");
        b.HasIndex(x => new { x.PayrollPeriodId, x.Status }).HasDatabaseName("IX_PayrollRuns_Period_Status"); b.HasIndex(x => x.CurrencyId).HasDatabaseName("IX_PayrollRuns_Currency");
        b.HasOne<PayrollPeriod>().WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollRuns_Period");
        b.HasOne<PayrollPolicy>().WithMany().HasForeignKey(x => x.PayrollPolicyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollRuns_Policy");
        b.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollRuns_Currency");
        b.HasOne<Currency>().WithMany().HasForeignKey(x => x.BaseCurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollRuns_BaseCurrency");
        b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollRuns_Journal");
    }
}

public sealed class EmployeePayrollConfiguration : IEntityTypeConfiguration<EmployeePayroll>
{
    public void Configure(EntityTypeBuilder<EmployeePayroll> b)
    {
        b.ToTable("EmployeePayrolls", "hr", t =>
        {
            t.HasCheckConstraint("CK_EmployeePayrolls_Coverage", "[CoverageTo] >= [CoverageFrom]");
            t.HasCheckConstraint("CK_EmployeePayrolls_Totals", "[ConfiguredBasicSalarySnapshot]>=0 AND [CalculatedBasicSalary]>=0 AND [GrossEarnings]>=0 AND [TotalDeductions]>=0 AND [TotalEmployerContributions]>=0 AND [NetPay]>=0 AND [BaseGrossEarnings]>=0 AND [BaseDeductions]>=0 AND [BaseEmployerContributions]>=0 AND [BaseNetPay]>=0");
        });
        b.HasKey(x => x.Id); b.ConfigureHrAudit(); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.CoverageFrom).HasColumnType("date"); b.Property(x => x.CoverageTo).HasColumnType("date");
        b.Property(x => x.EmployeeCodeSnapshot).HasMaxLength(32).IsRequired(); b.Property(x => x.EmployeeNameSnapshot).HasMaxLength(220).IsRequired(); b.Property(x => x.JobTitleSnapshot).HasMaxLength(150); b.Property(x => x.DepartmentSnapshot).HasMaxLength(150); b.Property(x => x.ContractCodeSnapshot).HasMaxLength(40);
        b.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(8).IsRequired(); b.Property(x => x.CurrencySymbolSnapshot).HasMaxLength(12); b.Property(x => x.Status).HasConversion<byte>();
        foreach (var name in new[]{nameof(EmployeePayroll.ConfiguredBasicSalarySnapshot),nameof(EmployeePayroll.CalculatedBasicSalary),nameof(EmployeePayroll.GrossEarnings),nameof(EmployeePayroll.TotalDeductions),nameof(EmployeePayroll.TotalEmployerContributions),nameof(EmployeePayroll.NetPay),nameof(EmployeePayroll.BaseGrossEarnings),nameof(EmployeePayroll.BaseDeductions),nameof(EmployeePayroll.BaseEmployerContributions),nameof(EmployeePayroll.BaseNetPay)}) b.Property(name).HasPrecision(19,4);
        b.Property(x => x.ReviewedBy).HasMaxLength(64); b.Property(x => x.ApprovedBy).HasMaxLength(64); b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.HasIndex(x => new { x.PayrollRunId, x.EmployeeId }).IsUnique().HasDatabaseName("UX_EmployeePayroll_Run_Employee");
        b.HasIndex(x => new { x.PayrollPeriodId, x.EmployeeId }).IsUnique().HasFilter("[Status] <> 5").HasDatabaseName("UX_EmployeePayroll_Period_Employee");
        b.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_EmployeePayroll_Employee"); b.HasIndex(x => x.Status).HasDatabaseName("IX_EmployeePayroll_Status");
        b.HasOne<PayrollRun>().WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrolls_Run");
        b.HasOne<PayrollPeriod>().WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrolls_Period");
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrolls_Employee");
        b.HasOne<EmployeeContract>().WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrolls_Contract");
        b.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrolls_Currency");
    }
}

public sealed class EmployeePayrollSalarySegmentConfiguration : IEntityTypeConfiguration<EmployeePayrollSalarySegment>
{
    public void Configure(EntityTypeBuilder<EmployeePayrollSalarySegment> b)
    {
        b.ToTable("EmployeePayrollSalarySegments", "hr", t =>
        {
            t.HasCheckConstraint("CK_PayrollSalarySegments_Dates", "[EffectiveTo] >= [EffectiveFrom]");
            t.HasCheckConstraint("CK_PayrollSalarySegments_Amounts", "[BasicSalaryRateSnapshot]>=0 AND [ProrationFactor]>=0 AND [ProrationFactor]<=1");
        });
        b.HasKey(x => x.Id); b.ConfigureHrAudit(); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.ContractCodeSnapshot).HasMaxLength(40); b.Property(x => x.SalaryStructureCodeSnapshot).HasMaxLength(40).IsRequired();
        b.Property(x => x.EffectiveFrom).HasColumnType("date"); b.Property(x => x.EffectiveTo).HasColumnType("date"); b.Property(x => x.BasicSalaryRateSnapshot).HasPrecision(19,4); b.Property(x => x.ProrationFactor).HasPrecision(18,8); b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.HasIndex(x => x.EmployeePayrollId).HasDatabaseName("IX_PayrollSalarySegments_Payroll");
        b.HasOne<EmployeePayroll>().WithMany().HasForeignKey(x => x.EmployeePayrollId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollSalarySegments_Payroll");
        b.HasOne<EmployeeContract>().WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollSalarySegments_Contract");
        b.HasOne<EmployeeSalaryStructure>().WithMany().HasForeignKey(x => x.SalaryStructureId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PayrollSalarySegments_Structure");
    }
}

public sealed class EmployeePayrollLineConfiguration : IEntityTypeConfiguration<EmployeePayrollLine>
{
    public void Configure(EntityTypeBuilder<EmployeePayrollLine> b)
    {
        b.ToTable("EmployeePayrollLines", "hr", t => t.HasCheckConstraint("CK_EmployeePayrollLines_Amount", "[Amount]>=0"));
        b.HasKey(x => x.Id); b.ConfigureHrAudit(); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.ComponentCodeSnapshot).HasMaxLength(32); b.Property(x => x.ComponentNameSnapshot).HasMaxLength(150).IsRequired();
        b.Property(x => x.ComponentType).HasConversion<byte>(); b.Property(x => x.SourceType).HasConversion<byte>(); b.Property(x => x.SourceModule).HasMaxLength(50); b.Property(x => x.SourceDocumentType).HasMaxLength(80); b.Property(x => x.SourceDate).HasColumnType("date");
        b.Property(x => x.Quantity).HasPrecision(18,4); b.Property(x => x.Rate).HasPrecision(19,6); b.Property(x => x.Amount).HasPrecision(19,4); b.Property(x => x.DebitPostingRole).HasMaxLength(50); b.Property(x => x.CreditPostingRole).HasMaxLength(50); b.Property(x => x.Description).HasMaxLength(500); b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.HasIndex(x => new { x.EmployeePayrollId, x.LineSequence }).IsUnique().HasDatabaseName("UX_EmployeePayrollLines_Payroll_Sequence");
        b.HasIndex(x => new { x.SourceType, x.SourceDocumentId }).HasDatabaseName("IX_EmployeePayrollLines_Source");
        b.HasOne<EmployeePayroll>().WithMany().HasForeignKey(x => x.EmployeePayrollId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrollLines_Payroll");
        b.HasOne<EmployeeSalaryStructure>().WithMany().HasForeignKey(x => x.SalaryStructureId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrollLines_Structure");
        b.HasOne<SalaryComponent>().WithMany().HasForeignKey(x => x.SalaryComponentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeePayrollLines_Component");
    }
}

public sealed class EndOfServiceSettlementConfiguration : IEntityTypeConfiguration<EndOfServiceSettlement>
{
    public void Configure(EntityTypeBuilder<EndOfServiceSettlement> b)
    {
        b.ToTable("EndOfServiceSettlements", "hr", t =>
        {
            t.HasCheckConstraint("CK_EndOfService_Amounts", "[OutstandingPayrollAmountSnapshot]>=0 AND [LeaveSettlementAmount]>=0 AND [EndOfServiceBenefitAmount]>=0 AND [OtherEarningsAmount]>=0 AND [LoanDeductionAmount]>=0 AND [OtherDeductionsAmount]>=0 AND [GrossSettlementAmount]>=0 AND [NetSettlementAmount]>=0 AND [BaseGrossSettlementAmount]>=0 AND [BaseNetSettlementAmount]>=0");
            t.HasCheckConstraint("CK_EndOfService_Rate", "[PostingExchangeRate] IS NULL OR [PostingExchangeRate]>0");
        });
        b.HasKey(x => x.Id); b.ConfigureHrAudit(); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.SettlementCode).HasMaxLength(40).IsRequired(); b.Property(x => x.LastWorkingDate).HasColumnType("date"); b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(8).IsRequired(); b.Property(x => x.CurrencySymbolSnapshot).HasMaxLength(12);
        foreach(var name in new[]{nameof(EndOfServiceSettlement.OutstandingPayrollAmountSnapshot),nameof(EndOfServiceSettlement.LeaveSettlementAmount),nameof(EndOfServiceSettlement.EndOfServiceBenefitAmount),nameof(EndOfServiceSettlement.OtherEarningsAmount),nameof(EndOfServiceSettlement.LoanDeductionAmount),nameof(EndOfServiceSettlement.OtherDeductionsAmount),nameof(EndOfServiceSettlement.GrossSettlementAmount),nameof(EndOfServiceSettlement.NetSettlementAmount),nameof(EndOfServiceSettlement.BaseGrossSettlementAmount),nameof(EndOfServiceSettlement.BaseNetSettlementAmount)}) b.Property(name).HasPrecision(19,4);
        b.Property(x => x.PostingExchangeRate).HasPrecision(19,8); b.Property(x => x.PostingExchangeRateDate).HasColumnType("date"); b.Property(x => x.Reason).HasMaxLength(500); b.Property(x => x.CalculatedBy).HasMaxLength(64); b.Property(x => x.ReviewedBy).HasMaxLength(64); b.Property(x => x.ApprovedBy).HasMaxLength(64); b.Property(x => x.PostedBy).HasMaxLength(64); b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.HasIndex(x => x.SettlementCode).IsUnique().HasDatabaseName("UX_EndOfService_SettlementCode"); b.HasIndex(x => x.EmployeeId).IsUnique().HasFilter("[Status] <> 7").HasDatabaseName("UX_EndOfService_Employee_Active");
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EndOfService_Employee");
        b.HasOne<EmployeeContract>().WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EndOfService_Contract");
        b.HasOne<EmployeePayroll>().WithMany().HasForeignKey(x => x.FinalEmployeePayrollId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EndOfService_FinalPayroll");
        b.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EndOfService_Currency");
        b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EndOfService_Journal");
    }
}

public sealed class EndOfServiceSettlementLineConfiguration : IEntityTypeConfiguration<EndOfServiceSettlementLine>
{
    public void Configure(EntityTypeBuilder<EndOfServiceSettlementLine> b)
    {
        b.ToTable("EndOfServiceSettlementLines", "hr", t => t.HasCheckConstraint("CK_EndOfServiceLines_Amount", "[Amount]>=0"));
        b.HasKey(x => x.Id); b.ConfigureHrAudit(); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.LineType).HasConversion<byte>(); b.Property(x => x.SourceDocumentType).HasMaxLength(80); b.Property(x => x.Description).HasMaxLength(500).IsRequired(); b.Property(x => x.Quantity).HasPrecision(18,4); b.Property(x => x.Rate).HasPrecision(19,6); b.Property(x => x.Amount).HasPrecision(19,4); b.Property(x => x.DebitPostingRole).HasMaxLength(50); b.Property(x => x.CreditPostingRole).HasMaxLength(50); b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.HasIndex(x => new { x.EndOfServiceSettlementId, x.LineSequence }).IsUnique().HasDatabaseName("UX_EndOfServiceLines_Settlement_Sequence");
        b.HasOne<EndOfServiceSettlement>().WithMany().HasForeignKey(x => x.EndOfServiceSettlementId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EndOfServiceLines_Settlement");
    }
}
