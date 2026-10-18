using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OAS.Infrastructure.Persistence.OasDbContext))]
[Migration("20261018170000_AddCustomerCreditApplications")]
public sealed class AddCustomerCreditApplications : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable(name:"tbl_CustomerCreditApplications",schema:"dbo",columns:table=>new
        {
            Id=table.Column<Guid>(type:"uniqueidentifier",nullable:false),
            SalesReturnId=table.Column<Guid>(type:"uniqueidentifier",nullable:false),
            CustomerId=table.Column<Guid>(type:"uniqueidentifier",nullable:false),
            SettlementKind=table.Column<byte>(type:"tinyint",nullable:false),
            TargetSalesInvoiceId=table.Column<Guid>(type:"uniqueidentifier",nullable:true),
            RefundPaymentVoucherLineId=table.Column<Guid>(type:"uniqueidentifier",nullable:true),
            AppliedSourceBaseAmount=table.Column<decimal>(type:"decimal(19,4)",nullable:false),
            TargetBaseAmount=table.Column<decimal>(type:"decimal(19,4)",nullable:true),
            TargetInvoiceCurrencyAmount=table.Column<decimal>(type:"decimal(19,4)",nullable:true),
            RefundSettlementBaseAmount=table.Column<decimal>(type:"decimal(19,4)",nullable:true),
            ReversalReceiptVoucherLineId=table.Column<Guid>(type:"uniqueidentifier",nullable:true),
            AppliedAtUtc=table.Column<DateTime>(type:"datetime2(3)",nullable:false),
            AppliedBy=table.Column<string>(type:"nvarchar(64)",maxLength:64,nullable:false),
            Status=table.Column<byte>(type:"tinyint",nullable:false),
            ReversedAtUtc=table.Column<DateTime>(type:"datetime2(3)",nullable:true),
            ReversedBy=table.Column<string>(type:"nvarchar(64)",maxLength:64,nullable:true),
            ReversalReason=table.Column<string>(type:"nvarchar(300)",maxLength:300,nullable:true),
            IdempotencyKey=table.Column<string>(type:"nvarchar(80)",maxLength:80,nullable:true),
            CreatedAt=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:false),
            CreatedBy=table.Column<string>(type:"nvarchar(64)",maxLength:64,nullable:true),
            UpdatedAt=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:true),
            UpdatedBy=table.Column<string>(type:"nvarchar(64)",maxLength:64,nullable:true),
            CreatedFromDevice=table.Column<string>(type:"nvarchar(256)",maxLength:256,nullable:true),
            UpdatedFromDevice=table.Column<string>(type:"nvarchar(256)",maxLength:256,nullable:true),
            RowVersion=table.Column<byte[]>(type:"rowversion",rowVersion:true,nullable:false)
        },constraints:table=>
        {
            table.PrimaryKey("PK_tbl_CustomerCreditApplications",x=>x.Id);
            table.CheckConstraint("CK_CustomerCreditApplications_SourcePositive","[AppliedSourceBaseAmount] > 0");
            table.CheckConstraint("CK_CustomerCreditApplications_Kind","[SettlementKind] IN (1,2)");
            table.CheckConstraint("CK_CustomerCreditApplications_Status","[Status] IN (1,2)");
            table.CheckConstraint("CK_CustomerCreditApplications_TargetShape","([SettlementKind]=1 AND [TargetSalesInvoiceId] IS NOT NULL AND [RefundPaymentVoucherLineId] IS NULL AND [TargetBaseAmount]>0 AND [TargetInvoiceCurrencyAmount]>0 AND [RefundSettlementBaseAmount] IS NULL) OR ([SettlementKind]=2 AND [TargetSalesInvoiceId] IS NULL AND [RefundPaymentVoucherLineId] IS NOT NULL AND [TargetBaseAmount] IS NULL AND [TargetInvoiceCurrencyAmount] IS NULL AND [RefundSettlementBaseAmount]>0)");
            table.CheckConstraint("CK_CustomerCreditApplications_ReversalState","([Status]=1 AND [ReversedAtUtc] IS NULL AND [ReversedBy] IS NULL AND [ReversalReason] IS NULL) OR ([Status]=2 AND [ReversedAtUtc] IS NOT NULL AND [ReversedBy] IS NOT NULL AND [ReversalReason] IS NOT NULL)");
            table.ForeignKey("FK_CustomerCreditApplications_SalesReturns",x=>x.SalesReturnId,"tbl_SalesReturns","Id",principalSchema:"dbo",onDelete:ReferentialAction.Restrict);
            table.ForeignKey("FK_CustomerCreditApplications_SalesInvoices",x=>x.TargetSalesInvoiceId,"tbl_SalesInvoices","Id",principalSchema:"dbo",onDelete:ReferentialAction.Restrict);
            table.ForeignKey("FK_CustomerCreditApplications_Customers",x=>x.CustomerId,"tbl_Customers","Id",principalSchema:"dbo",onDelete:ReferentialAction.Restrict);
            table.ForeignKey("FK_CustomerCreditApplications_PaymentVoucherLines",x=>x.RefundPaymentVoucherLineId,"tbl_PaymentVoucherLines","Id",principalSchema:"dbo",onDelete:ReferentialAction.Restrict);
            table.ForeignKey("FK_CustomerCreditApplications_ReceiptVoucherLines",x=>x.ReversalReceiptVoucherLineId,"tbl_ReceiptVoucherLines","Id",principalSchema:"dbo",onDelete:ReferentialAction.Restrict);
        });
        m.CreateIndex(name:"IX_CustomerCreditApplications_Return_Status",schema:"dbo",table:"tbl_CustomerCreditApplications",columns:new[]{"SalesReturnId","Status"});
        m.CreateIndex(name:"IX_CustomerCreditApplications_Invoice_Status",schema:"dbo",table:"tbl_CustomerCreditApplications",columns:new[]{"TargetSalesInvoiceId","Status"});
        m.CreateIndex(name:"IX_CustomerCreditApplications_Customer_Status",schema:"dbo",table:"tbl_CustomerCreditApplications",columns:new[]{"CustomerId","Status"});
        m.CreateIndex(name:"UX_CustomerCreditApplications_IdempotencyKey",schema:"dbo",table:"tbl_CustomerCreditApplications",column:"IdempotencyKey",unique:true,filter:"[IdempotencyKey] IS NOT NULL");
        m.CreateIndex(name:"UX_CustomerCreditApplications_ReversalReceipt",schema:"dbo",table:"tbl_CustomerCreditApplications",column:"ReversalReceiptVoucherLineId",unique:true,filter:"[ReversalReceiptVoucherLineId] IS NOT NULL");
        m.CreateIndex(name:"UX_CustomerCreditApplications_RefundLine",schema:"dbo",table:"tbl_CustomerCreditApplications",column:"RefundPaymentVoucherLineId",unique:true,filter:"[RefundPaymentVoucherLineId] IS NOT NULL");
    }
    protected override void Down(MigrationBuilder m)=>m.DropTable("tbl_CustomerCreditApplications","dbo");
}
