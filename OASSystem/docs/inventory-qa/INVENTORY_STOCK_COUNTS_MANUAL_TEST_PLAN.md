# Inventory V1 — Stock Counts UI Manual Test Plan

## الهدف
اختبار واجهة `/inventory/stock-counts` يدويًا بصورة كاملة قبل الانتقال إلى الاختبارات الآلية، والتأكد من أن دورة الجرد تعمل وفق الـWorkflow الحالي:

`Draft → Counting → Review → Approved → Posted`

مع `Cancelled` من الحالات غير المرحّلة فقط.

الاختبار يجب أن يثبت أن الإدخال الخاطئ لا يسبب Exception أو Blazor circuit failure، وأن البيانات تنتقل بصورة صحيحة:

`UI → API → Application → Domain → Database → UI`

وعند Post:

`StockCount → Adjustment Transaction(s) → Inventory Ledger → Inventory Balance`

---

## معيار التحقق المرئي المشترك
لكل حقل إجباري:
- [ ] توجد نجمة حمراء `*` بجانب Label.
- [ ] عند ترك الحقل فارغًا يظهر إطار/توهج أحمر حول الحقل.
- [ ] تظهر رسالة صغيرة تحت الحقل تحدد الخطأ.
- [ ] يظهر Snackbar عام يوضح أن هناك بيانات إجبارية أو غير صحيحة.

لكل حقل اختياري:
- [ ] لا توجد نجمة حمراء.
- [ ] يظهر `(اختياري)` في Placeholder عندما يكون مناسبًا.

لأي خطأ:
- [ ] لا يحدث unhandled exception.
- [ ] لا يتوقف Blazor circuit.
- [ ] لا تبقى الأزرار في Loading بعد انتهاء الخطأ.

---

## 1) تحميل الصفحة والقائمة والفلاتر
- [ ] فتح الصفحة بدون أخطاء.
- [ ] جميع عمليات الجرد تظهر، وليس أول 200 سجل فقط.
- [ ] البحث برقم الجرد يعمل.
- [ ] البحث باسم المخزن يعمل.
- [ ] البحث بالملاحظات يعمل.
- [ ] فلتر المخزن يعمل.
- [ ] فلتر الحالة يعمل لجميع الحالات الست.
- [ ] المخزن غير النشط يظهر في الجرد التاريخي مع تمييز `(غير نشط)`.
- [ ] اختيار صف يعرض Header وبنود الجرد كاملة.
- [ ] `SystemQuantity` و`CountedQuantity` و`DifferenceQuantity` تعرض بدقة 3 منازل عشرية.
- [ ] `VarianceValue` يعرض بمنزلتين.
- [ ] تفاصيل Started / Completed / Approved / Posted والمستخدمين تظهر عند توفرها.
- [ ] `CountedBy` ووقت آخر عد يظهران لكل بند تم عده.

---

## 2) إنشاء جرد جديد — الحقول
### Warehouse
- [ ] المخزن إجباري وتوجد `*` حمراء.
- [ ] حفظ بدون مخزن يمنع الطلب ويظهر `هذا الحقل إجباري.` على الحقل.
- [ ] القائمة تعرض المخازن النشطة فقط عند الإنشاء.
- [ ] لا يمكن إنشاء جرد لمخزن غير نشط حتى عبر API.
- [ ] لا يمكن إنشاء جرد لمخزن لا يحتوي على أي Inventory Balance row.
- [ ] لا يمكن إنشاء جرد جديد لنفس المخزن إذا كان لديه جرد بالحالة Draft/Counting/Review/Approved.
- [ ] يمكن إنشاء جرد جديد للمخزن بعد أن يصبح الجرد السابق Posted أو Cancelled.

### Count Date
- [ ] تاريخ الجرد إجباري وتوجد `*` حمراء.
- [ ] حذف التاريخ ثم الحفظ يظهر خطأ موضعي ولا يرسل الطلب.

