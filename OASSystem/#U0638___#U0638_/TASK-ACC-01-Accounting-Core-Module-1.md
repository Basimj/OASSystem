# TASK-ACC-01 — Accounting Core Module

**النظام:** OAS — Optical & Accounting Management System  
**الموديول:** Accounting  
**نوع المهمة:** Full Module / Full Stack / Clean Architecture  
**المنصة:** .NET 9 / ASP.NET Core 9 / Blazor WebAssembly / EF Core / SQL Server  

---

# 1. الهدف

إنشاء موديول `Accounting` كوحدة مستقلة داخل النظام، بحيث تكون جميع ملفات الحسابات تحت مجلد `Accounting` مباشر في كل طبقة، ولا توضع داخل `Features`.

المهمة تشمل فقط:

- شجرة الحسابات.
- السنوات والفترات المالية.
- القيود المحاسبية.
- Posting Profiles.
- حسابات العملاء والموردين المحاسبية.
- سندات القبض.
- سندات الصرف.
- تخصيص الدفعات.
- الصندوق والبنوك.
- ورديات الصندوق.
- المصروفات.
- مراكز التكلفة كهيكل اختياري قابل للتعطيل.
- الصفحات والـAPI والعقود والبنية التحتية والاختبارات الخاصة بهذه الأجزاء.

لا تشمل هذه المهمة التقارير المالية، ميزان المراجعة، إقفال السنة، Bank Reconciliation، AR/AP Aging، أو القوائم المالية؛ يتم تنفيذها كمهام لاحقة فوق هذا الأساس.

---

# 2. قاعدة البيانات

**SQL Schema المعتمد:**

```text
accounting
```

---

## 2.1 `accounting.Accounts`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف الحساب |
| `Code` | `nvarchar(30)` | رقم الحساب |
| `NameAr` | `nvarchar(150)` | الاسم العربي |
| `NameEn` | `nvarchar(150) NULL` | الاسم الإنجليزي |
| `ParentAccountId` | `uniqueidentifier NULL` | الحساب الأب |
| `Level` | `tinyint` | مستوى الحساب |
| `AccountClass` | `tinyint` | تصنيف الحساب |
| `AccountType` | `tinyint` | نوع الحساب |
| `NormalBalance` | `tinyint` | طبيعة الرصيد |
| `IsPostingAccount` | `bit` | يقبل ترحيل |
| `IsControlAccount` | `bit` | حساب مراقبة |
| `AllowManualPosting` | `bit` | يسمح بقيد يدوي |
| `IsSystemAccount` | `bit` | حساب نظام |
| `IsActive` | `bit` | حالة الحساب |
| `EffectiveDate` | `date NULL` | تاريخ السريان |
| `RowVersion` | `rowversion` | التزامن |

### `AccountClass`

```text
Asset     = 1
Liability = 2
Equity    = 3
Revenue   = 4
Expense   = 5
```

### `AccountType`

```text
Header    = 1
Posting   = 2
Control   = 3
Subledger = 4
```

### `NormalBalance`

```text
Debit  = 1
Credit = 2
```

---

## 2.2 `accounting.FiscalYears`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف السنة |
| `Code` | `nvarchar(20)` | كود السنة |
| `Name` | `nvarchar(100)` | اسم السنة |
| `StartDate` | `date` | بداية السنة |
| `EndDate` | `date` | نهاية السنة |
| `Status` | `tinyint` | حالة السنة |
| `ClosedAtUtc` | `datetime2(3) NULL` | وقت الإقفال |
| `ClosedBy` | `uniqueidentifier NULL` | المستخدم المقفل |
| `RowVersion` | `rowversion` | التزامن |

### `FiscalYearStatus`

```text
Future  = 1
Open    = 2
Closing = 3
Closed  = 4
```

---

## 2.3 `accounting.FiscalPeriods`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف الفترة |
| `FiscalYearId` | `uniqueidentifier` | السنة المالية |
| `PeriodNumber` | `tinyint` | رقم الفترة |
| `Name` | `nvarchar(50)` | اسم الفترة |
| `StartDate` | `date` | البداية |
| `EndDate` | `date` | النهاية |
| `Status` | `tinyint` | حالة الفترة |
| `SalesLocked` | `bit` | إقفال المبيعات |
| `InventoryLocked` | `bit` | إقفال المخزون |
| `AccountingLocked` | `bit` | إقفال الحسابات |
| `ClosedAtUtc` | `datetime2(3) NULL` | وقت الإقفال |
| `ClosedBy` | `uniqueidentifier NULL` | المستخدم المقفل |
| `RowVersion` | `rowversion` | التزامن |

### `FiscalPeriodStatus`

```text
Open       = 1
SoftClosed = 2
Closed     = 3
```

---

