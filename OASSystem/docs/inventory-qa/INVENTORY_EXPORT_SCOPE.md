# Inventory Excel Export Scope

تمت إضافة تصدير Excel إلى جداول Inventory V1 التي تمثل قوائم مرجعية أو تشغيلية قابلة للمراجعة خارج النظام.

## الجداول المصدّرة

1. التصنيفات (Product Categories)
2. الماركات (Brands)
3. الوحدات (Units)
4. المنتجات (Products)
5. متغيرات المنتجات (Product Variants)
6. المخازن (Warehouses)
7. أرصدة المخزون (Inventory Balances)
8. عمليات المخزون (Inventory Transactions) + بنود العمليات في Sheet مستقلة
9. حركات المخزون (Inventory Ledger)
10. الجرد المخزني (Stock Counts) + بنود الجرد في Sheet مستقلة

## سلوك التصدير

- يستخدم زر `UiSpreadsheetActions` نفسه المستخدم في Accounting، بوضع Export فقط.
- يحترم البحث والفلاتر الحالية في الشاشة قدر الإمكان.
- أسماء أعمدة Excel عربية ومناسبة للمستخدم، ولا يتم تصدير GUIDs للعلاقات عندما يمكن عرض الكود/الاسم بدلًا منها.
- تصدير Operations وStock Counts يحتوي Sheet رئيسية وSheet للبنود.
- لا يوجد Import أو Template ضمن هذا التعديل.
- Frame Details وLens Details لم يُضاف لهما زر مستقل لأنهما تفاصيل مرتبطة بالمنتج وليسا جدول قائمة مستقلًا في V1.
