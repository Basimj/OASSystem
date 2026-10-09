using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class OpticalJobConfiguration : IEntityTypeConfiguration<OpticalJob>
{
    public void Configure(EntityTypeBuilder<OpticalJob> builder)
    {
        builder.ToTable("tbl_OpticalJobs", "dbo");
        builder.HasKey(x => x.Id).HasName("PK_tbl_OpticalJobs");
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.JobCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CustomerOrderId).IsRequired();
        builder.Property(x => x.SalesInvoiceId);
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.RequiredDate).HasColumnType("date");
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.AssignedTechnicianId);
        builder.Property(x => x.AssignedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.StartedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.CompletedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.DeliveredAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.JobCode).IsUnique().HasDatabaseName("UX_OpticalJobs_JobCode");
        builder.HasIndex(x => x.CustomerOrderId).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_OpticalJobs_CustomerOrderId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_OpticalJobs_Status");
        builder.HasIndex(x => x.AssignedTechnicianId).HasDatabaseName("IX_OpticalJobs_AssignedTechnicianId");
        builder.HasIndex(x => x.RequiredDate).HasDatabaseName("IX_OpticalJobs_RequiredDate");
        builder.HasIndex(x => x.SalesInvoiceId).HasDatabaseName("IX_OpticalJobs_SalesInvoiceId");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_OpticalJobs_CustomerId");

        builder.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.CustomerOrderId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalJobs_CustomerOrders_CustomerOrderId");
        builder.HasOne<SalesInvoice>().WithMany().HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalJobs_SalesInvoices_SalesInvoiceId");
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalJobs_Customers_CustomerId");
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalJobs_Employees_AssignedTechnicianId");

        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OpticalJobId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalJobLines_OpticalJobs_OpticalJobId");
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
