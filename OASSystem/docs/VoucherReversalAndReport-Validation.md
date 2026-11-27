# اختبار عكس السندات وتقريرها

- لا توجد Migration. حالة العكس في التقرير مشتقة من القيد الأصلي ورابط ReversedJournalId؛ حالة السند الأصلية تبقى Posted.
- العكس المباشر متاح فقط لسند حديث مستقل من نوع أخرى، دون طرف أو تخصيص أو مرجع تشغيلي، وحساب مقابل إيراد للقبض أو مصروف للصرف، مع حساب تسوية نقدي/بنكي نشط ومطابق. الحالات الأخرى تُرفض؛ لا تُعكس تخصيصات الفواتير تلقائياً.
- اختبر بصندوق جديد ممول وحساب مصروف وإيراد صالحين للترحيل اليدوي: سند قبض 100، سند صرف 30، عكس الصرف ثم عكس القبض. يجب أن ترجع المحصلة إلى صفر وأن تبقى القيود الأصلية مرحلة. اختبر تاريخ عكس خارج الفترة، سبب فارغ، عكس متكرر، رصيد غير كافٍ لعكس القبض، ووردية إلزامية مغلقة.
- اختبر سندًا متعدد البنود؛ عد السند مرة واحدة. اختر أحد حسابات البنود؛ يجب أن تكون المجاميع للبنود المطابقة فقط. اختبر سندًا أجنبيًا: القيم الأصلية والدفترية من القيد، والعرض بعملة أخرى تحويل عرض فقط بسعر نهاية الفترة.
- اختبر فترتين منفصلتين يقع فيهما الأصل والعكس؛ يجب أن يظهر الأثر الموجب في فترة الأصل والأثر السالب في فترة العكس. حالة العكس المعروضة حالية.
- سند مرتبط بتخصيص أو سلفة أو مطابقة بنكية أو ذمة عميل/مورد/موظف يجب أن يرفض العكس المباشر. لا تستخدم هذا المسار لتصحيح سندات العمليات التابعة.
- جرّب الأنواع الأحد عشر للتقرير والفرز والتصفية والطباعة والتصدير، ثم طابق القيم مع القيود الأصلية والعكسية. المسودات لا تدخل في الإجماليات المالية.
- لم يتم تشغيل البناء أو اختبار SQL Server في بيئة التسليم؛ يجب تشغيل dotnet build وdotnet test قبل اعتماد النسخة.


## Voucher editor and fiscal filters follow-up

- Open a posted independent revenue receipt and an independent expense payment; the Reverse Voucher button is directly before Close. Confirm using an open posting period and a reason. The detail view should show Reversed and View reversal journal; repeat after reload. Original ledger entry must stay posted.
- Open a posted voucher for a customer/supplier/employee; the action must explain that integrated reversal is not supported. The existing server restrictions remain authoritative.
- For a reversed voucher, its detail toolbar must not offer new allocation. This is a UI safeguard, not a new global allocation rule.
- In the voucher report, confirm 9 report modes and a single independent kind selector. Changing report mode preserves kind and clears conflicting party selection.
- Select a real fiscal year and verify the full configured range; choose a period and verify its real start/end; return to all periods and verify the full year; select custom range and verify editable dates.
- Filter by a cash account and by a bank account separately; only matching detail lines must contribute to grouped totals. Both filters use definition ids, not ledger account ids.
- Report rows still link to the voucher and original/reversal journals but no longer run reversal. PDF and Excel must have the same column counts as the table.
- Test fiscal-reference permissions: a report reader without fiscal-year/period view permission can still report using custom dates.
- Run dotnet build and integration tests locally. No database schema changes in this follow-up.
