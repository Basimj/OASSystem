using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("EmployeeDocuments", "hr", t => t.HasCheckConstraint("CK_EmployeeDocuments_FileSize", "[FileSize] > 0"));
        builder.HasKey(x => x.Id);
        builder.ConfigureHrAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DocumentCode).IsRequired().HasMaxLength(40);
        builder.Property(x => x.DocumentType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(512);
        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Sha256Hash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.IssueDate).HasColumnType("date");
        builder.Property(x => x.ExpiryDate).HasColumnType("date");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.DocumentCode).IsUnique().HasDatabaseName("UX_EmployeeDocuments_DocumentCode");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_EmployeeDocuments_EmployeeId");
        builder.HasIndex(x => new { x.EmployeeId, x.IsActive }).HasDatabaseName("IX_EmployeeDocuments_Employee_Status");
        builder.HasIndex(x => x.ExpiryDate).HasDatabaseName("IX_EmployeeDocuments_ExpiryDate");
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EmployeeDocuments_Employees_EmployeeId");
    }
}
