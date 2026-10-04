# Foundation Gap Completion - Test Matrix

This folder contains the regression/acceptance coverage added for the foundation-gap task.

| Requirement | Automated coverage |
|---|---|
| Exact lens SKU identity | LensVariantDetailDomainTests, LensVariantResolverTests |
| One LensVariantDetail per ProductVariant | FoundationGapEfConfigurationTests, FoundationGapSqlServerConcurrencyTests |
| Manual/stored optical snapshot | CustomerOrderLineOpticalSnapshotDomainTests |
| Snapshot immutable outside Draft | CustomerOrderLineOpticalSnapshotDomainTests |
| Availability query is read-only | CustomerOrderAvailabilityServiceTests |
| Customer demand retry/idempotency | CustomerDemandProcurementServiceTests |
| Preferred supplier / schedule | CustomerDemandProcurementServiceTests |
| Receipt-like inbound -> reservation -> order status | FoundationGapWorkflowIntegrationTests |
| Inventory reservation concurrency | Existing ProductOpeningSqlServerTests + RowVersion model tests |
| Customer advance invariants | CustomerAdvanceFoundationDomainTests |
| Typed advance payment allocation | CustomerAdvancePaymentAllocationTests |
| Optical job lifecycle | OpticalJobFoundationDomainTests, OpticalJobServiceTests |
| One active optical job per order | FoundationGapEfConfigurationTests |
| Lab DTO financial isolation | FoundationGapArchitectureTests |
| EF tables/indexes/checks/rowversion | FoundationGapEfConfigurationTests |
| Raw SQL migration policy | FoundationGapMigrationTests |
| API routing/security | FoundationGapControllersTests |
| Granular permissions | FoundationGapPermissionTests |
| Architecture/base entity/enum parity | FoundationGapArchitectureTests + existing DependencyTests |

## SQL Server opt-in tests

Set `OAS_FOUNDATION_TEST_SQLSERVER` to a SQL Server connection string to run `FoundationSqlServer` tests. The fixture creates and deletes its own uniquely named temporary database.

## Remaining concurrency note

The automated suite verifies sequential retry/idempotency for Customer Demand and real SQL Server uniqueness/rowversion for lens variant details. A strict cross-process concurrent Customer Demand creation guarantee would require a database-level idempotency/locking strategy tied to active PurchaseRequest state; the current schema intentionally does not use a simple unique index because closed/cancelled historical demand must remain repeatable.

Customer Advance application currently uses RowVersion for optimistic concurrency, but ApplyCustomerAdvanceRequest has no operation/idempotency key. Therefore a true network-retry guarantee (same logical request cannot create a second application/journal after the client has refreshed state) cannot be proven without extending the production contract/persistence model with an idempotency identifier.
