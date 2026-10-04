using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;
namespace OAS.Infrastructure.Sales.Persistence.Configurations;
public sealed class CommissionEntryConfiguration : IEntityTypeConfiguration<CommissionEntry>
{
 public void Configure(EntityTypeBuilder<CommissionEntry> b){
  b.ToTable("tbl_CommissionEntries","dbo"); b.HasKey(x=>x.Id); b.ConfigureSalesAudit(); b.Property(x=>x.Id).ValueGeneratedNever();
  b.Property(x=>x.SourceDocumentType).HasMaxLength(40).IsRequired(); b.Property(x=>x.SourceDate).HasColumnType("date");
  b.Property(x=>x.BaseSalesAmount).HasPrecision(19,4); b.Property(x=>x.RatePercent).HasPrecision(9,4); b.Property(x=>x.CommissionBaseAmount).HasPrecision(19,4);
  b.Property(x=>x.RuleCodeSnapshot).HasMaxLength(40).IsRequired(); b.Property(x=>x.RuleNameSnapshot).HasMaxLength(160).IsRequired();
  b.HasIndex(x=>new{x.SourceDocumentType,x.SourceLineId}).IsUnique().HasDatabaseName("UX_CommissionEntries_SourceLine"); b.HasIndex(x=>x.OriginalSalesInvoiceLineId).HasDatabaseName("IX_CommissionEntries_OriginalSalesInvoiceLineId"); b.HasIndex(x=>x.EmployeeId).HasDatabaseName("IX_CommissionEntries_EmployeeId");
  b.HasOne<Employee>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CommissionEntries_Employees_EmployeeId");
  b.HasOne<CommissionRule>().WithMany().HasForeignKey(x=>x.CommissionRuleId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CommissionEntries_CommissionRules_CommissionRuleId");
  b.HasOne<SalesInvoice>().WithMany().HasForeignKey(x=>x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CommissionEntries_SalesInvoices_SalesInvoiceId");
  b.HasOne<SalesReturn>().WithMany().HasForeignKey(x=>x.SalesReturnId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CommissionEntries_SalesReturns_SalesReturnId");
 }
}
