using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;
namespace OAS.Infrastructure.Sales.Persistence.Configurations;
public sealed class CommissionStatementConfiguration : IEntityTypeConfiguration<CommissionStatement>
{
 public void Configure(EntityTypeBuilder<CommissionStatement> b){
  b.ToTable("tbl_CommissionStatements","dbo",t=>t.HasCheckConstraint("CK_CommissionStatements_Period","[ToDate] >= [FromDate]")); b.HasKey(x=>x.Id); b.ConfigureSalesAudit(); b.Property(x=>x.Id).ValueGeneratedNever();
  b.Property(x=>x.StatementCode).HasMaxLength(40).IsRequired(); b.Property(x=>x.EmployeeId).IsRequired(); b.Property(x=>x.FromDate).HasColumnType("date"); b.Property(x=>x.ToDate).HasColumnType("date"); b.Property(x=>x.Status).HasConversion<byte>();
  b.Property(x=>x.SalesBaseAmount).HasPrecision(19,4); b.Property(x=>x.ReturnsBaseAmount).HasPrecision(19,4); b.Property(x=>x.CommissionBaseAmount).HasPrecision(19,4);
  b.Property(x=>x.CalculatedAtUtc).HasColumnType("datetimeoffset"); b.Property(x=>x.CalculatedBy).HasMaxLength(64); b.Property(x=>x.FinalizedAtUtc).HasColumnType("datetimeoffset"); b.Property(x=>x.FinalizedBy).HasMaxLength(64); b.Property(x=>x.IsActive).IsRequired(); b.Property(x=>x.RowVersion).IsRowVersion().IsConcurrencyToken();
  b.HasIndex(x=>x.StatementCode).IsUnique().HasDatabaseName("UX_CommissionStatements_StatementCode"); b.HasIndex(x=>new{x.EmployeeId,x.FromDate,x.ToDate}).HasDatabaseName("IX_CommissionStatements_Employee_Period");
  b.HasOne<Employee>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CommissionStatements_Employees_EmployeeId");
  b.HasMany(x=>x.Entries).WithOne().HasForeignKey(x=>x.CommissionStatementId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_CommissionEntries_Statements_CommissionStatementId"); b.Navigation(x=>x.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);
 }
}
