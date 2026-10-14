using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261013170000_RemovePayOnPickupSalesPaymentPlan")]
public sealed class RemovePayOnPickupSalesPaymentPlan : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

/*
   SalesPaymentPlan value 3 was the legacy pay-on-pickup plan.
   The supported plans are now:
     1 = FullNow
     2 = PartialNow
     4 = AccountCredit

   Existing value-3 orders/invoices are migrated to PartialNow so they keep
   the safe rule that any remaining balance must be settled before delivery.
   Numeric value 3 remains reserved and must not be reused by another enum.
*/
IF OBJECT_ID(N'[dbo].[tbl_CustomerOrders]', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.tbl_CustomerOrders', N'PaymentPlan') IS NOT NULL
BEGIN
    EXEC(N'UPDATE [dbo].[tbl_CustomerOrders]
           SET [PaymentPlan] = 2
           WHERE [PaymentPlan] = 3;');
END;

IF OBJECT_ID(N'[dbo].[tbl_SalesInvoices]', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.tbl_SalesInvoices', N'PaymentPlan') IS NOT NULL
BEGIN
    EXEC(N'UPDATE [dbo].[tbl_SalesInvoices]
           SET [PaymentPlan] = 2
           WHERE [PaymentPlan] = 3;');
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        /*
           Intentionally no data rollback: after value 3 is merged into PartialNow,
           the database cannot distinguish former pay-on-pickup records from genuine
           partial-payment records without inventing business data.
        */
    }
}
