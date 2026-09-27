# تكامل OAS Print

أضيف إلى الحل مشروعان:

- `OAS.Printing.Core`: تعريف القوالب ومحرك قراءة بيانات JSON.
- `OAS.Print.Desktop`: برنامج WPF مستقل، وينشر باسم `OAS.Print.exe`.

## مسار الطباعة

1. افتح سند قبض أو سند صرف محفوظًا.
2. يظهر زر **طباعة** في شريط الإجراءات بجوار **تعديل**.
3. يرسل Client فقط نوع السند ومعرّفه إلى `POST /api/printing/jobs`.
4. يعيد API قراءة السند من قاعدة البيانات ويكوّن بيانات الطباعة.
5. توضع المهمة في Queue لمحطة `DEFAULT`.
6. يسحب `OAS.Print.exe` المهمة من `GET /api/printing/desktop/jobs/next?workstationCode=DEFAULT`.
7. يختار البرنامج القالب الافتراضي ويطبع عبر Windows Print Queue؛ لا يتم استخدام `window.print()` أو طباعة المتصفح.

## تشغيل برنامج الطباعة أثناء التطوير

الإعدادات الافتراضية متوافقة مع `OAS.API/Properties/launchSettings.json`:

- API: `https://localhost:7245`
- Workstation: `DEFAULT`
- Polling: مفعّل

إذا تغير منفذ API، افتح **إعدادات** داخل OAS Print وعدل العنوان.

> النسخة الحالية من Client ترسل إلى محطة `DEFAULT`، لذلك اترك Workstation في برنامج الطباعة على `DEFAULT`.

## API Key

في `OAS.API/appsettings.json`:

```json
"Printing": {
  "DesktopApiKey": ""
}
```

ترك القيمة فارغة مناسب للتطوير المحلي. عند النشر ضع قيمة سرية، ثم أدخل القيمة نفسها في إعدادات `OAS.Print.exe`.

## إخراج EXE واحد

شغّل من Windows:

```bat
scripts\publish-oas-print-win-x64.cmd
```

الناتج:

```text
publish\OAS.Print\win-x64\OAS.Print.exe
```

## القوالب الجاهزة

- سند قبض A5: `ACC-RECEIPT-A5`
- سند صرف A5: `ACC-PAYMENT-A5`
- مصروف A5
- قيد يومية A4

يمكن تعديل القوالب وحفظها من برنامج OAS Print نفسه.

## ملاحظة عن Queue

Queue الحالية داخل ذاكرة الـAPI ومصممة للطباعة الفورية. إذا أعيد تشغيل السيرفر قبل أن يستلم برنامج الطباعة المهمة فلن تبقى المهمة. إذا احتجنا ضمانًا دائمًا للطباعة لاحقًا يمكن تحويل Print Jobs إلى جدول قاعدة بيانات دون تغيير واجهة برنامج الطباعة.


## ملاحظة البناء دون اتصال بالإنترنت

تم فصل إعدادات `win-x64` و`SelfContained` و`PublishSingleFile` عن ملف `OAS.Print.Desktop.csproj` حتى لا يطلب Visual Studio تنزيل Runtime Pack أثناء كل Build/Restore.

- للبناء والتشغيل أثناء التطوير: Build المشروع بصورة عادية.
- لإنتاج EXE يعتمد على .NET 9 Desktop Runtime المثبت محلياً: شغّل `scripts\publish-oas-print-win-x64-framework-dependent.cmd`.
- لإنتاج EXE واحد Self-contained: شغّل `scripts\publish-oas-print-win-x64.cmd`. أول نشر Self-contained قد يحتاج وصولاً إلى NuGet لتنزيل Runtime Pack إذا لم يكن موجوداً في كاش الجهاز.
