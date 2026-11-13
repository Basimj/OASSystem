# إصلاح أعطال فلترة متابعة طلبات العملاء وتحسين التمرير والترقيم

**التاريخ:** 2026-11-13  
**الصفحة:** `/sales/order-tracking`  
**أساس العمل:** `OASSystem_Order_Tracking_Full_Project_2026-11-13.zip`.

## 1. السبب الجذري للاستثناء `System.InvalidCastException`

كان استعلام `CustomerOrderOperationsQueryService.BuildBaseQuery` يستخدم مقارنة بين `CustomerOrderStatus` الموجودة في عقود API وبين `CustomerOrderStatus` التابعة لكيان `Domain` بعد تحويلهما إلى `byte` داخل تعبير LINQ. في حالة تشغيل EF Core مع `ValueConverter` للحالة، حاول مزود SQL Server تحويل قيمة بارامتر من تعداد `Contracts` إلى تعداد `Domain` مباشرة وفشل في `GetPageAsync` عند `LongCountAsync`.

**الإصلاح:** إجراء التحويل الصريح إلى تعداد `Domain` في C# قبل إنشاء تعبير LINQ، وتطبيق الشرط على كيان `CustomerOrder` مباشرة قبل عمليات الإسقاط والربط:

```csharp
if (request.Status is { } contractStatus)
{
    var domainStatus = (OAS.Domain.Sales.Enums.CustomerOrderStatus)(byte)contractStatus;
    orders = orders.Where(order => order.Status == domainStatus);
}
```

بذلك لا يدخل تعداد `Contracts` إلى محوّل القيم الخاص بالحالة في استعلام EF Core. بقية الفلاتر، بما فيها المتأخرة والتواريخ والتوفر، تبقى وفق منطقها السابق.

## 2. صفحة المتابعة والتفاصيل

- تقليل عدد الطلبات الافتراضي في الصفحة من 20 إلى **10**.
- إضافة قائمة لاختيار **5 أو 10 أو 15 أو 20** طلبًا في الصفحة، وإعادة الترقيم إلى الصفحة الأولى عند تغيير العدد.
- الحفاظ على عدد الصفحة المختار في حالة صفحة المتابعة أثناء التنقل في الجلسة.
- تقييد ارتفاع لوحة التفاصيل بما يلائم ارتفاع المتصفح، مع تمرير رأسي داخلي للمعلومات الطويلة وسجل الأحداث.
- إبقاء عنوان التفاصيل وأزرار فتح الطلب والتوريد والمعمل والتسليم خارج منطقة التمرير.
- دعم تمرير التفاصيل باستخدام لوحة المفاتيح وإظهار شريط تمرير رفيع عند الحاجة.

## 3. الملفات المعدلة

- `OAS.Infrastructure/Sales/Persistence/Queries/CustomerOrderOperationsQueryService.cs`
- `OAS.Client/Sales/OrderTracking/Pages/CustomerOrderOperationsPage.razor`
- `OAS.Client/Sales/OrderTracking/Pages/CustomerOrderOperationsPage.razor.cs`
- `OAS.Client/Sales/OrderTracking/State/CustomerOrderOperationsState.cs`
- `Shared/UiLib/Components/Sales/OrderTracking/UiCustomerOrderOperationsSurface.razor`
- `Shared/UiLib/wwwroot/css/features/order-tracking-approved.css`

## 4. ما لم يتغير

لم تتغير قواعد الحالات في النطاق المحاسبي أو المخزني أو بيانات جدول الطلبات أو صلاحيات الوصول أو تصميم الألوان العام.

## 5. التحقق والاختبار المطلوب في بيئة المستخدم

**فحص مكتمل هنا:** فحص نصي لبنية Razor وارتباط مدخلات حجم الصفحة، سلامة صياغة CSS باستخدام `tinycss2`، تطابق قيم تعداد الحالات بين `Contracts` و`Domain`، وسلامة حزم ZIP ومحتوياتها. **لم يُشغّل `dotnet build` ولا SQL Server في بيئة المراجعة**؛ لذلك لا يمكن الجزم بأن اختبارات التشغيل نجحت.

1. في Visual Studio: `Clean Solution` ثم `Rebuild Solution`.
2. افتح `/sales/order-tracking`، ثم اختر **قيد التجهيز**، **جاهزة للتسليم**، **بانتظار مخزون**، **متأخرة**، ثم جرّب فلتر الحالة من القائمة. يجب ألّا يظهر خطأ `InvalidCastException`.
3. اختر «طلبات اليوم» ثم «متأخرة» للتحقق من أن فلتر اليوم السابق لا يستمر.
4. افتح طلبًا ذا بنود وأحداث كثيرة. ينبغي أن يتحرك محتوى تفاصيله عموديًا داخل اللوحة، مع بقاء عنوانها وأزرارها مرئية.
5. تحقق من أن الجدول يبدأ بعشرة طلبات، ثم جرّب الخيارات 5 و15 و20 وتأكد من تحديث العدد والصفحات بشكل صحيح.
6. اختبر الوصول إلى طلب محدد عبر رقم الطلب، والتصفية في مختلف الحالات، وتبديل الصفحات بدون تكرار السجلات أو اختفائها.

**ملاحظة:** إذا استمر استثناء EF بعد هذا الإصلاح، فاحفظ الاستعلام وحالة الفلتر والـStackTrace الجديدين؛ فقد تكون هناك مقارنة ثانية في استعلام آخر خارج الخدمة المعدلة.