## 2.4 `accounting.JournalEntries`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف القيد |
| `JournalNumber` | `nvarchar(40)` | رقم القيد |
| `JournalType` | `tinyint` | نوع القيد |
| `PostingDate` | `date` | تاريخ الترحيل |
| `DocumentDate` | `date` | تاريخ المستند |
| `FiscalPeriodId` | `uniqueidentifier` | الفترة المالية |
| `Description` | `nvarchar(500)` | البيان |
| `SourceModule` | `nvarchar(50) NULL` | الموديول المصدر |
| `SourceDocumentType` | `nvarchar(50) NULL` | نوع المستند |
| `SourceDocumentId` | `uniqueidentifier NULL` | معرف المستند |
| `Status` | `tinyint` | حالة القيد |
| `ReversedJournalId` | `uniqueidentifier NULL` | القيد المعكوس |
| `CreatedBy` | `uniqueidentifier` | المنشئ |
| `CreatedAtUtc` | `datetime2(3)` | وقت الإنشاء |
| `ApprovedBy` | `uniqueidentifier NULL` | المعتمد |
| `ApprovedAtUtc` | `datetime2(3) NULL` | وقت الاعتماد |
| `PostedBy` | `uniqueidentifier NULL` | المرحل |
| `PostedAtUtc` | `datetime2(3) NULL` | وقت الترحيل |
| `RowVersion` | `rowversion` | التزامن |

### `JournalType`

```text
Automatic = 1
Manual    = 2
Opening   = 3
Closing   = 4
Reversal  = 5
```

### `JournalEntryStatus`

```text
Draft           = 1
PendingApproval = 2
Approved        = 3
Posted          = 4
Reversed        = 5
```

---

## 2.5 `accounting.JournalEntryLines`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف السطر |
| `JournalEntryId` | `uniqueidentifier` | رأس القيد |
| `LineNumber` | `int` | رقم السطر |
| `AccountId` | `uniqueidentifier` | الحساب |
| `DebitAmount` | `decimal(19,4)` | مدين |
| `CreditAmount` | `decimal(19,4)` | دائن |
| `Description` | `nvarchar(300) NULL` | البيان |
| `CustomerId` | `uniqueidentifier NULL` | مرجع عميل |
| `SupplierId` | `uniqueidentifier NULL` | مرجع مورد |
| `CostCenterId` | `uniqueidentifier NULL` | مركز تكلفة |
| `ProductVariantId` | `uniqueidentifier NULL` | مرجع منتج |
| `WarehouseId` | `uniqueidentifier NULL` | مرجع مخزن |

---

## 2.6 `accounting.PostingProfiles`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف الملف |
| `Code` | `nvarchar(40)` | كود الملف |
| `Name` | `nvarchar(150)` | الاسم |
| `Module` | `nvarchar(50)` | الموديول |
| `DocumentType` | `nvarchar(50)` | نوع المستند |
| `IsActive` | `bit` | الحالة |
| `RowVersion` | `rowversion` | التزامن |

---

## 2.7 `accounting.PostingProfileLines`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف السطر |
| `PostingProfileId` | `uniqueidentifier` | ملف الترحيل |
| `AccountRole` | `nvarchar(50)` | دور الحساب |
| `AccountId` | `uniqueidentifier` | الحساب |
| `IsRequired` | `bit` | إلزامي |

### قيم `AccountRole` الأساسية

```text
Cash
Bank
ARControl
APControl
SalesRevenue
SalesReturn
SalesDiscount
Inventory
COGS
TaxPayable
InventoryGain
InventoryLoss
CustomerAdvances
```

---

## 2.8 `accounting.CostCenters`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف المركز |
| `Code` | `nvarchar(30)` | كود المركز |
| `NameAr` | `nvarchar(150)` | الاسم العربي |
| `NameEn` | `nvarchar(150) NULL` | الاسم الإنجليزي |
| `ParentCostCenterId` | `uniqueidentifier NULL` | المركز الأب |
| `IsActive` | `bit` | الحالة |
| `RowVersion` | `rowversion` | التزامن |

> مراكز التكلفة قابلة للتعطيل في V1، لكن الهيكل موجود من البداية.

---

## 2.9 `accounting.CustomerAccounts`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف الربط |
| `CustomerId` | `uniqueidentifier` | العميل |
| `AccountId` | `uniqueidentifier` | الحساب الفرعي |
| `ControlAccountId` | `uniqueidentifier` | حساب AR Control |
| `IsActive` | `bit` | الحالة |
| `CreatedAtUtc` | `datetime2(3)` | وقت الإنشاء |
| `RowVersion` | `rowversion` | التزامن |

---

## 2.10 `accounting.SupplierAccounts`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف الربط |
| `SupplierId` | `uniqueidentifier` | المورد |
| `AccountId` | `uniqueidentifier` | الحساب الفرعي |
| `ControlAccountId` | `uniqueidentifier` | حساب AP Control |
| `IsActive` | `bit` | الحالة |
| `CreatedAtUtc` | `datetime2(3)` | وقت الإنشاء |
| `RowVersion` | `rowversion` | التزامن |

---

