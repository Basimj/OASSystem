using Microsoft.EntityFrameworkCore;

#nullable disable
namespace OAS.Infrastructure.Persistence.Migrations;

partial class OasDbContextModelSnapshot
{
    private static void AddSalesInvoiceCollectionAttemptSnapshot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity("OAS.Infrastructure.Persistence.SalesInvoiceCollectionAttempt", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            b.Property<Guid>("InvoiceId").HasColumnType("uniqueidentifier");
            b.Property<string>("Fingerprint").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ResultJson").IsRequired().HasColumnType("nvarchar(max)");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2");
            b.HasKey("Id");
            b.HasIndex("InvoiceId");
            b.ToTable("tbl_SalesInvoiceCollectionAttempts", "dbo");
        });
    }
}
