using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable
namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261115180000_SalesInvoiceCollectionIdempotency")]
public sealed class SalesInvoiceCollectionIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "tbl_SalesInvoiceCollectionAttempts", schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Fingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SalesInvoiceCollectionAttempts", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_SalesInvoiceCollectionAttempts_InvoiceId",
            schema: "dbo", table: "tbl_SalesInvoiceCollectionAttempts", column: "InvoiceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "tbl_SalesInvoiceCollectionAttempts", schema: "dbo");
    }
}