## 2.11 `accounting.ReceiptVouchers`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف السند |
| `VoucherNumber` | `nvarchar(40)` | رقم السند |
| `VoucherDate` | `date` | التاريخ |
| `PartyType` | `tinyint` | نوع الطرف |
| `CustomerId` | `uniqueidentifier NULL` | العميل |
| `ReceivedFrom` | `nvarchar(200) NULL` | المستلم منه |
| `PaymentMethod` | `tinyint` | طريقة القبض |
| `CashAccountId` | `uniqueidentifier NULL` | الصندوق |
| `BankAccountId` | `uniqueidentifier NULL` | البنك |
| `TotalAmount` | `decimal(19,4)` | الإجمالي |
| `Status` | `tinyint` | الحالة |
| `Description` | `nvarchar(500) NULL` | البيان |
| `JournalEntryId` | `uniqueidentifier NULL` | القيد الناتج |
| `CreatedBy` | `uniqueidentifier` | المنشئ |
| `CreatedAtUtc` | `datetime2(3)` | وقت الإنشاء |
| `PostedBy` | `uniqueidentifier NULL` | المرحل |
| `PostedAtUtc` | `datetime2(3) NULL` | وقت الترحيل |
| `RowVersion` | `rowversion` | التزامن |

### `ReceiptPartyType`

```text
Customer = 1
Other    = 2
```

### `PaymentMethod`

```text
Cash         = 1
Card         = 2
BankTransfer = 3
Cheque       = 4
Other        = 5
```

### `ReceiptVoucherStatus`

```text
Draft     = 1
Approved  = 2
Posted    = 3
Cancelled = 4
```

---

## 2.12 `accounting.ReceiptVoucherLines`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف السطر |
| `ReceiptVoucherId` | `uniqueidentifier` | رأس السند |
| `LineNumber` | `int` | رقم السطر |
| `AccountId` | `uniqueidentifier` | الحساب المقابل |
| `Amount` | `decimal(19,4)` | المبلغ |
| `ReferenceType` | `nvarchar(50) NULL` | نوع المرجع |
| `ReferenceId` | `uniqueidentifier NULL` | معرف المرجع |
| `Description` | `nvarchar(300) NULL` | البيان |

---

## 2.13 `accounting.PaymentVouchers`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف السند |
| `VoucherNumber` | `nvarchar(40)` | رقم السند |
| `VoucherDate` | `date` | التاريخ |
| `PartyType` | `tinyint` | نوع المستفيد |
| `SupplierId` | `uniqueidentifier NULL` | المورد |
| `BeneficiaryName` | `nvarchar(200) NULL` | اسم المستفيد |
| `PaymentMethod` | `tinyint` | طريقة الدفع |
| `CashAccountId` | `uniqueidentifier NULL` | الصندوق |
| `BankAccountId` | `uniqueidentifier NULL` | البنك |
| `TotalAmount` | `decimal(19,4)` | الإجمالي |
| `Status` | `tinyint` | الحالة |
| `Description` | `nvarchar(500) NULL` | البيان |
| `JournalEntryId` | `uniqueidentifier NULL` | القيد الناتج |
| `CreatedBy` | `uniqueidentifier` | المنشئ |
| `CreatedAtUtc` | `datetime2(3)` | وقت الإنشاء |
| `PostedBy` | `uniqueidentifier NULL` | المرحل |
| `PostedAtUtc` | `datetime2(3) NULL` | وقت الترحيل |
| `RowVersion` | `rowversion` | التزامن |

### `PaymentPartyType`

```text
Supplier = 1
Employee = 2
Other    = 3
```

### `PaymentVoucherStatus`

```text
Draft     = 1
Approved  = 2
Posted    = 3
Cancelled = 4
```

---

## 2.14 `accounting.PaymentVoucherLines`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف السطر |
| `PaymentVoucherId` | `uniqueidentifier` | رأس السند |
| `LineNumber` | `int` | رقم السطر |
| `AccountId` | `uniqueidentifier` | الحساب المقابل |
| `Amount` | `decimal(19,4)` | المبلغ |
| `ReferenceType` | `nvarchar(50) NULL` | نوع المرجع |
| `ReferenceId` | `uniqueidentifier NULL` | معرف المرجع |
| `Description` | `nvarchar(300) NULL` | البيان |

---

## 2.15 `accounting.PaymentAllocations`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف التخصيص |
| `PaymentSourceType` | `tinyint` | مصدر الدفعة |
| `PaymentSourceId` | `uniqueidentifier` | معرف المصدر |
| `TargetDocumentType` | `tinyint` | المستند المستهدف |
| `TargetDocumentId` | `uniqueidentifier` | معرف المستند |
| `AllocatedAmount` | `decimal(19,4)` | المبلغ المخصص |
| `AllocatedAtUtc` | `datetime2(3)` | وقت التخصيص |
| `CreatedBy` | `uniqueidentifier` | المنشئ |

### `PaymentSourceType`

```text
ReceiptVoucher = 1
PaymentVoucher = 2
CustomerAdvance = 3
```

### `AllocationTargetDocumentType`

