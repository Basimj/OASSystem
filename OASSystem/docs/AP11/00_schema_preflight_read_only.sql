/* AP-11 PRE-FLIGHT: READ ONLY, run on a restored TEST database.
   Confirm current DB in SSMS before executing. */
SET NOCOUNT ON;
SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS ServerName, SYSDATETIMEOFFSET() AS CheckedAt;
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
SELECT t.name AS TableName FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
WHERE s.name=N'dbo' AND t.name IN (N'tbl_PurchaseInvoices',N'tbl_PurchaseReceipts',
 N'tbl_JournalEntries',N'tbl_JournalEntryLines',N'tbl_SupplierCreditApplications',
 N'tbl_PaymentAllocations',N'tbl_PurchasingIntegrationJobs') ORDER BY t.name;
SELECT t.name AS TableName,i.name AS IndexName,i.is_unique,i.is_disabled,i.has_filter,i.filter_definition
FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id
WHERE i.name IN (N'UX_JournalEntries_SourceDocument',N'UX_PaymentAllocations_IdempotencyKey',
 N'UX_PurchaseInvoiceReceiptAllocations_InvoiceLine_ReceiptLine',
 N'UX_PurchasingIntegrationJobs_Receipt_CustomerOrder',N'UX_PurchasingIntegrationJobs_Receipt_RequestLine')
ORDER BY t.name,i.name;
SELECT t.name AS TableName,fk.name AS ForeignKey,fk.is_disabled,fk.is_not_trusted
FROM sys.foreign_keys fk JOIN sys.tables t ON t.object_id=fk.parent_object_id
WHERE t.name IN (N'tbl_PaymentAllocations',N'tbl_PurchasingIntegrationJobs',N'tbl_PurchaseInvoices',
 N'tbl_PurchaseReceipts',N'tbl_PurchaseReturns',N'tbl_JournalEntryLines')
ORDER BY t.name,fk.name;
SELECT t.name AS TableName,c.name AS ColumnName,ty.name AS SqlType,c.is_nullable
FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id
JOIN sys.types ty ON ty.user_type_id=c.user_type_id
WHERE t.name IN (N'tbl_PaymentAllocations',N'tbl_PurchaseInvoices',N'tbl_PurchasingIntegrationJobs')
  AND c.name IN (N'TargetDocumentAmount',N'ReversedAtUtc',N'RowVersion',N'IdempotencyKey',
 N'CurrencyCodeSnapshot',N'NextAttemptAtUtc',N'DueDate') ORDER BY t.name,c.name;
