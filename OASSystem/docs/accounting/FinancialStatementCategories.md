# Financial statement classification — phase 1

Base: OASSystem_JournalReversal_Hardening_Full.zip.

## Database upgrade

Back up the current database and verify the connection string before upgrading. From the solution directory (with .NET 9 and EF Core tools installed):

```
dotnet build OASSystem.sln
dotnet ef database update --project OAS.Infrastructure --startup-project OAS.API --context OasDbContext
```

Migration `20261126090000_AddFinancialStatementCategory` adds a nullable tinyint column to `dbo.tbl_Accounts`. It classifies known linked accounts and active SalesInvoice posting-profile roles using actual IDs and class compatibility. Conflicting compatible proposals remain unset. It does not delete accounts, alter accounting movements, or recalculate balances. Row versions of classified records change, so reopen old account tabs after upgrading. Review suggested classifications before preparing formal financial statements. Down drops the field and its classifications, not journal entries.

## Workflow

Open Accounting > Chart of Accounts. The Financial statement categories button opens the bulk dialog. The dialog shows the entire directory, searches account names and numbers and filters unclassified/inherited/explicit assignments. Changes are saved together under the existing account edit permission; the write checks the class and row version. A null value means inherit from the closest compatible ancestor, not a copy of its category. Equity has one implicit category. Existing unmatched accounts stay unclassified; a manually created posting account must have an effective category. Header accounts may remain unset. Existing managed accounts can be classified through the dedicated dialog; their structural editing restrictions remain in effect.

Changing financial presentation metadata changes how future executions of historical-period financial statements group the accounts. No historical version of this metadata is stored in this phase. System-generated accounts continue their existing creation flows and can inherit categories or be reviewed in the bulk dialog.

Income statement and balance sheet services/visuals are not included in this phase. They must use the effective category resolver and include unclassified balances rather than silently discarding them.

## Verification

Run the account-domain/application tests, then validate the migration on a backed-up staging database. Confirm inheritance after changing a parent, explicit child overrides, invalid classifications, existing linked accounts and the unchanged chart/ledger totals. No SDK or SQL Server execution was available during patch preparation.