```text
SalesInvoice     = 1
PurchaseInvoice  = 2
DebitAdjustment  = 3
CreditAdjustment = 4
```

---

## 2.16 `accounting.CashAccounts`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف الصندوق |
| `Code` | `nvarchar(30)` | كود الصندوق |
| `Name` | `nvarchar(150)` | اسم الصندوق |
| `AccountId` | `uniqueidentifier` | حساب GL |
| `IsDefault` | `bit` | الصندوق الافتراضي |
| `IsActive` | `bit` | الحالة |
| `RowVersion` | `rowversion` | التزامن |

---

## 2.17 `accounting.BankAccounts`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف البنك |
| `Code` | `nvarchar(30)` | الكود |
| `BankName` | `nvarchar(150)` | اسم البنك |
| `AccountName` | `nvarchar(150)` | اسم الحساب |
| `AccountNumber` | `nvarchar(100)` | رقم الحساب |
| `IBAN` | `nvarchar(50) NULL` | IBAN |
| `AccountId` | `uniqueidentifier` | حساب GL |
| `IsActive` | `bit` | الحالة |
| `RowVersion` | `rowversion` | التزامن |

---

## 2.18 `accounting.CashShifts`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف الوردية |
| `ShiftNumber` | `nvarchar(40)` | رقم الوردية |
| `CashAccountId` | `uniqueidentifier` | الصندوق |
| `OpenedBy` | `uniqueidentifier` | فاتح الوردية |
| `OpenedAtUtc` | `datetime2(3)` | وقت الفتح |
| `OpeningBalance` | `decimal(19,4)` | رصيد البداية |
| `ExpectedClosingBalance` | `decimal(19,4) NULL` | الرصيد المتوقع |
| `ActualClosingBalance` | `decimal(19,4) NULL` | الرصيد الفعلي |
| `DifferenceAmount` | `decimal(19,4) NULL` | الفرق |
| `ClosedBy` | `uniqueidentifier NULL` | مغلق الوردية |
| `ClosedAtUtc` | `datetime2(3) NULL` | وقت الإغلاق |
| `Status` | `tinyint` | الحالة |
| `RowVersion` | `rowversion` | التزامن |

### `CashShiftStatus`

```text
Open     = 1
Closing  = 2
Closed   = 3
Approved = 4
```

---

## 2.19 `accounting.ExpenseTypes`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف النوع |
| `Code` | `nvarchar(30)` | الكود |
| `NameAr` | `nvarchar(150)` | الاسم العربي |
| `NameEn` | `nvarchar(150) NULL` | الاسم الإنجليزي |
| `DefaultExpenseAccountId` | `uniqueidentifier NULL` | الحساب الافتراضي |
| `IsActive` | `bit` | الحالة |
| `RowVersion` | `rowversion` | التزامن |

---

## 2.20 `accounting.Expenses`

| العمود | النوع | الوصف المختصر |
|---|---|---|
| `Id` | `uniqueidentifier` | معرف المصروف |
| `ExpenseNumber` | `nvarchar(40)` | رقم المصروف |
| `ExpenseDate` | `date` | التاريخ |
| `ExpenseTypeId` | `uniqueidentifier` | نوع المصروف |
| `ExpenseAccountId` | `uniqueidentifier` | حساب المصروف |
| `Beneficiary` | `nvarchar(200) NULL` | المستفيد |
| `Amount` | `decimal(19,4)` | المبلغ |
| `PaymentMethod` | `tinyint` | طريقة الدفع |
| `CashAccountId` | `uniqueidentifier NULL` | الصندوق |
| `BankAccountId` | `uniqueidentifier NULL` | البنك |
| `Description` | `nvarchar(500) NULL` | البيان |
| `Status` | `tinyint` | الحالة |
| `JournalEntryId` | `uniqueidentifier NULL` | القيد الناتج |
| `CreatedBy` | `uniqueidentifier` | المنشئ |
| `CreatedAtUtc` | `datetime2(3)` | وقت الإنشاء |
| `PostedAtUtc` | `datetime2(3) NULL` | وقت الترحيل |
| `RowVersion` | `rowversion` | التزامن |

### `ExpenseStatus`

```text
Draft     = 1
Approved  = 2
Posted    = 3
Cancelled = 4
```

---

# 3. العلاقات بين الجداول