### Notes
- [ ] الملاحظات اختيارية.
- [ ] Placeholder يظهر `(اختياري)`.
- [ ] الحد الأقصى 500 حرف.
- [ ] قيمة أطول لا تصل إلى قاعدة البيانات.

### Count Number
- [ ] المستخدم لا يحتاج لإدخال الرقم.
- [ ] يتم توليده آليًا بصيغة `SC-YYYY-######`.
- [ ] الرقم فريد.

---

## 3) Create Round-trip
اختر مخزنًا يحتوي على عدة أرصدة بكميات معروفة.

1. [ ] سجّل أرصدة المخزن قبل الإنشاء.
2. [ ] أنشئ الجرد.
3. [ ] Status بعد الإنشاء = `Draft = 1`.
4. [ ] عدد `StockCountLines` يساوي عدد Balance rows في المخزن لحظة الإنشاء.
5. [ ] لكل Variant: `SystemQuantity = OnHandQuantity` لحظة إنشاء الجرد.
6. [ ] `AverageCostSnapshot = AverageUnitCost` لحظة الإنشاء.
7. [ ] CountedAtUtc وCountedBy فارغان قبل العد.
8. [ ] أعد تحميل الصفحة وافتح الجرد؛ القيم المعروضة تطابق DB.
9. [ ] استخدم `verify-inventory-stock-counts.sql` للتحقق.

> ملاحظة: التصميم الحالي يأخذ Snapshot عند **Create** وليس عند Start.

---

## 4) منع جردين مفتوحين لنفس المخزن
- [ ] أنشئ جرد Draft للمخزن A.
- [ ] حاول إنشاء جرد ثانٍ للمخزن A.
- [ ] يظهر خطأ واضح على حقل المخزن.
- [ ] لا يتم إنشاء Header ثانٍ في DB.
- [ ] ألغِ الجرد الأول، ثم أنشئ جردًا جديدًا لنفس المخزن؛ يجب أن ينجح.

---

## 5) Start Counting
على جرد Draft صالح:
- [ ] زر `بدء العد` متاح فقط في Draft.
- [ ] Start يحول الحالة إلى `Counting = 2`.
- [ ] `StartedAtUtc` يتم تعبئته.
- [ ] لا يتغير `SystemQuantity` بعد Start.
- [ ] الضغط غير المشروع على Start بعد Counting عبر API يرجع خطأ حالة ولا يغير البيانات.
- [ ] الجرد الذي لا يحتوي Lines لا يمكن بدء عدّه.

---

## 6) Record Count — Validation لكل بند
### حقل الكمية الفعلية
- [ ] الحقل إجباري وتظهر `*`.
- [ ] تركه فارغًا ثم `تسجيل` يظهر `هذا الحقل إجباري.`.
- [ ] Snackbar عام يظهر ولا يتوقف البرنامج.
- [ ] نص غير رقمي مرفوض بدون Exception.
- [ ] قيمة سالبة مرفوضة.
- [ ] الصفر `0` مقبول.
- [ ] `1.234` مقبول.
- [ ] `1.2345` مرفوض لأن SQL `decimal(18,3)`.
- [ ] قيمة فيها أكثر من 15 رقمًا قبل الفاصلة مرفوضة.
- [ ] القيم الصحيحة تحفظ كما أدخلت.

### بعد التسجيل
- [ ] `CountedQuantity` في DB يساوي القيمة المدخلة.
- [ ] `DifferenceQuantity = CountedQuantity - SystemQuantity`.
- [ ] `VarianceValue = DifferenceQuantity × AverageCostSnapshot` بعد التقريب لمنزلتين.
- [ ] `CountedAtUtc` يأتي من الخادم ويُملأ.
- [ ] `CountedBy` يأتي من المستخدم الحالي ويُملأ.
- [ ] إعادة تحميل الصفحة تعرض القيمة نفسها.

---

## 7) Complete — لا يسمح ببنود غير معدودة
اختبر جردًا فيه 3 بنود مثلًا:

