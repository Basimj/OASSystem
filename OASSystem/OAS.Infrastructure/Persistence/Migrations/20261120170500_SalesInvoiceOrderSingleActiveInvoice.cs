using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable
namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261120170500_SalesInvoiceOrderSingleActiveInvoice")]
public sealed class SalesInvoiceOrderSingleActiveInvoice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Previous version tried to enforce uniqueness over every historical invoice,
        // and failed on legitimate or unresolved orders with several active invoices.
        // Preserve all historical rows. Only invoices generated after this migration
        // by SalesInvoiceFromOrderService get NewOrderInvoiceKey = CustomerOrderId.
        migrationBuilder.AddColumn<Guid>(
            name: "NewOrderInvoiceKey",
            schema: "dbo",
            table: "tbl_SalesInvoices",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "UX_SalesInvoices_NewActiveOrderInvoiceKey",
            schema: "dbo",
            table: "tbl_SalesInvoices",
            column: "NewOrderInvoiceKey",
            unique: true,
            filter: "[NewOrderInvoiceKey] IS NOT NULL AND [Status] <> 4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_SalesInvoices_NewActiveOrderInvoiceKey",
            schema: "dbo",
            table: "tbl_SalesInvoices");
        migrationBuilder.DropColumn(
            name: "NewOrderInvoiceKey",
            schema: "dbo",
            table: "tbl_SalesInvoices");
    }
}