```text
Accounts.ParentAccountId
→ Accounts.Id

CostCenters.ParentCostCenterId
→ CostCenters.Id

FiscalPeriods.FiscalYearId
→ FiscalYears.Id

JournalEntries.FiscalPeriodId
→ FiscalPeriods.Id

JournalEntries.ReversedJournalId
→ JournalEntries.Id

JournalEntryLines.JournalEntryId
→ JournalEntries.Id

JournalEntryLines.AccountId
→ Accounts.Id

JournalEntryLines.CostCenterId
→ CostCenters.Id

PostingProfileLines.PostingProfileId
→ PostingProfiles.Id

PostingProfileLines.AccountId
→ Accounts.Id

CustomerAccounts.AccountId
→ Accounts.Id

CustomerAccounts.ControlAccountId
→ Accounts.Id

SupplierAccounts.AccountId
→ Accounts.Id

SupplierAccounts.ControlAccountId
→ Accounts.Id

ReceiptVoucherLines.ReceiptVoucherId
→ ReceiptVouchers.Id

ReceiptVoucherLines.AccountId
→ Accounts.Id

ReceiptVouchers.CashAccountId
→ CashAccounts.Id

ReceiptVouchers.BankAccountId
→ BankAccounts.Id

ReceiptVouchers.JournalEntryId
→ JournalEntries.Id

PaymentVoucherLines.PaymentVoucherId
→ PaymentVouchers.Id

PaymentVoucherLines.AccountId
→ Accounts.Id

PaymentVouchers.CashAccountId
→ CashAccounts.Id

PaymentVouchers.BankAccountId
→ BankAccounts.Id

PaymentVouchers.JournalEntryId
→ JournalEntries.Id

CashAccounts.AccountId
→ Accounts.Id

BankAccounts.AccountId
→ Accounts.Id

CashShifts.CashAccountId
→ CashAccounts.Id

ExpenseTypes.DefaultExpenseAccountId
→ Accounts.Id

Expenses.ExpenseTypeId
→ ExpenseTypes.Id

Expenses.ExpenseAccountId
→ Accounts.Id

Expenses.CashAccountId
→ CashAccounts.Id

Expenses.BankAccountId
→ BankAccounts.Id

Expenses.JournalEntryId
→ JournalEntries.Id
```

---

# 4. العلاقات مع الموديولات الأخرى

هذه العلاقات **مرجعية فقط**. لا يجوز لـAccounting تعديل بيانات الموديول الآخر مباشرة.

```text
CustomerAccounts.CustomerId
→ Customers Module / Customer.Id

SupplierAccounts.SupplierId
→ Suppliers/Purchasing Module / Supplier.Id

JournalEntryLines.CustomerId
→ Customers Module / Customer.Id

JournalEntryLines.SupplierId
→ Suppliers Module / Supplier.Id

JournalEntryLines.ProductVariantId
→ Catalog Module / ProductVariant.Id

JournalEntryLines.WarehouseId
→ Inventory Module / Warehouse.Id

PaymentAllocations.TargetDocumentId
→ Sales / Purchasing حسب TargetDocumentType

JournalEntries.SourceDocumentId
→ المستند المصدر في Sales / Purchasing / Inventory / Expenses / Cash
```

**قاعدة الربط:**

- لا يتم استدعاء Repository من موديول خارجي.
- لا يتم تعديل Entity خارج `Accounting` من داخل Accounting.
- التكامل بين الموديولات يتم عن طريق `Contracts / Events / Application Ports` فقط.
- إذا لم يكن جدول الموديول الخارجي موجودًا وقت تنفيذ Accounting، يحتفظ Accounting بالـ`Guid` كمرجع ولا ينشئ جدولًا بديلًا.

---

# 5. القيود الأساسية

```text
Account.Code                    UNIQUE
FiscalYear.Code                 UNIQUE
JournalEntry.JournalNumber      UNIQUE
PostingProfile.Code             UNIQUE
ReceiptVoucher.VoucherNumber    UNIQUE
PaymentVoucher.VoucherNumber    UNIQUE
CashAccount.Code                UNIQUE
BankAccount.Code                UNIQUE
CashShift.ShiftNumber           UNIQUE
ExpenseType.Code                UNIQUE
Expense.ExpenseNumber           UNIQUE
CustomerAccounts.CustomerId     UNIQUE
SupplierAccounts.SupplierId     UNIQUE
```

ويجب تطبيق القواعد التالية:

```text
JournalEntry Total Debit = Total Credit
DebitAmount >= 0
CreditAmount >= 0
السطر لا يكون Debit وCredit معًا
السطر يجب أن يحتوي Debit أو Credit بقيمة أكبر من صفر
ممنوع Posting إلى Header Account
ممنوع Posting إلى Account غير فعال
ممنوع Manual Posting إلى حساب AllowManualPosting = false
ممنوع Posting داخل FiscalPeriod مغلقة
القيد Posted لا يعدل ولا يحذف
تصحيح Posted Journal يتم بـ Reversal
TotalAmount > 0 في سند القبض والصرف والمصروف
AllocatedAmount > 0
مجموع PaymentAllocations لا يتجاوز قيمة الدفعة المتاحة
```

---

# 6. الصفحات المطلوبة