1. سجل كمية بند واحد فقط.
2. اضغط `إكمال العد`.
3. [ ] لا تنتقل الحالة إلى Review.
4. [ ] كل بند غير مسجل يظهر عليه خطأ موضعي.
5. [ ] يظهر Snackbar يذكر عدد الأصناف غير المسجلة.
6. [ ] Status في DB يبقى Counting.
7. أكمل تسجيل جميع البنود.
8. [ ] `إكمال العد` ينجح.
9. [ ] Status يصبح `Review = 3`.
10. [ ] `CompletedAtUtc` يتم تعبئته.

---

## 8) Review — تعديل العد قبل الاعتماد
في Review:
- [ ] حقول الكمية الفعلية ما زالت قابلة للتعديل.
- [ ] غيّر كمية أحد البنود ثم اضغط تسجيل.
- [ ] CountedQuantity/Difference/Variance تتحدث في DB وفي UI.
- [ ] لا يتغير Status ويبقى Review.
- [ ] لا يسمح بكمية غير صحيحة أو Precision غير صالح.

---

## 9) Approve
- [ ] زر اعتماد متاح فقط في Review.
- [ ] بعد الاعتماد Status = `Approved = 4`.
- [ ] `ApprovedAtUtc` يتم تعبئته.
- [ ] `ApprovedBy` يتم تعبئته من المستخدم الحالي.
- [ ] بعد Approved لا يمكن تعديل كميات البنود من UI.
- [ ] محاولة Approve من حالة أخرى عبر API تفشل بدون تغيير البيانات.

---

## 10) Post — فرق صفري بالكامل
أنشئ جردًا تكون كل `CountedQuantity = SystemQuantity`:
- [ ] Complete ثم Approve ثم Post ينجح.
- [ ] Status = `Posted = 5`.
- [ ] PostedAtUtc/PostedBy يتم تعبئتهما.
- [ ] لا يتم إنشاء Adjustment Transactions لأن كل الفروق صفر.
- [ ] لا يتم إنشاء InventoryLedger rows بسبب هذا الجرد.
- [ ] InventoryBalance لا يتغير.

---

## 11) Post — فروق موجبة فقط
مثال: System=10.000, Counted=12.500.

بعد Post:
- [ ] يتم إنشاء InventoryTransaction واحد من النوع `AdjustmentIncrease = 5`.
- [ ] Status للعملية المولدة = `Posted = 2`.
- [ ] `ReferenceType = StockCount`.
- [ ] `ReferenceId = StockCount.Id`.
- [ ] DestinationWarehouseId = مخزن الجرد.
- [ ] لكل فرق موجب يوجد TransactionLine واحد.
- [ ] Quantity = DifferenceQuantity.
- [ ] لكل TransactionLine يوجد Ledger واحد `MovementType = In = 1`.
- [ ] QuantityIn = الفرق وQuantityOut = 0.
- [ ] InventoryBalance.OnHand يزيد بمقدار الفرق.
- [ ] Average cost/value تتحدث عبر `InventoryPostingService`.

---

## 12) Post — فروق سالبة فقط
مثال: System=10.000, Counted=7.250.

بعد Post:
- [ ] يتم إنشاء InventoryTransaction واحد `AdjustmentDecrease = 6`.
- [ ] SourceWarehouseId = مخزن الجرد.
- [ ] لكل فرق سالب TransactionLine بكمية `ABS(DifferenceQuantity)`.
- [ ] Ledger يكون `MovementType = Out = 2`.
- [ ] QuantityOut = ABS(diff) وQuantityIn = 0.
- [ ] OnHand ينقص بمقدار الفرق.
- [ ] تكلفة الصادر في Ledger تستخدم تكلفة الرصيد الحالية حسب PostingService.

---

## 13) Post — فروق مختلطة
اجعل بعض البنود أكبر من النظام وبعضها أقل وبعضها مساويًا.

- [ ] يتم إنشاء **عمليتين فقط كحد أقصى**:
  - AdjustmentIncrease للفروق الموجبة.
  - AdjustmentDecrease للفروق السالبة.
