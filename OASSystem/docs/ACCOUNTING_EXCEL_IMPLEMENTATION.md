# تنفيذ Excel للمحاسبة — OASSystem

## النسخة الأساسية

تم العمل على **OASSystem(6).zip**، وهو الملف المرفق المتاح في هذه المحادثة. لم تكن النسخة (5) مرفقة.

## المنفّذ

- Import + Template + Export: Accounts, Cost Centers, Cash Accounts, Bank Accounts, Expense Types, Posting Profiles, Journals.
- Export فقط: Fiscal Years, Fiscal Periods, Receipt Vouchers, Payment Vouchers, Payment Allocations, Expenses, Cash Shifts.
- لا توجد أزرار أو قوالب أو مسارات Excel للعملاء أو الموردين. قائمة الأقسام المسموحة على الخادم تستبعدهما أيضاً.
- محرك عام `ISpreadsheetWorkbook` وتعريفات أوراق وأعمدة قابلة لإعادة الاستخدام، باستخدام ClosedXML الموجودة دون إضافة حزمة جديدة.
- يستخدم تصدير Employees المحرك المشترك؛ بقيت أسماء الأعمدة العربية والأكواد النصية وتاريخ التوظيف وخيارات نعم/لا كما هي، مع اختبار توافق. تنسيق رأس الملف مشترك مع قالب Employees.
- اختيار الملف ثم المعاينة والتحقق حسب رقم الصف والورقة، ثم تأكيد مستقل ونتيجة. يعاد التحقق على الخادم عند التأكيد. لا يحفظ أي سجل إذا وجد صف غير صالح. الحفظ ضمن معاملة واحدة باستخدام IUnitOfWork ومسار الأوامر الحالي.
- التحقق من التكرارات داخل الملف وفي قاعدة البيانات، المراجع، أنواع القيم، الحقول المطلوبة، validators الحالية، ودورات شجرة الحسابات ومراكز التكلفة. دعم الأب قبل الابن وبعده داخل الملف وحساب Level آلياً.
- PostingProfiles/Lines في ورقتين مرتبطتين بـ ProfileCode، وحل AccountCode إلى الحساب. أسماء الأدوار مطلوبة وغير مكررة، مع نقل IsRequired. المشروع الحالي لا يحتوي قاموس أدوار إلزامية حسب Module/DocumentType؛ لم تُخترع قائمة أدوار جديدة.
- القيود تجمع عبر JournalKey، مع تطابق معلومات الرأس، توازن المدين والدائن، صلاحية الحساب والفترة ومركز التكلفة ودقة المبالغ. تستخدم CreateJournalEntryCommand ومولّد الأرقام الحالي؛ Draft فقط، ولا تُستورد الأنواع Automatic أو Reversal.
- التصدير يمر على كل صفحات الاستعلام الحالي بدلاً من الاكتفاء بـ25 سجلاً، ويحترم Search ونوع سند تخصيص الدفعة. تصدير تفاصيل القيود وملفات الترحيل وأسطر السندات أيضاً.
- الأكواد والمراجع المحاسبية الرئيسية نصوص بدلاً من GUID، والأرقام البنكية تحتفظ بالأصفار البادئة، والمبالغ خلايا رقمية. بعض مراجع التدقيق والمستندات الخارجية التي لا يوفّر النظام لها كوداً تبقى معرفاتها في التصدير.
- نقل تقسيم شجرة الحسابات إلى UiAccountingSplitView، وحذف CSS من Client ونقله إلى accounting.css في UiLib. نسبة 28/72 ومتجاوب. تحميل كامل نتائج الشجرة، مع الحفاظ على حماية null والدورات الموجودة سابقاً والتبويبات والمسارات المنفصلة.
- معالجة ملفات xlsx غير الصالحة ضمن validation منظم، وحماية حجم الرفع (10 MB) والمحتوى المفكوك (100 MB) وعدد الصفوف (10000) ورفض الصيغ في الاستيراد.

## ملاحظات تشغيل

- طبّق migration الجديدة `20260921180000_BankAccountNumberUnique` مع آلية تحديث قاعدة البيانات المعتادة. تضيف Unique Index لرقم الحساب البنكي، وتحوله طبقة UnitOfWork إلى Conflict/409 عند السباق. إذا وجدت أرقام بنكية مكررة مسبقاً يتوقف الترحيل برسالة واضحة دون حذف أو تغيير بيانات.
- قوالب الاستيراد تولّد وقت الطلب ولا تحتوي GUID أو ملفات Excel ثابتة. توجد ورقة Instructions وقوائم منسدلة للقيم الثابتة. لا توجد بيانات نموذجية قابلة للاستيراد بالخطأ، بما يتوافق مع قالب Employees الحالي.
- أزرار الفترات وأنواع المصروفات والتخصيصات داخل قسمها الفرعي في الصفحات المشتركة.

## التحقق الفعلي

بيئة الاختبار Linux، SDK .NET 9.0.318.

