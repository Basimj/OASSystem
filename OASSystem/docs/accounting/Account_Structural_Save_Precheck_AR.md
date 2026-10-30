# إصلاح فحص الحفظ لتعارض تغيير بنية الحساب

## المشكلة

الاستثناء `accounting_mapped_account_structure_immutable` صادر من `AccountIntegrityService.ValidateUpdateAsync` عند تغيير التصنيف أو النوع أو الطبيعة أو قبول الترحيل أو الحساب التحكمي لحساب نظامي أو مستخدم في ربط محاسبي. يجب الحفاظ على رمي `ConflictException` في مسار الكتابة.

## ما تغير

- أصبحت `SaveAccountAsync` تنشئ `UpdateAccountRequest` مرة واحدة فقط عند الحفظ.
- يُنفّذ طلب القراءة والتحقق `POST /api/accounting/accounts/{id}/save-check` مباشرة قبل أي طلب `PUT`.
- إذا عاد `CanSave=false` يعرض العميل رسالة عربية من `ReasonCode` مع الحفاظ على التغييرات وعدم إرسال `PUT`.
- إذا عاد `CanSave=true` يرسل **نفس كائن الطلب** الذي فُحص؛ فلا يعاد بناء الطلب من نموذج واجهة قابل للتغيير بين الفحص والتحديث.
- لم تُعدّل `AccountIntegrityService.ValidateUpdateAsync` أو `UpdateAccountCommandHandler`؛ تستمر قواعد الخادم في رمي الاستثناء لحماية الطلبات المباشرة والتزامن.

## التحقق الموصى به

1. **أعد بناء مشروعَي OAS.API وOAS.Client وشغلهما من المجلد الجديد نفسه**؛ تثبيت ملفات العميل فقط لا يكفي إذا ظلت خدمة API تعمل بإصدار قديم.
2. في أدوات مطور المتصفح > Network: عند حفظ تغيير بنيوي محظور يجب أن يظهر `POST /api/accounting/accounts/{id}/save-check` مستجيبًا ببيانات مثل `{"canSave":false,"reasonCode":"accounting_mapped_account_structure_immutable"}` **دون** طلب `PUT` تالٍ.
3. جرّب تعديل اسم حساب مرتبط دون تعديل بنيته: قد يسمح الفحص بذلك إذا لم يكن الحساب مُدارًا مباشرة من شاشة مالكه، مع بقاء قواعد أخرى في طبقة الأعمال.
4. إذا رأيت `PUT` دون POST فهناك نسخة قديمة من العميل أو مسار حفظ مختلف قيد التشغيل. إذا ظهر POST مع `canSave=true` ثم رُفض PUT، راجع اختلاف إصدارات API أو تغيّر البيانات بالتزامن ونتائج Network.
5. تأكد من إيقاف جلسات API القديمة وخدمات IIS Express/Kestrel قبل إعادة التشغيل. تحديث المتصفح باستخدام Ctrl+F5 لا يعيد تحميل مكتبات .NET على الخادم القديم.
6. شغّل `dotnet build OASSystem.sln` ثم `dotnet test OAS.Tests/OAS.Tests.csproj --filter AccountSaveCheckQueryTests`.

## حدود التنفيذ

تم فحص اتساق الشيفرة وملفات الحزم فقط؛ بيئة إعداد الحزمة لا تحتوي .NET SDK لتشغيل البناء أو اختبارات NUnit. لا يمكن منع سباق تزامن بعد فحص سليم مسبقًا، ولذلك يبقى الاستثناء في الخادم مطلوبًا.
