# إصلاح أخطاء CS0019 وCS0246 — طلبات العملاء

التاريخ: 2026-11-13

## التشخيص

كيان `OAS.Domain.Sales.Entities.CustomerOrder` يعرّف `Status` بنوع `OAS.Domain.Sales.Enums.CustomerOrderStatus`، بينما كان الملفان أدناه يستوردان `OAS.Contracts.Sales.Enums.CustomerOrderStatus`؛ وعلى الرغم من تطابق أسماء وقيم الحالات فهما تعدادان مختلفان في C#، وبالتالي فشلت المقارنات.

## الإصلاح

1. `OAS.Application/Sales/CustomerOrders/Queries/GetCustomerOrdersDashboardSummaryQuery.cs` — استبدال استيراد تعداد Contracts باستيراد تعداد Domain في شروط `CountAsync` الأربعة.
2. `OAS.Application/Sales/CustomerOrders/Specifications/CustomerOrderSpecificationFactory.cs` — استخدام تعداد Domain عند تحويل قيمة فلتر `Status` الواردة كنص وعند مقارنتها بـ `CustomerOrder.Status`.

## ماذا عن CS0246؟

`CustomerOrdersController.cs` يتضمن بالفعل مساحة الأسماء `OAS.Application.Sales.CustomerOrders.Queries`، والاستعلام موجود بالمشروع. الخطأ غالبًا تابع لفشل بناء `OAS.Application`؛ إذا بقي بعد إعادة البناء، راجع أول خطأ في قائمة الأخطاء وحالة مرجع المشروع.

## التحقق

- فحص ساكن للمسارات ومساحة الأسماء ونوع حقل حالة الطلب وقيم التعدادين: ناجح.
- التحقق من أن الملفات الأخرى لم تتغير: يتم أثناء تجهيز الأرشيف.
- لا يتوفر .NET SDK في البيئة، لذلك لم يُشغّل `dotnet build` أو اختبارات التطبيق.

## التشغيل في Visual Studio

فك الضغط فوق مجلد المشروع بحيث تتطابق المسارات، ثم `Build > Clean Solution` ثم `Build > Rebuild Solution`، وركّز على أول خطأ إذا ظهرت أخطاء جديدة.