| الصفحة | Route مقترح | الوظيفة |
|---|---|---|
| `AccountingHomePage` | `/accounting` | مدخل موديول الحسابات |
| `ChartOfAccountsPage` | `/accounting/accounts` | إدارة شجرة الحسابات |
| `FiscalYearsPage` | `/accounting/fiscal-years` | إدارة السنوات والفترات |
| `JournalEntriesPage` | `/accounting/journals` | عرض وإنشاء القيود |
| `PostingProfilesPage` | `/accounting/posting-profiles` | ربط العمليات بالحسابات |
| `CustomerAccountsPage` | `/accounting/customer-accounts` | ربط العملاء بحساباتهم |
| `SupplierAccountsPage` | `/accounting/supplier-accounts` | ربط الموردين بحساباتهم |
| `ReceiptVouchersPage` | `/accounting/receipt-vouchers` | سندات القبض |
| `PaymentVouchersPage` | `/accounting/payment-vouchers` | سندات الصرف |
| `CashBankAccountsPage` | `/accounting/cash-bank` | الصناديق والحسابات البنكية |
| `CashShiftsPage` | `/accounting/cash-shifts` | فتح وإغلاق ورديات الصندوق |
| `ExpensesPage` | `/accounting/expenses` | إدارة المصروفات |
| `CostCentersPage` | `/accounting/cost-centers` | إدارة مراكز التكلفة عند تفعيلها |

---

# 7. هيكل الموديول داخل الطبقات

```text
OAS.Domain/
└── Accounting/
    ├── Entities/
    │   ├── Account.cs
    │   ├── FiscalYear.cs
    │   ├── FiscalPeriod.cs
    │   ├── JournalEntry.cs
    │   ├── JournalEntryLine.cs
    │   ├── PostingProfile.cs
    │   ├── PostingProfileLine.cs
    │   ├── CostCenter.cs
    │   ├── CustomerAccount.cs
    │   ├── SupplierAccount.cs
    │   ├── ReceiptVoucher.cs
    │   ├── ReceiptVoucherLine.cs
    │   ├── PaymentVoucher.cs
    │   ├── PaymentVoucherLine.cs
    │   ├── PaymentAllocation.cs
    │   ├── CashAccount.cs
    │   ├── BankAccount.cs
    │   ├── CashShift.cs
    │   ├── ExpenseType.cs
    │   └── Expense.cs
    ├── Enums/
    ├── ValueObjects/
    ├── Rules/
    └── Events/

OAS.Contracts/
└── Accounting/
    ├── Common/
    ├── Enums/
    ├── Accounts/
    ├── FiscalYears/
    ├── FiscalPeriods/
    ├── Journals/
    ├── PostingProfiles/
    ├── CostCenters/
    ├── CustomerAccounts/
    ├── SupplierAccounts/
    ├── ReceiptVouchers/
    ├── PaymentVouchers/
    ├── PaymentAllocations/
    ├── CashAccounts/
    ├── BankAccounts/
    ├── CashShifts/
    └── Expenses/

OAS.Application/
└── Accounting/
    ├── Authorization/
    ├── Abstractions/
    ├── Accounts/
    │   ├── Commands/
    │   ├── Queries/
    │   ├── Mapping/
    │   └── Specifications/
    ├── FiscalYears/
    ├── FiscalPeriods/
    ├── Journals/
    ├── PostingProfiles/
    ├── CostCenters/
    ├── CustomerAccounts/
    ├── SupplierAccounts/
    ├── ReceiptVouchers/
    ├── PaymentVouchers/
    ├── PaymentAllocations/
    ├── CashAccounts/
    ├── BankAccounts/
    ├── CashShifts/
    └── Expenses/

OAS.Infrastructure/
└── Accounting/
    ├── Persistence/
    │   ├── Configurations/
    │   ├── Repositories/
    │   └── Queries/
    ├── Posting/
    ├── Numbering/
    └── Services/

OAS.API/
└── Accounting/
    └── Controllers/
        ├── AccountsController.cs
        ├── FiscalYearsController.cs
        ├── FiscalPeriodsController.cs
        ├── JournalEntriesController.cs
        ├── PostingProfilesController.cs
        ├── CostCentersController.cs
        ├── CustomerAccountsController.cs
        ├── SupplierAccountsController.cs
        ├── ReceiptVouchersController.cs
        ├── PaymentVouchersController.cs
        ├── CashAccountsController.cs
        ├── BankAccountsController.cs
        ├── CashShiftsController.cs
        └── ExpensesController.cs

OAS.Client/
└── Accounting/
    ├── Pages/
    │   ├── AccountingHomePage.razor
    │   ├── ChartOfAccountsPage.razor
    │   ├── FiscalYearsPage.razor
    │   ├── JournalEntriesPage.razor
    │   ├── PostingProfilesPage.razor
    │   ├── CostCentersPage.razor
    │   ├── CustomerAccountsPage.razor
    │   ├── SupplierAccountsPage.razor
    │   ├── ReceiptVouchersPage.razor
    │   ├── PaymentVouchersPage.razor
    │   ├── CashBankAccountsPage.razor
    │   ├── CashShiftsPage.razor
    │   └── ExpensesPage.razor
    ├── Services/
    ├── State/
    ├── Mapping/
    └── Workspace/

Shared/UiLib/
└── Components/
    └── Accounting/
        ├── Workspace/
        ├── Accounts/
        ├── Fiscal/
        ├── Journals/
        ├── PostingProfiles/
        ├── CostCenters/
        ├── Parties/
        ├── Vouchers/
        ├── CashBank/
        └── Expenses/

Shared/UiLib/wwwroot/css/features/
└── accounting.css

OAS.Tests/
└── Accounting/
    ├── Domain/
    ├── Application/
    ├── Infrastructure/
    ├── API/
    └── Integration/
```