- [ ] البنود ذات الفرق صفر لا تنشئ TransactionLine ولا Ledger.
- [ ] كل Adjustment مرتبط بنفس StockCount عبر ReferenceId.
- [ ] كل Ledger مرتبط بـInventoryTransaction/InventoryTransactionLine صحيحة؛ لا يحدث FK error.
- [ ] StockCount يصبح Posted بعد نجاح العمليتين بالكامل.

---

## 14) Atomicity — فشل Post بسبب رصيد غير كافٍ
هذه حالة إلزامية لأنها تختبر عدم ترك بيانات جزئية.

1. أنشئ الجرد وسجل فرقًا سالبًا.
2. أوصل الجرد إلى Approved.
3. قبل Post، أنقص الرصيد الحالي لنفس Variant بواسطة عملية مخزون أخرى بحيث يصبح الرصيد أقل من مقدار الفرق المطلوب.
4. نفّذ Post.

النتيجة المطلوبة:
- [ ] يظهر خطأ `الرصيد غير كافٍ` بدون توقف البرنامج.
- [ ] StockCount يبقى `Approved`.
- [ ] لا تبقى Adjustment Transaction جزئية مرتبطة بالجرد.
- [ ] لا يبقى TransactionLine جزئي.
- [ ] لا يبقى Ledger جزئي.
- [ ] لا يتغير Balance لأي بند سبق تنفيذه داخل نفس المحاولة.
- [ ] يمكن تصحيح الرصيد ثم إعادة Post لاحقًا.

> زيادة رقم Sequence أثناء محاولة فاشلة ليست فشلًا؛ SQL sequences لا يلزم أن تكون بلا فجوات.

---

## 15) Cancel
اختبر الإلغاء من كل حالة مسموحة:
- [ ] Draft → Cancelled.
- [ ] Counting → Cancelled.
- [ ] Review → Cancelled.
- [ ] Approved → Cancelled.
- [ ] بعد Cancelled لا تظهر أزرار Start/Record/Complete/Approve/Post/Cancel كأزرار صالحة للتنفيذ.
- [ ] محاولة Cancel مرة ثانية عبر API ترجع خطأ واضحًا.
- [ ] محاولة Cancel بعد Posted مرفوضة.
- [ ] إلغاء الجرد لا يغير InventoryBalance ولا ينشئ Ledger.

---

## 16) Round-trip النهائي
لجرد Posted بفروق مختلطة:
- [ ] Header في UI يطابق `tbl_StockCounts`.
- [ ] كل line يطابق `tbl_StockCountLines`.
- [ ] System/Counted/Difference/Cost/Variance تطابق DB.
- [ ] المستخدم وأوقات Start/Complete/Approve/Post تطابق DB.
- [ ] Adjustment Transactions المولدة تظهر أيضًا في شاشة عمليات المخزون.
- [ ] حركاتها تظهر في شاشة Inventory Ledger.
- [ ] أرصدة المخزون بعد Post تطابق النتائج المتوقعة.
- [ ] Refresh ثم إعادة فتح الجرد لا يفقد أي قيمة.

---

## 17) حماية الحالات غير المتوقعة
- [ ] قطع API أثناء Load يظهر Feedback ولا يوقف التطبيق.
- [ ] قطع API أثناء Create يعيد الزر من Loading.
- [ ] قطع API أثناء Select لا يكسر الصفحة.
- [ ] قطع API أثناء Record لا يمحو القيمة ولا يوقف التطبيق.
- [ ] قطع API أثناء Transition يعيد الأزرار من Loading.
- [ ] أي 400/409/403 يظهر Feedback مفهومًا.
- [ ] لا تظهر شاشة بيضاء في أي حالة اختبار.

---

## نتيجة المرحلة
لا نعتبر واجهة الجرد Passed إلا إذا:
- كل الحالات أعلاه ناجحة.
- لا توجد أخطاء unhandled في Browser Console أو API log.
- SQL verification لا يعرض أي FAIL غير متوقع.
- UI وDB يعيدان نفس القيم بعد Round-trip.