| التحقق | النتيجة |
|---|---|
| `dotnet build OASSystem.sln -m:1 --no-restore` | ناجح، 0 أخطاء؛ سجل البناء النهائي يتضمن 10 تحذيرات |
| اختبارات Accounting | 161 ناجح، 0 فاشل |
| اختبار DependencyInjectionValidation الإضافي | 1 ناجح |
| Architecture + AccountingUiPolicy | 7 ناجح، 2 فاشل قديمان خارج Accounting |
| سياسة Accounting Client الخاصة | ناجح: لا ملفات CSS ولا HTML خام |

استُخدم `-m:1` لأن البناء المتوازي الافتراضي لم يكتمل في بيئة التنفيذ. تم استرجاع الحزم وبناء الحل كاملاً بنجاح. أوامر إعادة الاختبار:

```sh
dotnet build OASSystem.sln -m:1
dotnet test OAS.Tests/OAS.Tests.csproj --no-build --filter 'FullyQualifiedName~OAS.Tests.Accounting'
dotnet test OAS.Tests/OAS.Tests.csproj --no-build --filter 'FullyQualifiedName~OAS.Tests.Architecture|FullyQualifiedName~AccountingUiPolicyTests'
```

الاختباران العامان الفاشلان هما `ClientMustNotContainComponentCssFiles` و`ClientRazorFilesMustNotRenderRawHtmlElements`. المخالفات في Inventory وIdentity وEmployees موجودة في المصدر المرفق؛ تم التأكد أن ملفات Razor العشرة المخالفة خارج Accounting لم تتغير. لم يتم تغيير الاختبارين لتجاوز السياسة.

توجد سجلات البناء والاختبارات وملفات TRX داخل `docs/validation-accounting-excel/`.

## حدود التحقق

- لم يتم تشغيل ترحيل قاعدة البيانات على SQL Server فعلي، أو اختبار rollback/سباق uniqueness على قاعدة حية.
- لم يتم إجراء اختبار بصري تفاعلي بمتصفح متصل بقاعدة بيانات. اختبارات UI هنا بنيوية، إلى جانب نجاح تجميع Razor.
- ZIP يحتوي المشروع ومصادره وأصوله كاملة؛ استُبعدت bin/obj/.vs ونتائج البناء المؤقتة. يمكن إعادة توليدها بالبناء.

## الملفات المضافة

- `OAS.API/Accounting/Controllers/AccountingSpreadsheetsController.cs`
- `OAS.Application/Accounting/Spreadsheets/AccountingSpreadsheetDefinitions.cs`
- `OAS.Application/Accounting/Spreadsheets/AccountingSpreadsheetExport.cs`
- `OAS.Application/Accounting/Spreadsheets/AccountingSpreadsheetService.cs`
- `OAS.Application/Spreadsheets/ISpreadsheetWorkbook.cs`
- `OAS.Client/Accounting/Components/AccountingWorkspaceHost.Excel.cs`
- `OAS.Client/Accounting/Services/AccountingSpreadsheetClient.cs`
- `OAS.Contracts/Spreadsheets/SpreadsheetPreview.cs`
- `OAS.Infrastructure/Persistence/Migrations/20260921180000_BankAccountNumberUnique.cs`
- `OAS.Infrastructure/Spreadsheets/ClosedXmlSpreadsheetWorkbook.cs`
- `OAS.Infrastructure/Spreadsheets/SpreadsheetWorkbookStyle.cs`
- `OAS.Tests/Accounting/Client/AccountingUiPolicyTests.cs`
- `OAS.Tests/Accounting/Spreadsheets/AccountingSpreadsheetTests.cs`
- `Shared/UiLib/Components/Accounting/Workspace/UiAccountingSplitView.razor`
- `Shared/UiLib/Components/Spreadsheets/UiSpreadsheetActions.razor`
- `Shared/UiLib/Components/Spreadsheets/UiSpreadsheetImport.razor`
- `Shared/UiLib/Components/Spreadsheets/UiSpreadsheetImport.razor.css`
- `Shared/UiLib/Components/Spreadsheets/UiSpreadsheetModels.cs`

## الملفات المعدّلة

- `OAS.Application/DependencyInjection.cs`
- `OAS.Client/Accounting/Components/AccountingWorkspaceHost.razor`
- `OAS.Client/Accounting/Components/AccountingWorkspaceHost.razor.cs`
- `OAS.Client/Services/ClientServices.cs`
- `OAS.Infrastructure/Accounting/Persistence/Configurations/BankAccountConfiguration.cs`
- `OAS.Infrastructure/DependencyInjection.cs`
- `OAS.Infrastructure/Features/Employees/Export/EmployeeExcelExporter.cs`
- `OAS.Infrastructure/Features/Employees/Import/EmployeeExcelTemplateGenerator.cs`
- `OAS.Infrastructure/Persistence/EfUnitOfWork.cs`
- `Shared/UiLib/wwwroot/css/features/accounting.css`

## الملف المحذوف

- `OAS.Client/Accounting/Components/AccountingWorkspaceHost.razor.css`