**ممنوع إنشاء:**

```text
OAS.Client/Features/Accounting
OAS.Domain/Features/Accounting
OAS.Application/Features/Accounting
```

`Accounting` موديول مباشر ومستقل في كل طبقة.

---

# 8. قواعد الـEnums

أي `enum` يستخدم داخل الـDomain ويظهر في API/Client يجب أن يوجد بنسخة Contract مستقلة داخل:

```text
OAS.Domain/Accounting/Enums/
OAS.Contracts/Accounting/Enums/
```

ويجب أن يكون:

- **نفس اسم الـEnum.**
- **نفس أسماء القيم.**
- **نفس القيمة الرقمية لكل عنصر.**
- **نفس الترتيب.**
- القيم الرقمية تكتب صراحة ولا تعتمد على الترقيم التلقائي.
- بعد اعتماد Enum لا يجوز إعادة ترتيب قيمه أو إعادة استخدام قيمة رقمية قديمة لمعنى آخر.

مثال إلزامي:

```csharp
// Domain
public enum JournalEntryStatus : byte
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Posted = 4,
    Reversed = 5
}
```

```csharp
// Contracts
public enum JournalEntryStatus : byte
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Posted = 4,
    Reversed = 5
}
```

الـ`Contracts` لا تشير إلى Enum من `Domain`، والـ`Domain` لا تشير إلى `Contracts`.

---

# 9. قواعد الـUI الإلزامية

1. يجب إعادة استخدام ووراثة/تركيب مكونات `OAS.UiLib` الحالية قدر الإمكان، وعدم إعادة بناء Button/Input/Grid/Dialog/Layout موجود أصلًا.
2. أي HTML أو CSS جديد خاص بواجهات Accounting يجب أن يكون داخل `Shared/UiLib`.
3. `OAS.Client/Accounting` مسؤول عن Routing وState واستدعاء API وتحويل Contracts إلى UI Models فقط.
4. صفحات الـClient يجب أن تستدعي مكونات `UiLib` ولا ترسم Raw HTML.
5. ممنوع داخل `OAS.Client`:

```text
<div>
<span>
<section>
<form>
<input>
<button>
<table>
<style>
inline CSS
*.razor.css
```

6. يجب استمرار نجاح:

```text
OAS.Tests/Architecture/ClientUiPolicyTests.cs
```

7. `OAS.UiLib` لا تعتمد على:

```text
OAS.Domain
OAS.Contracts
OAS.Application
OAS.Infrastructure
OAS.API
OAS.Client
```

8. مكونات Accounting داخل UiLib تكون Presentation-only وتستقبل Primitives أو UI Models محلية وتصدر `EventCallback`.
9. لا تستدعي UiLib أي API ولا تحتوي Business Logic.
10. الـClient يحول DTOs القادمة من `OAS.Contracts` إلى UI Models عند الحاجة.

---

# 10. قواعد Clean Architecture

يجب احترام القيود الحالية للمشروع:

```text
Domain
  ↑
Application
  ↑
Infrastructure
  ↑
API

Contracts ← Client
UiLib     ← Client
```

ممنوع:

```text
Domain         → Contracts / Application / Infrastructure / API / Client
Contracts      → Domain / Application / Infrastructure / API / Client
Application    → Infrastructure / API / Client / UiLib
Infrastructure → API / Client / UiLib
Client         → Domain / Application / Infrastructure
UiLib          → Domain / Contracts / Application / Infrastructure / API / Client
```

ويجب استمرار نجاح:

```text
OAS.Tests/Architecture/DependencyTests.cs
OAS.Tests/Architecture/ClientUiPolicyTests.cs
```

---

# 11. قواعد الوراثة وإعادة الاستخدام

- كل Entity تستخدم Base Entity الموجودة في المشروع عندما يكون ذلك مناسبًا.
- كل Entity تحتاج Audit تستخدم Base Auditable Entity الموجودة بدل تكرار خصائص وسلوك Audit في الكود.
- تستخدم Repository/Specification/Transaction abstractions الموجودة بدل إنشاء Infrastructure pattern موازٍ بدون سبب.
- تستخدم `IUnitOfWork` والـTransaction pipeline الحالي للعمليات التي تغير أكثر من سجل.
- تستخدم Validation/Authorization/MediatR patterns الحالية.
- لا يتم إنشاء Generic CRUD جديد إذا كان الموجود يلبي الحاجة.
- القيود المحاسبية المهمة يجب أن تكون Domain Rules وليست فقط Validation في الواجهة.
- `Posted Journal` وعمليات الترحيل ليست CRUD عاديًا؛ يجب أن يكون لها Commands واضحة وسلوك Domain واضح.

---

# 12. قواعد Migration

