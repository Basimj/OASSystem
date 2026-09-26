# Accounting Multi-Currency rollout

The implementation intentionally uses an **Expand → Configure → Verify → Contract** rollout.

## 1. Expand

Migration `20260926100000_AccountingMultiCurrencyExpand` creates currencies, exchange rates,
accounting settings and employee-account mappings, and adds nullable multi-currency fields to
cash/bank, vouchers, journals and payment allocations. It does **not** drop legacy financial fields.

## 2. Configure

After applying Expand:

1. Create at least one active currency from **Accounting → Currencies**.
2. Open **Accounting → Settings** and select the base currency.
3. Configure the parent control accounts for employee, cash and bank linked accounts.
4. Add exchange rates for active foreign currencies.
5. Assign a currency to every legacy cash/bank account before using it in new settlement lines.

New cash/bank records create their linked GL account automatically under the configured parent.
Employee GL accounts are created only when an employee is activated for accounting.

## 3. Verify old data

Run `scripts/accounting/multi-currency-preflight.sql` against a backup/staging copy first.
Do not blindly treat historical `ReceiptVoucherLine.AccountId` / `PaymentVoucherLine.AccountId`
as the counterparty account: those legacy lines represented accounting distributions.

Legacy vouchers and journals remain readable/postable through compatibility paths during Expand.

## 4. Backfill

Only unambiguous historical records should be backfilled to base currency (`ExchangeRate = 1`).
Any `legacy_*_ambiguous_distribution` result requires a business decision before conversion.

## 5. Contract

Do not add/apply the destructive Contract migration until:

- Base currency is configured.
- Cash/Bank CurrencyId is populated.
- All required BaseAmount/BaseTotal snapshots are populated.
- All typed-party mappings are resolved.
- Every journal remains balanced.
- The preflight report contains no unresolved issues.

At that point a separate Contract migration may make new fields NOT NULL and remove obsolete
voucher-header fields. Keeping Contract out of the automatic Expand deployment prevents data loss.

## EF model snapshot note

This repository historically uses hand-authored SQL migrations for Accounting/Inventory while the
checked-in `OasDbContextModelSnapshot` does not represent those modules completely. The new Expand
migration follows the existing hand-authored convention. Do not generate a normal EF diff migration
for Accounting/Inventory until the snapshot is deliberately baselined against the production schema.