**ممنوع استخدام:**

```text
dotnet ef migrations add
Add-Migration
```

Migration الخاصة بـAccounting تكتب يدويًا بنفس النمط الموجود حاليًا في المشروع.

المسار:

```text
OAS.Infrastructure/Persistence/Migrations/
```

وتكون Migration class عادية مسجلة على `OasDbContext`، لكن إنشاء/تعديل قاعدة البيانات يتم باستخدام **Raw SQL فقط** داخل:

```csharp
migrationBuilder.Sql("""
...
""");
```

المطلوب في `Up`:

- إنشاء Schema `accounting` إذا لم يكن موجودًا.
- إنشاء الجداول بواسطة `CREATE TABLE` Raw SQL.
- إنشاء PK/FK/Unique/Check Constraints بواسطة Raw SQL.
- إنشاء Indexes بواسطة Raw SQL.
- استخدام Guards مثل `SCHEMA_ID`, `OBJECT_ID`, `sys.indexes` لمنع إنشاء العنصر إذا كان موجودًا.

المطلوب في `Down`:

- إسقاط العلاقات بالترتيب الصحيح.
- إسقاط الجداول التابعة قبل الرئيسية.
- استخدام Raw SQL فقط.

**ممنوع داخل Migration:**

```text
migrationBuilder.CreateTable(...)
migrationBuilder.AddColumn(...)
migrationBuilder.CreateIndex(...)
```

يجب أن تتوافق EF Configurations مع نفس أسماء الجداول والأعمدة والـConstraints الموجودة في Raw SQL.

لا يتم تعديل Migration قديمة خاصة بموديول آخر.

---

# 13. قواعد EF Core

- كل Entity لها `IEntityTypeConfiguration<T>` مستقلة تحت:

```text
OAS.Infrastructure/Accounting/Persistence/Configurations/
```

- يعتمد التسجيل على:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(OasDbContext).Assembly);
```

- لا تضف `DbSet` إلى `OasDbContext` لمجرد التسجيل إذا لم توجد حاجة فعلية؛ استخدم `Set<T>()`/Repositories لتقليل تعديل الملف المشترك.
- أسماء Schema/Table/Column يجب أن تطابق هذا الملف حرفيًا.
- `rowversion` يضبط كـConcurrency Token.
- العلاقات الاختيارية لا تتحول إلى Cascade Delete غير مقصود.
- الحساب أو القيد التاريخي المستخدم لا يحذف Cascade.

---

# 14. قواعد التنفيذ المالي

- لا ينشئ أي موديول خارجي Journal Lines مباشرة.
- الـPosting يدخل إلى Accounting عبر Contract/Application Port معتمد.
- `JournalEntries` هو المصدر المحاسبي الرسمي.
- `JournalEntryLines` هي المصدر الذي يبنى عليه General Ledger لاحقًا.
- سند القبض وسند الصرف والمصروف يحتفظ كل منها بمرجع `JournalEntryId` بعد الترحيل.
- المستند `Posted` لا يعدل مباشرة.
- الإلغاء المالي بعد الترحيل يتم عبر Reversal/Correction وليس حذف البيانات التاريخية.
- لا يتم تكرار Customers/Suppliers/Products/Warehouses داخل Accounting.

---

# 15. Definition of Done

تعتبر المهمة مكتملة فقط عندما:

```text
[ ] تم إنشاء مجلد Accounting مباشر في جميع الطبقات المطلوبة.
[ ] تم إنشاء جميع الـEntities المذكورة في المهمة.
[ ] تم إنشاء جميع Enums مع تطابق Domain وContracts اسمًا وقيمًا وترتيبًا.
[ ] تم إنشاء EF Configurations لكل Entity.
[ ] تم إنشاء Raw SQL Migration يدويًا بدون Add-Migration.
[ ] تم إنشاء جميع الجداول والعلاقات المذكورة في المهمة.
[ ] تم إنشاء Contracts وCommands وQueries اللازمة للصفحات.
[ ] تم إنشاء Controllers داخل OAS.API/Accounting.
[ ] تم إنشاء الصفحات تحت OAS.Client/Accounting مباشرة.
[ ] لا يوجد Raw HTML أو CSS خاص بـAccounting داخل OAS.Client.
[ ] كل تصميم Accounting الجديد موجود داخل OAS.UiLib ويعاد استخدام المكونات الحالية قدر الإمكان.
[ ] لا تعتمد UiLib على Contracts أو Domain.
[ ] لا توجد مخالفة لحدود Clean Architecture.
[ ] Journal Debit = Credit قبل Posting.
[ ] لا يسمح بالترحيل إلى فترة مغلقة أو حساب غير صالح للترحيل.
[ ] القيد Posted غير قابل للتعديل/الحذف المباشر.
[ ] تم تطبيق RowVersion/Concurrency حيث هو محدد.
[ ] اختبارات Domain/Application/Integration الأساسية موجودة.
[ ] DependencyTests ناجحة.
[ ] ClientUiPolicyTests ناجحة.
```

