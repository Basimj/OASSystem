# التوثيق العربي الشامل لمكتبة OAS.UiLib

> **الإصدار الموثَّق:** OASSystem v10  
> **المسار:** `Shared/UiLib`  
> **المنصة:** .NET 9 / Blazor / Razor Class Library  
> **نطاق التدقيق:** تمت مراجعة **93 ملفًا** داخل المكتبة، بإجمالي يقارب **1607 سطرًا** من C#/Razor/CSS/Project/Resources، إضافة إلى مراجعة الاستخدامات الفعلية للمكتبة داخل `OAS.Client` وملف الاستضافة `OAS.API/Components/App.razor`.

---

## 1. الهدف من هذه الوثيقة

هذه الوثيقة هي المرجع التطبيقي لمكتبة واجهة المستخدم `OAS.UiLib`. الغرض منها أن يستطيع أي مطور في المشروع أن يعرف:

- أين توجد المكتبة وكيف ترتبط بالمشروع.
- ما الفلسفة المعمارية التي تتبعها.
- ما المكونات المتاحة حاليًا.
- كيف يستخدم كل مكون.
- ما معنى كل `Parameter` وما قيمته الافتراضية.
- كيف يتم ربط البيانات `Binding` والأحداث `Events`.
- كيف تعمل الترجمة داخل المكتبة.
- كيف تستخدم Snackbar وDialog.
- كيف تتحكم بالأحجام، الألوان، المسافات والـDesign Tokens.
- ما القيود الحالية لكل مكون، حتى لا يُفترض أنه يدعم وظيفة غير موجودة.
- كيف تضيف مكونًا جديدًا بدون كسر قواعد المشروع.

> **قاعدة أساسية:** `OAS.UiLib` مكتبة أفقية عامة. لا يجب أن تعرف أي شيء عن `Identity` أو `Sales` أو `Inventory` أو أي Business Feature. المكونات تكون عامة وقابلة لإعادة الاستخدام في أي جزء من النظام.

---

## فهرس المحتويات

1. الهدف من الوثيقة
2. الفلسفة المعمارية
3. بنية المشروع
4. ملف المشروع والحزم
5. ربط UiLib داخل Client
6. CSS وStatic Web Assets
7. الترجمة Localization
8. Design Tokens
9. Enums
10. المكونات العشرون بالتفصيل
11. Snackbar Service
12. Dialog Service
13. Core Models
14. أمثلة مركبة من المشروع الفعلي
15. التحكم بحالة المكونات
16. RTL / LTR
17. Accessibility
18. Bootstrap Loader
19. إضافة Component جديد
20. قواعد الاستخدام داخل OAS.Client
21. الأخطاء الشائعة
22. القيود الحالية
23. Quick Reference
24. خريطة الملفات الكاملة
25. خلاصة التدقيق
26. قاعدة صيانة الوثيقة

> يمكن البحث داخل الملف باسم أي Component مثل `UiInputText` أو `UiDialogService` للوصول مباشرة إلى مرجعه.

---

## 2. الفلسفة المعمارية

العلاقة الحالية في الواجهة هي:

```text
OAS.Client
   ├── OAS.Contracts
   └── OAS.UiLib

OAS.UiLib
   └── Blazor / Localization فقط
```

`OAS.Client` لا يعتمد على `OAS.Application` أو `OAS.Infrastructure` أو `OAS.Domain`. أما `OAS.UiLib` فلا تعتمد على أي طبقة Business في OAS.

المكتبة تستخدم نمطًا ثابتًا للمكونات:

```text
ComponentName.razor       → Markup الخاص بالمكون
ComponentName.razor.cs    → Parameters / State / Events / Logic
ComponentName.razor.css   → CSS Isolation الخاص بالمكون
```

والـCSS العام محصور في Design Tokens وBootstrap loader تحت:

```text
Shared/UiLib/wwwroot/css/
```

### 2.1 ما الذي يوضع في UiLib؟

يوضع فيها شيء عام مثل:

- Button
- Input
- Select
- Grid
- Card
- Dialog
- Snackbar
- Navigation Link
- Layout primitives

ولا يوضع فيها شيء باسم Business Feature مثل:

```text
LoginPanel          ❌
UserEditor          ❌
InvoiceForm         ❌
InventoryCard       ❌
```

لأن هذه تركيبات تخص الـClient/Feature، بينما عناصر البناء الأساسية فقط هي مسؤولية UiLib.

---

## 3. بنية المشروع

```text
Shared/UiLib/
├── Components/
│   ├── Buttons/
│   ├── Data/
│   ├── Dialogs/
│   ├── Display/
│   ├── Feedback/
│   ├── Forms/
│   ├── Inputs/
│   ├── Layout/
│   ├── Media/
│   ├── Navigation/
│   └── Surfaces/
├── Core/
│   ├── Enums/
│   └── Models/
├── Extensions/
├── Localization/
├── Resources/
├── Services/
│   ├── Dialogs/
│   └── Feedback/
├── wwwroot/css/
├── _Imports.razor
└── OAS.UiLib.csproj
```

---

## 4. ملف المشروع OAS.UiLib.csproj

المشروع Razor Class Library:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
```

ويستهدف:

```xml
<TargetFramework>net9.0</TargetFramework>
```

الحزم المباشرة الحالية:

| الحزمة | الإصدار | الاستخدام |
|---|---:|---|
| `Microsoft.AspNetCore.Components.Web` | `9.0.8` | مكونات Blazor وأحداث الويب |
| `Microsoft.Extensions.Localization` | `9.0.8` | `IStringLocalizer` وموارد الترجمة |

لا توجد مراجع ProjectReference إلى طبقات OAS الأخرى، وهذا مقصود للحفاظ على استقلال المكتبة.

---

## 5. ربط UiLib داخل Client

المشروع `OAS.Client` يحتوي المرجع:

```xml
<ProjectReference Include="..\Shared\UiLib\OAS.UiLib.csproj" />
```

ثم يتم تسجيل خدمات المكتبة في DI عن طريق:

```csharp
services.AddOasUiLib();
```

والدالة موجودة في:

```text
Shared/UiLib/Extensions/ServiceCollectionExtensions.cs
```

وتقوم حاليًا بتسجيل:

```csharp
services.AddLocalization(options => options.ResourcesPath = "Resources");
services.AddScoped<IUiSnackbarService, UiSnackbarService>();
services.AddScoped<IUiDialogService, UiDialogService>();
```

وفي OAS يتم استدعاؤها فعليًا من:

```text
OAS.Client/Services/ClientServices.cs
```

### 5.1 Imports المطلوبة

`OAS.Client/_Imports.razor` يستورد namespaces الخاصة بالمكتبة، مثل:

```razor
@using OAS.UiLib.Components.Buttons
@using OAS.UiLib.Components.Data
@using OAS.UiLib.Components.Dialogs
@using OAS.UiLib.Components.Display
@using OAS.UiLib.Components.Feedback
@using OAS.UiLib.Components.Forms
@using OAS.UiLib.Components.Inputs
@using OAS.UiLib.Components.Layout
@using OAS.UiLib.Components.Media
@using OAS.UiLib.Components.Navigation
@using OAS.UiLib.Components.Surfaces
@using OAS.UiLib.Core.Enums
@using OAS.UiLib.Core.Models
@using OAS.UiLib.Services.Feedback
@using OAS.UiLib.Services.Dialogs
```

بعد ذلك يمكن استخدام المكونات مباشرة دون كتابة namespace في كل صفحة.

---

## 6. CSS وStatic Web Assets

في الاستضافة الحالية لا يستدعي `OAS.API/Components/App.razor` ملفات UiLib واحدًا واحدًا، بل يستدعي **Master Bundle واحدًا فقط**:

```html
<link rel="stylesheet" href="_content/OAS.UiLib/css/oas-ui-bundle.css" />
```

الملف `oas-ui-bundle.css` موجود داخل UiLib وهو نقطة الدخول الوحيدة لتنسيقات النظام. وترتيبه الحالي هو:

```css
@import "../lib/bootstrap/css/bootstrap.min.css";
@import "../lib/font-awesome/css/all.min.css";
@import "./variables.css";
@import "./colors.css";
@import "./sizing.css";
@import "./bootstrap-loader.css";
@import "../OAS.UiLib.bundle.scp.css";
```

بهذا الترتيب تكون مكتبات الطرف الثالث أولًا، ثم Design Tokens الخاصة بـOAS، ثم CSS Isolation للمكونات في النهاية حتى تبقى مكونات UiLib صاحبة الأولوية في شكل النظام.

### مكتبات الطرف الثالث المحلية

المكتبات لا تُحمَّل من CDN أثناء تشغيل النظام، بل موجودة محليًا داخل:

```text
Shared/UiLib/wwwroot/lib/
├── bootstrap/
│   ├── css/bootstrap.min.css
│   ├── js/bootstrap.bundle.min.js
│   └── LICENSE
└── font-awesome/
    ├── css/all.min.css
    ├── webfonts/
    └── LICENSE.txt
```

النسخة المضمنة حاليًا في المشروع هي Bootstrap `5.3.6` وFont Awesome Free `6.7.2`. يوجد سكربت تحديث داخل `Shared/UiLib/tools/install-ui-libraries.ps1` مضبوط افتراضيًا لتنزيل Bootstrap `5.3.8` وFont Awesome Free `7.3.1`.

> `bootstrap.bundle.min.js` محفوظ محليًا لاستخدامه عند الحاجة، لكنه **غير مستدعى حاليًا** من `App.razor`. المكونات التفاعلية يجب أن تظل تحت تحكم Blazor/UiLib بدل أن يتنافس Bootstrap JavaScript معها على DOM.

> Bootstrap Icons غير موجودة عمدًا؛ مكتبة الأيقونات القياسية في OAS هي Font Awesome.

> عند نقل UiLib إلى Host آخر، يجب التأكد من خدمة Static Web Assets الخاصة بالـRazor Class Library، وإلا ستظهر المكونات بدون تنسيق.

---

## 6.1 استخدام Font Awesome

بعد تحميل `oas-ui-bundle.css` تصبح أصناف Font Awesome متاحة داخل UiLib تلقائيًا. مثال داخل Component تابع للمكتبة:

```html
<i class="fa-solid fa-user"></i>
<i class="fa-solid fa-gear"></i>
<i class="fa-regular fa-eye"></i>
```

لكن وفق سياسة OAS، صفحات `OAS.Client` لا يُفترض أن تكتب عناصر HTML خام. إذا احتاجت الصفحة إلى أيقونة، فالأفضل أن تقوم مكونات UiLib نفسها بعرض الأيقونة أو إضافة Component عام للأيقونات عند توسيع المكتبة.

## 6.2 تحديث المكتبات المحلية

من جذر المستودع يمكن تشغيل:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Shared\UiLib\tools\install-ui-libraries.ps1
```

أو تحديد إصدارات صريحة:

```powershell
.\Shared\UiLib\tools\install-ui-libraries.ps1 `
    -BootstrapVersion 5.3.8 `
    -FontAwesomeVersion 7.3.1
```

السكربت يستبدل الملفات الموجودة داخل `wwwroot/lib` فقط، ولا يغير `App.razor` ولا مكونات UiLib.

---

# 7. نظام الترجمة Localization

## 7.1 Marker class

المكتبة تستخدم:

```csharp
OAS.UiLib.Localization.UiLibSharedResources
```

وفي `_Imports.razor` الخاص بالمكتبة يتم حقن:

```razor
@inject IStringLocalizer<UiLibSharedResources> L
```

لذلك أي Component داخل UiLib يستطيع استخدام:

```razor
@L["Loading"]
```

بدون إعادة حقن Localizer في كل ملف.

## 7.2 ملفات الموارد

```text
Resources/
├── Localization.UiLibSharedResources.resx
└── Localization.UiLibSharedResources.ar.resx
```

المفاتيح الموجودة حاليًا:

| المفتاح | English | العربية |
|---|---|---|
| `Loading` | Loading… | جارٍ التحميل… |
| `Required` | Required | مطلوب |
| `Save` | Save | حفظ |
| `Cancel` | Cancel | إلغاء |
| `PrimaryNavigation` | Primary navigation | التنقل الرئيسي |
| `ShowPassword` | Show password | إظهار كلمة المرور |
| `HidePassword` | Hide password | إخفاء كلمة المرور |
| `Close` | Close | إغلاق |
| `Confirm` | Confirm | تأكيد |

## 7.3 النص المباشر مقابل Resource Key

بعض المكونات تسمح بالطريقتين:

```razor
<UiButton Text="حفظ" />
```

أو:

```razor
<UiButton TextResourceKey="Save" />
```

في الحالة الثانية تقوم UiLib بالترجمة داخليًا باستخدام مواردها هي.

**الأولوية داخل `UiButton`:**

```text
Loading
→ ChildContent
→ TextResourceKey
→ Text
```

أي إذا كان `Loading=true` فلن يظهر `Text` أو `ChildContent` بل Spinner + `L["Loading"]`.

---

# 8. Design Tokens

## 8.1 المسافات `variables.css`

| المتغير | القيمة الحالية |
|---|---:|
| `--oas-space-1` | `.25rem` |
| `--oas-space-2` | `.5rem` |
| `--oas-space-3` | `.75rem` |
| `--oas-space-4` | `1rem` |
| `--oas-space-5` | `1.25rem` |
| `--oas-radius-sm` | `.25rem` |
| `--oas-radius-md` | `.375rem` |
| `--oas-radius-lg` | `.5rem` |
| `--oas-shadow-sm` | `0 1px 2px rgb(16 24 40 / 5%)` |

## 8.2 الأحجام `sizing.css`

| المتغير | القيمة الحالية |
|---|---:|
| `--oas-font-sm` | `.8125rem` |
| `--oas-font-md` | `.875rem` |
| `--oas-font-lg` | `1rem` |
| `--oas-control-sm` | `1.875rem` |
| `--oas-control-md` | `2.25rem` |
| `--oas-control-lg` | `2.75rem` |

المكتبة مصممة حاليًا لتكون مضغوطة نسبيًا، و`ControlSize.Small` مناسب للشاشات الكثيفة.

## 8.3 الألوان `colors.css`

> **مهم:** اللون الأساسي في نسخة v10 الحالية هو أزرق `#2457d6`، وليس البنفسجي المقترح في التصاميم اللاحقة. هذه الوثيقة تصف الكود الفعلي الحالي.

| Token | القيمة |
|---|---|
| `--oas-color-primary` | `#2457d6` |
| `--oas-color-primary-soft` | `#eef4ff` |
| `--oas-color-on-primary` | `#ffffff` |
| `--oas-color-danger` | `#b42318` |
| `--oas-color-danger-surface` | `#fef3f2` |
| `--oas-color-success` | `#067647` |
| `--oas-color-success-surface` | `#ecfdf3` |
| `--oas-color-warning` | `#b54708` |
| `--oas-color-warning-surface` | `#fffaeb` |
| `--oas-color-info-border` | `#84adff` |
| `--oas-color-info-surface` | `#eff4ff` |
| `--oas-color-background` | `#f8fafc` |
| `--oas-color-surface` | `#ffffff` |
| `--oas-color-subtle` | `#f9fafb` |
| `--oas-color-hover` | `#f2f4f7` |
| `--oas-color-disabled-surface` | `#f2f4f7` |
| `--oas-color-text` | `#101828` |
| `--oas-color-text-muted` | `#667085` |
| `--oas-color-border` | `#d0d5dd` |

لتغيير الـTheme لاحقًا يفضَّل تغيير Tokens المركزية بدل نسخ ألوان داخل صفحات Client.

---

# 9. مرجع Enums

## `ControlSize`

```csharp
Small, Medium, Large
```

يستخدمه: `UiButton`, `UiTextLabel`, `UiInputText`, `UiSelect`, `UiStatusAction`.

## `ButtonVariant`

```csharp
Primary, Secondary, Danger, Ghost
```

- `Primary`: الإجراء الرئيسي.
- `Secondary`: إجراء ثانوي بحدود.
- `Danger`: إجراء خطر مثل حذف.
- `Ghost`: زر شفاف بدون Surface.

## `AlertTone`

```csharp
Info, Success, Warning, Danger
```

يستخدم في `UiAlert`, Snackbar, `UiStatusAction`, Confirm dialog.

## `GridRatio`

```csharp
Equal, TwoToOne, OneToTwo
```

النسب الخاصة تعمل فعليًا فقط عندما `Columns=2`.

## `PagePlacement`

```csharp
Start, Center
```

`Center` يجعل الصفحة بارتفاع `100dvh` ويضع المحتوى في المنتصف؛ مستخدم حاليًا في Login.

## `StackGap`

```csharp
Small, Medium, Large
```

## `SurfaceWidth`

```csharp
Auto, Compact, Medium, Full
```

- `Compact`: حد أقصى `28rem`.
- `Medium`: حد أقصى `48rem`.
- `Full`: 100%.
- `Auto`: عرض طبيعي.

## `TextTone`

```csharp
Default, Muted, Danger, Success
```

## `TextWeight`

```csharp
Normal, Medium, Semibold, Bold
```

## `UiDialogSize`

```csharp
Small, Medium, Large, FullWidth
```

## `UiSnackbarPosition`

```text
TopLeft      TopCenter      TopRight
CenterLeft   Center         CenterRight
BottomLeft   BottomCenter   BottomRight
```

---

# 10. المكونات Components

## 10.1 `UiButton`

**المسار:** `Components/Buttons/UiButton.*`

زر عام يدعم الحالات، الأحجام، أنواع العرض، محتوى مخصص، Attributes إضافية، وحالة Loading.

### Parameters

| الخاصية | النوع | الافتراضي | المعنى |
|---|---|---|---|
| `Type` | `string` | `"button"` | قيمة HTML `type` مثل `button` أو `submit` |
| `Text` | `string?` | `null` | النص المباشر |
| `TextResourceKey` | `string?` | `null` | مفتاح ترجمة من Resources الخاصة بـUiLib |
| `Variant` | `ButtonVariant` | `Primary` | الشكل اللوني |
| `Size` | `ControlSize` | `Medium` | Small/Medium/Large |
| `Disabled` | `bool` | `false` | تعطيل الزر |
| `Loading` | `bool` | `false` | تعطيل الزر وإظهار Spinner + نص Loading |
| `FullWidth` | `bool` | `false` | يجعل العرض 100% |
| `AdditionalCssClass` | `string?` | `null` | Class إضافي على العنصر الداخلي |
| `ChildContent` | `RenderFragment?` | `null` | محتوى Razor مخصص بدل النص |
| `OnClick` | `EventCallback<MouseEventArgs>` | — | حدث النقر |
| `AdditionalAttributes` | Dictionary | — | Attributes غير معرفة مثل `aria-*`, `data-*`, `title` |

### سلوك مهم

- `Loading=true` يجعل `disabled=true` تلقائيًا.
- عند `Disabled` أو `Loading` لا يتم استدعاء `OnClick`.
- يتم منع `AdditionalAttributes` من تجاوز: `class`, `type`, `disabled`, `aria-busy`.
- إذا استخدمت `ChildContent` فهو أعلى أولوية من `TextResourceKey` و`Text`.

### مثال زر عادي

```razor
<UiButton Text="@L[\"Save\"]"
          Variant="ButtonVariant.Primary"
          Size="ControlSize.Small"
          OnClick="SaveAsync" />
```

### زر Submit

```razor
<UiButton Type="submit"
          Text="@L[\"Login_Button\"]"
          Loading="_saving"
          FullWidth="true"
          Size="ControlSize.Small" />
```

### زر بمحتوى مخصص

```razor
<UiButton Variant="ButtonVariant.Ghost">
    <span>+</span>
    <span>إضافة</span>
</UiButton>
```

> HTML المخصص في المثال الأخير يوضع فقط إذا كان المشروع يسمح به في ذلك المستوى. في OAS.Client القاعدة الحالية هي تركيب UiLib Components، لذلك الأفضل تطوير Icon support داخل UiButton بدل بناء markup محلي متكرر.

---

## 10.2 `UiTextLabel`

**المسار:** `Components/Display/UiTextLabel.*`

يعرض `label` فعليًا إذا كانت `For` موجودة، وإلا يعرض `span`.

### Parameters

| الخاصية | النوع | الافتراضي | المعنى |
|---|---|---|---|
| `For` | `string?` | null | ID الحقل المرتبط؛ وجوده يحول العنصر إلى `<label>` |
| `Text` | `string?` | null | نص مباشر |
| `TextResourceKey` | `string?` | null | مفتاح ترجمة UiLib |
| `Required` | `bool` | false | يظهر `*` باللون الأحمر |
| `Size` | `ControlSize` | Medium | حجم النص |
| `Weight` | `TextWeight` | Medium | وزن الخط |
| `Tone` | `TextTone` | Default | لون/دلالة النص |
| `AdditionalCssClass` | `string?` | null | Class إضافي |
| `ChildContent` | `RenderFragment?` | null | محتوى مخصص |
| `AdditionalAttributes` | Dictionary | — | Attributes إضافية |

**أولوية المحتوى:** `ChildContent → TextResourceKey → Text`.

### أمثلة

```razor
<UiTextLabel Text="اسم العميل" Required="true" />
```

```razor
<UiTextLabel TextResourceKey="Save"
             Tone="TextTone.Muted"
             Weight="TextWeight.Semibold" />
```

### ملاحظة Accessibility

النجمة `*` عليها `aria-hidden=true`، أي أنها مؤشر بصري فقط. تعريف الحقل كمطلوب فعليًا مسؤولية Input (`aria-required`/`required`) وليس Label وحدها.

---

## 10.3 `UiInputText`

**المسار:** `Components/Inputs/UiInputText.*`

هذا من أهم مكونات المكتبة. يرث من:

```csharp
InputBase<string?>
```

ولذلك يتكامل مع `EditContext` و`@bind-Value` وحالات `modified/valid/invalid` الخاصة بـBlazor.

### Parameters المعلنة مباشرة

| الخاصية | النوع | الافتراضي | المعنى |
|---|---|---|---|
| `Id` | `string?` | null | ID يدوي. إذا لم يحدد يُنشأ من FieldIdentifier |
| `Type` | `string` | `text` | HTML input type مثل text/password/email/tel |
| `Label` | `string?` | null | Label مباشر |
| `LabelResourceKey` | `string?` | null | ترجمة Label من UiLib Resources |
| `Placeholder` | `string?` | null | Placeholder مباشر |
| `PlaceholderResourceKey` | `string?` | null | ترجمة Placeholder من UiLib Resources |
| `Required` | `bool` | false | يضبط `aria-required` ويشارك في native required حسب الإعداد |
| `EnableNativeValidation` | `bool` | true | إذا false لا يرسل `required` للمتصفح |
| `ReadOnly` | `bool` | false | يجعل الحقل للقراءة فقط |
| `Disabled` | `bool` | false | يعطل الحقل |
| `MaxLength` | `int?` | null | maxlength |
| `AutoComplete` | `string?` | null | مثل `username`, `current-password` |
| `InputMode` | `string?` | null | تلميح لوحة المفاتيح مثل `numeric`, `email` |
| `ShowValidationMessage` | `bool` | true | يعرض `ValidationMessage` تحت الحقل عند وجود ValueExpression |
| `RevealPassword` | `bool` | false | إظهار زر العين إذا كان Type=password |
| `Size` | `ControlSize` | Medium | الحجم |

### Parameters الموروثة المهمة من `InputBase<string?>`

عمليًا تستطيع استخدام:

```text
Value
ValueChanged
ValueExpression
AdditionalAttributes
```

والأسهل غالبًا:

```razor
@bind-Value="_model.Name"
```

### Binding

```razor
<UiInputText @bind-Value="_model.UserName"
             Label="اسم المستخدم"
             Size="ControlSize.Small" />
```

القيمة تتحدث على حدث `oninput`، أي أثناء الكتابة، وليس فقط عند فقدان التركيز.

### Password Reveal

```razor
<UiInputText @bind-Value="_model.Password"
             Type="password"
             RevealPassword="true"
             AutoComplete="current-password" />
```

- زر العين لا يظهر إلا إذا `RevealPassword=true` و`Type=password`.
- يبدل النوع داخليًا بين `password` و`text`.
- `aria-label` يأتي من `ShowPassword/HidePassword` في موارد UiLib.

### Validation

إذا كان الحقل داخل `EditForm`/`UiForm` وله `ValueExpression`، يضيف Blazor classes مثل:

```text
modified
valid
invalid
```

وCSS الحالي يلون `.invalid` باللون الأحمر.

إذا `ShowValidationMessage=true` يتم إنشاء:

```razor
<ValidationMessage For="@ValueExpression" />
```

### Native Validation مقابل Application Validation

- `Required=true + EnableNativeValidation=true`: المتصفح يحصل على `required`.
- `EnableNativeValidation=false`: يبقى `aria-required`, لكن لا يعتمد على browser validation. هذا هو النمط المستخدم حاليًا في Login لأن التحقق يتم عبر التطبيق/API.

### AdditionalAttributes

يسمح بتمرير Attributes إضافية، لكن يمنع تجاوز Attributes التي يديرها المكون نفسه:

```text
id, type, class, value, placeholder, readonly, disabled,
required, aria-required, maxlength, autocomplete, inputmode
```

يمكن مثلًا تمرير:

```razor
<UiInputText @bind-Value="_value"
             aria-describedby="name-help"
             data-testid="name-input" />
```

### قيود حالية

- المكون مخصص لقيمة `string?` فقط.
- لا يوجد Prefix/Suffix/Icon API عام حتى الآن.
- لا يوجد Debounce للـoninput.

---

## 10.4 `UiCheckbox`

**المسار:** `Components/Inputs/UiCheckbox.*`

### Parameters

| الخاصية | النوع | الافتراضي | المعنى |
|---|---|---|---|
| `Value` | bool | false | القيمة الحالية |
| `ValueChanged` | EventCallback<bool> | — | حدث تغير القيمة |
| `Label` | string? | null | النص بجانب Checkbox |
| `Disabled` | bool | false | تعطيل التحكم |

### Binding

```razor
<UiCheckbox @bind-Value="_model.RememberMe"
            Label="تذكرني" />
```

### سلوك داخلي

`label` يغلف `input` والنص، لذلك الضغط على النص يغير Checkbox أيضًا.

### قيد مهم

`UiCheckbox` **لا يرث من `InputBase<bool>` حاليًا**. لذلك:

- `@bind-Value` يعمل.
- لكنه لا يشارك تلقائيًا في EditContext/Validation بنفس عمق `UiInputText`.
- لا توجد `ValidationMessage` أو `ValueExpression` مخصصة له حاليًا.

---

## 10.5 `UiSelect`

**المسار:** `Components/Inputs/UiSelect.*`

يرث من `InputBase<string?>`.

### Parameters

| الخاصية | النوع | الافتراضي | المعنى |
|---|---|---|---|
| `Id` | string? | null | ID يدوي أو مولد من FieldIdentifier |
| `Label` | string? | null | Label مباشر |
| `Placeholder` | string? | null | أول Option بقيمة فارغة |
| `Options` | `IReadOnlyList<UiSelectOption>` | `[]` | الخيارات |
| `Required` | bool | false | Required/aria-required |
| `EnableNativeValidation` | bool | true | تفعيل required الأصلي |
| `Disabled` | bool | false | تعطيل القائمة |
| `Size` | ControlSize | Medium | الحجم |
| `OnValueChanged` | `EventCallback<string?>` | — | Callback إضافي بعد تحديث القيمة |

### UiSelectOption

```csharp
public sealed record UiSelectOption(string Value, string Text);
```

### تجهيز الخيارات من بيانات API

مثال فعلي شبيه بما يحدث في Login:

```csharp
_profileOptions = profiles
    .Select(x => new UiSelectOption(x.Key, x.DisplayName))
    .ToArray();
```

ثم:

```razor
<UiSelect @bind-Value="_selectedProfileKey"
          Options="_profileOptions"
          Label="قاعدة البيانات"
          OnValueChanged="OnProfileChangedAsync" />
```

### استخدام Guid

لأن القيمة الحالية `string?`، حول Guid إلى string:

```csharp
_roleOptions = roles
    .Select(r => new UiSelectOption(r.Id.ToString(), r.Name))
    .ToArray();
```

ثم عند الحفظ:

```csharp
var roleId = Guid.Parse(_form.RoleId);
```

### قيود حالية

- المكون ليس Generic؛ القيمة دائمًا `string?`.
- لا يوجد Search داخل القائمة.
- لا يوجد Multiple selection.
- لا يعرض `ValidationMessage` داخليًا حاليًا.
- رغم أن `InputBase` يملك `AdditionalAttributes`، الـmarkup الحالي لا يمررها إلى `<select>`.
- الترجمة للـLabel/Placeholder تتم من خارج المكون؛ لا توجد `LabelResourceKey` حاليًا.

---

## 10.6 `UiForm`

**المسار:** `Components/Forms/UiForm.*`

Wrapper بسيط حول `EditForm`.

### Parameters

| الخاصية | النوع | مطلوب | المعنى |
|---|---|---|---|
| `Model` | object | نعم (`EditorRequired`) | النموذج المرتبط |
| `OnSubmit` | EventCallback | لا | ينفذ عند Submit |
| `ChildContent` | RenderFragment? | لا | حقول النموذج |

### مثال

```razor
<UiForm Model="_model" OnSubmit="SaveAsync">
    <UiInputText @bind-Value="_model.Name" Label="الاسم" />
    <UiButton Type="submit" Text="حفظ" />
</UiForm>
```

### نقطة مهمة جدًا عن Validation

المكون يستخدم:

```razor
<EditForm Model="@Model" OnSubmit="HandleSubmitAsync">
```

وليس `OnValidSubmit` أو `OnInvalidSubmit`.

لذلك `OnSubmit` يعني **كل عملية Submit**، ولا يعني تلقائيًا "بعد نجاح DataAnnotations/FluentValidation".

إذا أردت Validation مبنيًا على EditContext، يجب أن يكون هناك Validator مناسب وأن يتم تصميم تدفق التحقق بوضوح. في OAS الحالي كثير من النماذج تقوم بالتحقق في Client/API قبل التنفيذ.

### قيد حالي

لا يوجد Parameter لاختيار `OnValidSubmit`/`OnInvalidSubmit`، ولا يتم إدراج `DataAnnotationsValidator` تلقائيًا.

---

## 10.7 `UiCard`

**المسار:** `Components/Surfaces/UiCard.*`

Surface عام يحتوي Header اختياري وBody.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Title` | string? | null |
| `Subtitle` | string? | null |
| `Width` | SurfaceWidth | Full |
| `ChildContent` | RenderFragment? | null |

### مثال

```razor
<UiCard Title="بيانات المستخدم"
        Subtitle="البيانات الأساسية"
        Width="SurfaceWidth.Medium">
    <UiTextLabel Text="المحتوى" />
</UiCard>
```

### Width

- `Compact`: `min(100%, 28rem)` ومتمركز.
- `Medium`: `min(100%, 48rem)` ومتمركز.
- `Full`: 100%.
- `Auto`: Auto width.

---

## 10.8 `UiPage`

**المسار:** `Components/Layout/UiPage.*`

Container رئيسي للصفحة.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Title` | string? | null |
| `Subtitle` | string? | null |
| `Actions` | RenderFragment? | null |
| `ChildContent` | RenderFragment? | null |
| `Placement` | PagePlacement | Start |

### مثال Header + Actions

```razor
<UiPage Title="المستخدمون" Subtitle="إدارة حسابات النظام">
    <Actions>
        <UiButton Text="إضافة مستخدم" Size="ControlSize.Small" />
    </Actions>
    <ChildContent>
        ...
    </ChildContent>
</UiPage>
```

### توسيط Login

```razor
<UiPage Placement="PagePlacement.Center">
    <UiCard Width="SurfaceWidth.Medium">...</UiCard>
</UiPage>
```

`Center` يستخدم `100dvh` و`place-items:center`.

---

## 10.9 `UiGrid`

**المسار:** `Components/Layout/UiGrid.*`

Grid بسيط من 1 إلى 4 أعمدة.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Columns` | int | 2 |
| `Ratio` | GridRatio | Equal |
| `ChildContent` | RenderFragment? | null |

`Columns` يتم ضبطه داخليًا باستخدام:

```csharp
Math.Clamp(Columns, 1, 4)
```

أي لو أرسلت 8 فسيستخدم 4.

### نسب عمودين

```razor
<UiGrid Columns="2" Ratio="GridRatio.TwoToOne">
    ...
</UiGrid>
```

النسب الخاصة (`TwoToOne`, `OneToTwo`) لها CSS فعلي فقط مع `Columns=2`.

### Responsive

عند عرض أقل من `48rem` يتحول Grid إلى عمود واحد.

---

## 10.10 `UiStackLayout`

**المسار:** `Components/Layout/UiStackLayout.*`

يرتب المحتوى عموديًا باستخدام CSS Grid.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Gap` | StackGap | Medium |
| `ChildContent` | RenderFragment? | null |

### القيم

- Small → `--oas-space-2`
- Medium → `--oas-space-4`
- Large → `--oas-space-5`

### مثال

```razor
<UiStackLayout Gap="StackGap.Small">
    <UiInputText ... />
    <UiInputText ... />
    <UiButton ... />
</UiStackLayout>
```

---

## 10.11 `UiAppShell`

**المسار:** `Components/Layout/UiAppShell.*`

Shell التطبيق الحالي. يتكون من Header علوي + Navigation + Actions + Main content.

### Parameters / Slots

| الخاصية | النوع | الوظيفة |
|---|---|---|
| `Brand` | string? | اسم النظام |
| `NavigationContent` | RenderFragment? | روابط التنقل |
| `ActionsContent` | RenderFragment? | إجراءات الهيدر |
| `ChildContent` | RenderFragment? | محتوى الصفحة |

### مثال حقيقي من MainLayout

```razor
<UiAppShell Brand="@L[\"App_Name\"]">
    <NavigationContent>
        <UiNavLink Href="/" Match="NavLinkMatch.All" Text="الرئيسية" />
    </NavigationContent>
    <ActionsContent>
        <UiButton Text="تسجيل الخروج" Size="ControlSize.Small" />
    </ActionsContent>
    <ChildContent>@Body</ChildContent>
</UiAppShell>
```

### ملاحظة مهمة

النسخة الحالية من `UiAppShell` **ليست Sidebar/Tab Workspace** بعد. هي Header علوي بسيط. أي توثيق أو كود يفترض وجود sidebar collapsible حاليًا سيكون غير صحيح.

---

## 10.12 `UiNavLink`

**المسار:** `Components/Navigation/UiNavLink.*`

Wrapper حول Blazor `NavLink`.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Href` | string | مطلوب |
| `Text` | string? | null |
| `Match` | NavLinkMatch | Prefix |

### مثال

```razor
<UiNavLink Href="/identity/users"
           Text="المستخدمون"
           Match="NavLinkMatch.Prefix" />
```

`NavLink` يضيف class `active` تلقائيًا، وCSS الخاص بـUiLib يطبّق لون Primary عليها.

### قيود حالية

لا يوجد Icon/Badge/ChildContent API.

---

## 10.13 `UiPictureBox`

**المسار:** `Components/Media/UiPictureBox.*`

يعرض صورة إذا `Src` موجود، وإلا يعرض Fallback دائري.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Src` | string? | null |
| `Alt` | string | `""` |
| `FallbackText` | string | `"OAS"` |

### مثال صورة

```razor
<UiPictureBox Src="images/login.jpg"
              Alt="صورة النظام" />
```

### مثال Fallback

```razor
<UiPictureBox Alt="شعار النظام" FallbackText="OAS" />
```

### السلوك

- الصورة `loading="lazy"`.
- `object-fit: cover`.
- الحد الأدنى للارتفاع الحالي `14rem`.

---

## 10.14 `UiAlert`

**المسار:** `Components/Feedback/UiAlert.*`

رسالة ثابتة Inline، تختلف عن Snackbar المؤقت.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Message` | string? | null |
| `Tone` | AlertTone | Info |
| `ChildContent` | RenderFragment? | null |

إذا لم توجد Message ولا ChildContent فلا يتم Render لأي عنصر.

```razor
<UiAlert Tone="AlertTone.Warning"
         Message="لا توجد بيانات." />
```

استخدم `UiAlert` لرسالة جزء من تخطيط الصفحة، واستخدم Snackbar لإشعار عابر مرتبط بعملية.

---

## 10.15 `UiStatusAction`

**المسار:** `Components/Feedback/UiStatusAction.*`

يجمع Status indicator مع `UiButton`. مستخدم حاليًا في Login لزر تحديث قاعدة البيانات.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Text` | string? | null |
| `StatusText` | string? | null |
| `Tone` | AlertTone | Info |
| `Disabled` | bool | false |
| `Loading` | bool | false |
| `AlignToLabeledControl` | bool | false |
| `Variant` | ButtonVariant | Secondary |
| `Size` | ControlSize | Medium |
| `OnClick` | EventCallback<MouseEventArgs> | — |

### Indicator

- Success: دائرة خضراء بدون نص.
- Warning: دائرة برتقالية وبداخلها `?`.
- Danger: حمراء.
- Info: رمادية.

`StatusText` يستخدم كـ`title` و`aria-label`.

### AlignToLabeledControl

إذا كان بجانب Input/Select لديه Label:

```razor
<UiStatusAction AlignToLabeledControl="true" ... />
```

يضيف padding علوي محسوب بحيث يصطف الزر مع control وليس مع أعلى الـLabel.

---

## 10.16 `UiDataTable<TItem>`

**المسار:** `Components/Data/UiDataTable.*`

جدول عرض نصي Generic خفيف.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Items` | `IReadOnlyList<TItem>` | `[]` |
| `Columns` | `IReadOnlyList<UiDataColumn<TItem>>` | `[]` |
| `EmptyText` | string | empty |

### تعريف الأعمدة

```csharp
public sealed record UiDataColumn<TItem>(
    string Header,
    Func<TItem, string?> ValueSelector);
```

مثال:

```csharp
_columns =
[
    new("اسم المستخدم", x => x.UserName),
    new("الاسم", x => x.DisplayName),
    new("البريد", x => x.Email ?? "—")
];
```

ثم:

```razor
<UiDataTable TItem="UserDto"
             Items="_users"
             Columns="_columns"
             EmptyText="لا توجد بيانات" />
```

### جلب البيانات

```csharp
protected override async Task OnInitializedAsync()
{
    _users = await UserService.GetUsersAsync();
}
```

عند تحديث `_users` وإعادة render، الجدول يعرض القائمة الجديدة.

### القيود الحالية — مهمة

`UiDataTable` **ليس DataGrid متقدمًا حاليًا**. لا يدعم داخليًا:

- Pagination.
- Sorting.
- Filtering.
- Row selection.
- Cell templates.
- Action column templates.
- Virtualization.
- Server-side data source.

كما أن `ValueSelector` يرجع `string?` فقط.

---

## 10.17 `UiSnackbarHost`

**المسار:** `Components/Feedback/UiSnackbarHost.*`

هذا هو Renderer الخاص برسائل `IUiSnackbarService`.

لا يحتوي Parameters؛ يأخذ Service من DI:

```csharp
[Inject] IUiSnackbarService Snackbar
```

ويستمع إلى event:

```csharp
Snackbar.Changed
```

### أين يوضع؟

مرة واحدة فقط على مستوى التطبيق، وحاليًا موجود في:

```text
OAS.Client/Routes.razor
```

```razor
<UiDialogHost />
<UiSnackbarHost />
```

لا تضع Host داخل كل صفحة.

### Accessibility

- Host: `aria-live="polite"`.
- رسائل Danger تحصل على `role="alert"`.
- بقية الرسائل `role="status"`.

---

## 10.18 `UiDialog`

**المسار:** `Components/Dialogs/UiDialog.*`

Surface الأساسي للحوار المنبثق.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Title` | string? | null |
| `Size` | UiDialogSize | Medium |
| `ShowCloseButton` | bool | true |
| `ChildContent` | RenderFragment? | null |
| `Footer` | RenderFragment? | null |
| `OnClose` | EventCallback | — |

### Accessibility

يستخدم:

```text
role="dialog"
aria-modal="true"
aria-label=Title
```

ويوقف propagation للنقر حتى لا يعتبر النقر داخل الـDialog نقرًا على Backdrop.

### استخدام مباشر

```razor
<UiDialog Title="تفاصيل" OnClose="Close">
    <ChildContent>...</ChildContent>
    <Footer>
        <UiButton Text="إغلاق" OnClick="..." />
    </Footer>
</UiDialog>
```

غالبًا الأفضل في التطبيق استخدام `IUiDialogService` بدل إدارة الظهور يدويًا.

---

## 10.19 `UiConfirmDialog`

Wrapper جاهز فوق `UiDialog` لعملية التأكيد.

### Parameters

| الخاصية | النوع | الافتراضي |
|---|---|---|
| `Title` | string | مطلوب |
| `Message` | string | مطلوب |
| `Tone` | AlertTone | Warning |
| `Size` | UiDialogSize | Small |
| `ShowCloseButton` | bool | true |
| `ConfirmText` | string? | `L["Confirm"]` |
| `CancelText` | string? | `L["Cancel"]` |
| `OnConfirm` | EventCallback | — |
| `OnCancel` | EventCallback | — |

إذا `Tone=Danger` يصبح زر التأكيد `ButtonVariant.Danger`. بقية الحالات تستخدم Primary.

الاستخدام المعتاد يكون عبر:

```csharp
await Dialog.ConfirmAsync(...)
```

بدل Render المكون يدويًا.

---

## 10.20 `UiDialogHost`

Renderer المركزي لـ`IUiDialogService`.

- إذا `Current.IsConfirm=true` يعرض `UiConfirmDialog`.
- إذا `ComponentType` موجود، يستخدم `DynamicComponent`.
- النقر على Backdrop يغلق فقط إذا `CloseOnBackdrop=true`.
- Escape يغلق فقط إذا `CloseOnEscape=true` **وعندما يصل keyboard event إلى الـBackdrop**.

> يوجد Dialog واحد فقط في الوقت نفسه. فتح Dialog جديد يلغي السابق تلقائيًا في `UiDialogService`.

---

# 11. Snackbar Service بالتفصيل

## 11.1 الحقن

```razor
@inject IUiSnackbarService Snackbar
```

أو في C# constructor injection.

## 11.2 API

```csharp
Snackbar.Success("تم الحفظ");
Snackbar.Error("تعذر الحفظ");
Snackbar.Warning("توجد بيانات ناقصة");
Snackbar.Info("تم تحديث البيانات");
```

والدالة العامة:

```csharp
Snackbar.Show(
    message,
    tone: AlertTone.Info,
    durationMilliseconds: 5000,
    autoClose: true);
```

ترجع `Guid` للرسالة، ويمكن إغلاقها:

```csharp
var id = Snackbar.Info("جارٍ التنفيذ", autoClose: false);
...
Snackbar.Dismiss(id);
```

أو حذف الجميع:

```csharp
Snackbar.Clear();
```

## 11.3 الإعدادات الافتراضية

```csharp
Position = UiSnackbarPosition.TopRight;
DurationMilliseconds = 4000;
MaxVisible = 4;
ShowCloseButton = true;
```

## 11.4 الحدود

`Normalize()` يفرض:

```text
DurationMilliseconds: من 1000 إلى 60000
MaxVisible: من 1 إلى 10
```

وكذلك المدة المرسلة لكل رسالة يتم Clamp لها بين 1–60 ثانية.

إذا تجاوز العدد `MaxVisible` تتم إزالة **أقدم رسالة** أولًا.

## 11.5 تغيير الموقع والمدة

مباشرة من UiLib:

```csharp
Snackbar.Configure(new UiSnackbarOptions
{
    Position = UiSnackbarPosition.BottomCenter,
    DurationMilliseconds = 6000,
    MaxVisible = 3,
    ShowCloseButton = true
});
```

### التكامل الحالي داخل OAS.Client

OAS يضيف Service وسيطًا:

```text
OAS.Client/Common/Feedback/Services/UiFeedbackSettingsService.cs
```

ويستقبل:

```csharp
public sealed record UiFeedbackSettings
{
    public UiSnackbarPosition SnackbarPosition { get; init; } = UiSnackbarPosition.TopRight;
    public int SnackbarDurationMilliseconds { get; init; } = 4000;
    public int SnackbarMaxVisible { get; init; } = 4;
}
```

ثم يطبقها على UiLib. هذه هي النقطة الصحيحة مستقبلًا لربط شاشة إعدادات النظام بمكان ومدة Snackbar.

---

# 12. Dialog Service بالتفصيل

## 12.1 الحقن

```razor
@inject IUiDialogService Dialog
```

## 12.2 Confirm

```csharp
var confirmed = await Dialog.ConfirmAsync(
    title: "تأكيد الحذف",
    message: "هل تريد حذف المستخدم؟",
    tone: AlertTone.Danger);

if (!confirmed)
    return;

await DeleteAsync();
```

### تخصيص النص

```csharp
var confirmed = await Dialog.ConfirmAsync(
    "تأكيد",
    "هل تريد المتابعة؟",
    confirmText: "نعم",
    cancelText: "لا");
```

## 12.3 فتح Component كامل

```csharp
var result = await Dialog.ShowAsync<UserEditorComponent>(
    title: "تعديل المستخدم",
    parameters: new Dictionary<string, object>
    {
        ["UserId"] = userId
    },
    options: new UiDialogOptions
    {
        Size = UiDialogSize.Large,
        CloseOnBackdrop = false,
        CloseOnEscape = true,
        ShowCloseButton = true
    });
```

أسماء `parameters` يجب أن تطابق أسماء `[Parameter]` في Component المستهدف.

## 12.4 إغلاق Dialog من داخله

المكون المعروض يستطيع حقن Service:

```razor
@inject IUiDialogService Dialog
```

ثم:

```csharp
Dialog.Close(savedDto);
```

والطرف الذي فتح Dialog يحصل على:

```csharp
if (!result.Cancelled)
{
    var saved = result.Value as UserDto;
}
```

أو:

```csharp
Dialog.Cancel();
```

## 12.5 UiDialogOptions

| الخاصية | الافتراضي | الوظيفة |
|---|---:|---|
| `Size` | Medium | حجم الحوار |
| `CloseOnBackdrop` | true | إغلاق بالنقر خارج الحوار |
| `CloseOnEscape` | true | إغلاق بـEscape عند وصول الحدث |
| `ShowCloseButton` | true | زر × في الهيدر |

## 12.6 سلوك Dialog المتزامن

الخدمة تحتوي `Current` واحدًا و`TaskCompletionSource` واحدًا. إذا فتحت Dialog جديدًا قبل إغلاق الحالي:

```text
الحالي → Cancelled
الجديد → يصبح Current
```

لا يوجد Dialog stack متعدد حاليًا.

---

# 13. النماذج Core Models

## `UiDataColumn<TItem>`

```csharp
record UiDataColumn<TItem>(string Header, Func<TItem,string?> ValueSelector)
```

تعريف عمود نصي في UiDataTable.

## `UiSelectOption`

```csharp
record UiSelectOption(string Value, string Text)
```

## `UiSnackbarMessage`

يحمل:

```text
Id
Message
Tone
DurationMilliseconds
AutoClose
```

## `UiSnackbarOptions`

إعدادات Host العامة.

## `UiDialogOptions`

سلوك وحجم Dialog.

## `UiDialogRequest`

الحالة الداخلية التي يقرأها Host، وتشمل:

```text
Id
Title
ComponentType
Parameters
Message
IsConfirm
ConfirmText
CancelText
Tone
Options
```

## `UiDialogResult`

```csharp
record UiDialogResult(bool Cancelled, object? Value)
```

Helpers:

```csharp
UiDialogResult.Confirmed(value)
UiDialogResult.Canceled()
```

---

# 14. أمثلة مركبة من المشروع الفعلي

## 14.1 بناء Login باستخدام UiLib فقط

الصفحة الحالية تستخدم:

```text
UiPage
└── UiCard
    └── UiGrid (عمودان)
        ├── UiForm
        │   └── UiStackLayout
        │       ├── UiGrid
        │       │   ├── UiSelect
        │       │   └── UiStatusAction
        │       ├── UiInputText (username)
        │       ├── UiInputText (password)
        │       ├── UiCheckbox
        │       └── UiButton
        └── UiPictureBox
```

مثال مقتبس من البنية الفعلية:

```razor
<UiPage Placement="PagePlacement.Center">
    <UiCard Width="SurfaceWidth.Medium">
        <UiGrid Columns="2">
            <UiForm Model="_model" OnSubmit="LoginAsync">
                <UiStackLayout Gap="StackGap.Small">
                    <UiGrid Columns="2">
                        <UiSelect @bind-Value="_selectedProfileKey"
                                  Label="@L[\"Login_Database\"]"
                                  Options="_profileOptions"
                                  Size="ControlSize.Small" />
                        <UiStatusAction Text="@L[\"Login_UpdateDatabase\"]"
                                        Tone="@DatabaseStatusTone"
                                        AlignToLabeledControl="true"
                                        Size="ControlSize.Small" />
                    </UiGrid>
                    <UiInputText @bind-Value="_model.Login"
                                 Label="@L[\"Login_UserNameOrEmail\"]"
                                 Size="ControlSize.Small" />
                    <UiInputText @bind-Value="_model.Password"
                                 Type="password"
                                 RevealPassword="true"
                                 Size="ControlSize.Small" />
                    <UiCheckbox @bind-Value="_model.RememberMe"
                                Label="@L[\"Login_RememberMe\"]" />
                    <UiButton Type="submit"
                              Text="@L[\"Login_Button\"]"
                              FullWidth="true"
                              Size="ControlSize.Small" />
                </UiStackLayout>
            </UiForm>
            <UiPictureBox Alt="@L[\"Login_SystemImage\"]" FallbackText="OAS" />
        </UiGrid>
    </UiCard>
</UiPage>
```

هذا يوضح القاعدة: صفحة Client تركب Components، بينما HTML/CSS الحقيقي داخل UiLib.

## 14.2 Users + DataTable

يتم أولًا جلب البيانات من Client service، ثم بناء Columns:

```csharp
_users = await UserService.GetUsersAsync();

_columns =
[
    new(L["Users_UserName"], x => x.UserName),
    new(L["Users_Name"], x => x.DisplayName),
    new(L["Users_Email"], x => x.Email ?? "—")
];
```

ثم:

```razor
<UiDataTable TItem="UserDto"
             Items="_users"
             Columns="_columns"
             EmptyText="@L[\"Users_Empty\"]" />
```

المكون لا يجلب API بنفسه. **جلب البيانات مسؤولية Client Feature/Service**، ثم تمرر البيانات للمكون.

---

# 15. كيف تتحكم بحالة المكونات

قاعدة Blazor المستخدمة في UiLib هي: الحالة تكون في الصفحة/Feature، والمكون يستقبلها Parameters.

مثال زر تحميل:

```csharp
private bool _saving;

private async Task SaveAsync()
{
    if (_saving) return;
    _saving = true;
    try
    {
        await Service.SaveAsync();
    }
    finally
    {
        _saving = false;
    }
}
```

```razor
<UiButton Text="حفظ"
          Loading="_saving"
          Disabled="_saving"
          OnClick="SaveAsync" />
```

مثال تعطيل Inputs حسب حالة النظام:

```razor
<UiInputText @bind-Value="_model.Name"
             Disabled="@_databaseNeedsUpdate" />
```

المكون لا يقرر قواعد Business؛ الصفحة هي التي تحسب الحالة ثم تمررها.

---

# 16. RTL / LTR

المكتبة تستخدم خصائص CSS منطقية في عدة أماكن، مثل:

```css
padding-inline
padding-inline-end
inset-inline-end
margin-inline
text-align: start
```

وهذا يجعلها أكثر توافقًا مع RTL/LTR من استخدام `left/right` دائمًا.

اتجاه الصفحة نفسه يحدد حاليًا من `OAS.API/Components/App.razor`:

```razor
<html lang="@defaultCulture"
      dir="@(defaultCulture.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "rtl" : "ltr")">
```

هناك بعض عناصر positioning العامة مثل Snackbar positions تعتمد صراحةً على left/right لأنها تمثل موقعًا محددًا على الشاشة (`TopLeft`, `TopRight`) وليس اتجاهًا لغويًا.

---

# 17. Accessibility

الموجود حاليًا:

- `UiButton`: `aria-busy` أثناء Loading.
- `UiInputText`: `aria-required` + labels مرتبطة بـID.
- Password reveal: `aria-label`, `aria-pressed`.
- `UiPictureBox`: Alt أو fallback `role=img`.
- `UiAlert`: `role=alert`.
- Snackbar: `aria-live=polite`, Danger=`role=alert`.
- Dialog: `role=dialog`, `aria-modal=true`, `aria-label=Title`.
- `UiStatusAction`: `aria-label=StatusText`.
- App shell nav: `aria-label=L["PrimaryNavigation"]`.

### نقاط يمكن تحسينها مستقبلًا

- Focus trapping داخل Dialog غير موجود حاليًا.
- نقل focus تلقائيًا إلى Dialog عند الفتح غير موجود.
- إعادة focus للعنصر الذي فتح Dialog غير موجود.
- UiCheckbox لا يملك API لـaria-describedby أو validation linkage.
- UiDataTable لا يدعم captions/column scopes مخصصة أكثر من الوضع الحالي.

---

# 18. Bootstrap Loader داخل UiLib

الملف:

```text
wwwroot/css/bootstrap-loader.css
```

موجود داخل UiLib لأن تنسيق شاشة التهيئة مركزي. لكن الـmarkup والJavaScript موجودان في:

```text
OAS.API/Components/App.razor
```

وليس كـBlazor Component، لأن شاشة التحميل تظهر **قبل اكتمال تشغيل Blazor Components**.

الـCSS يعرّف:

```text
.oas-boot-screen
.oas-boot-card
.oas-boot-ring
.oas-boot-ring__segment
.oas-boot-percentage
.oas-boot-message
.oas-boot-error
.oas-boot-retry
```

الحلقة الحالية تحتوي 36 Segment، ويقوم JavaScript بتفعيلها حسب تقدم تحميل موارد Blazor.

> هذه حالة خاصة: UI التطبيق بعد تشغيل Blazor يعتمد على Components، لكن Boot shell قبل Blazor لا يمكنه استخدام Razor Components التي لم تُحمّل بعد.

---

# 19. قواعد إضافة Component جديد

## 19.1 اختر التصنيف الصحيح

مثال `UiDatePicker`:

```text
Components/Inputs/
```

ولا تنشئ مجلد Business Feature داخل UiLib.

## 19.2 أنشئ الملفات الثلاثة

```text
UiDatePicker.razor
UiDatePicker.razor.cs
UiDatePicker.razor.css
```

## 19.3 Parameters عامة فقط

خطأ:

```csharp
[Parameter] public InvoiceDto Invoice { get; set; } // ❌ UiLib عرفت Business Contract
```

صحيح:

```csharp
[Parameter] public DateTime? Value { get; set; }
[Parameter] public EventCallback<DateTime?> ValueChanged { get; set; }
```

## 19.4 استخدم Tokens

بدل:

```css
padding: 13px;
color: #2457d6;
```

استخدم:

```css
padding: var(--oas-space-3);
color: var(--oas-color-primary);
```

## 19.5 CSS Isolation

تنسيق المكون يكون في `.razor.css`. لا تضف CSS خاصًا بالمكون إلى `colors.css` أو `sizing.css`.

## 19.6 الترجمة

إذا المكون لديه نص داخلي ثابت مثل Loading/Close، أضف Resource Key إلى ملفي الموارد واستخدم `L[...]`.

أما النصوص التجارية مثل "رقم الفاتورة" فتبقى في Client resources ويتم تمريرها للمكون كقيمة.

## 19.7 Accessibility

أي عنصر تفاعلي جديد يجب مراجعة:

```text
label / aria-label
keyboard behavior
focus
role
aria-expanded / aria-pressed عند الحاجة
```

---

# 20. قواعد الاستخدام داخل OAS.Client

1. لا تنشئ CSS خاصًا بالصفحة إذا كان الشكل قابلًا للتعبير عنه بمكونات UiLib.
2. لا تضع Business logic داخل UiLib.
3. جلب API يتم عبر Client Services، وليس داخل المكونات العامة.
4. الترجمة التجارية من `OAS.Client.SharedResources`.
5. الترجمة الداخلية العامة للمكونات من `UiLibSharedResources`.
6. استخدم `ControlSize.Small` للشاشات الكثيفة عند الحاجة.
7. ضع `UiSnackbarHost` و`UiDialogHost` مرة واحدة فقط.
8. لا تستخدم `UiDataTable` على أنها DataGrid متكاملة؛ راجع قيودها قبل التصميم.

---

# 21. الأخطاء الشائعة

## Snackbar service يعمل لكن لا تظهر الرسالة

تحقق من وجود:

```razor
<UiSnackbarHost />
```

مرة واحدة في شجرة render.

## Dialog لا يظهر

تحقق من:

```razor
<UiDialogHost />
```

ومن تسجيل:

```csharp
services.AddOasUiLib();
```

## UiInputText يعطي مشاكل Binding

استخدم عادة:

```razor
@bind-Value="_model.Property"
```

ليتم إنشاء `ValueExpression` المطلوب لـInputBase تلقائيًا.

## Select مع Guid

UiSelect يقبل string، لذلك حول Guid إلى string في options ثم `Guid.Parse/TryParse` عند الاستعمال.

## زر لا ينفذ OnClick

تحقق من:

```text
Disabled
Loading
```

كلاهما يمنعان استدعاء callback.

## Static CSS لا يظهر

تحقق من Static Web Assets ومسارات `_content/OAS.UiLib/...` في Host.

---

# 22. القيود الحالية الملخصة

| الجزء | القيد الحالي |
|---|---|
| UiDataTable | عرض نصي فقط؛ لا Sorting/Paging/Templates |
| UiSelect | قيمة string فقط؛ لا Search/Multi-select |
| UiCheckbox | لا يرث InputBase ولا Validation integration كامل |
| UiForm | OnSubmit فقط؛ لا OnValidSubmit API |
| UiNavLink | نص فقط؛ لا Icon/Badge/ChildContent |
| UiAppShell | Header علوي؛ لا Sidebar/Workspace Tabs حاليًا |
| UiDialogService | Dialog واحد فقط؛ لا stack |
| UiDialog | لا Focus trap/auto-focus management |
| UiPictureBox | Styling ثابت نسبيًا؛ لا fit/height parameters |
| UiInputText | string فقط؛ لا Prefix/Suffix/Debounce |
| Translation | بعض المكونات تدعم ResourceKey وبعضها يأخذ النص جاهزًا فقط |

هذه ليست أخطاء بالضرورة؛ هي حدود النسخة الحالية ويجب معرفتها قبل بناء شاشة تعتمد عليها.

---

# 23. Quick Reference

| المكون | الاستخدام الرئيسي | Binding/Event الرئيسي |
|---|---|---|
| `UiButton` | زر | `OnClick`, `Type=submit` |
| `UiTextLabel` | Label/Text | `Text`, `ChildContent` |
| `UiInputText` | إدخال نص | `@bind-Value` |
| `UiCheckbox` | Boolean | `@bind-Value` |
| `UiSelect` | قائمة string | `@bind-Value`, `OnValueChanged` |
| `UiForm` | Form wrapper | `Model`, `OnSubmit` |
| `UiCard` | Surface | `ChildContent` |
| `UiPage` | Page container | `Actions`, `ChildContent` |
| `UiGrid` | Grid 1–4 columns | `Columns`, `Ratio` |
| `UiStackLayout` | Vertical stack | `Gap` |
| `UiAppShell` | App shell | Navigation/Actions/ChildContent |
| `UiNavLink` | Navigation | `Href`, `Match` |
| `UiPictureBox` | صورة/Fallback | `Src` |
| `UiAlert` | Inline feedback | `Message`, `Tone` |
| `UiStatusAction` | Status + Button | `Tone`, `OnClick` |
| `UiDataTable<T>` | جدول نصي | `Items`, `Columns` |
| `UiSnackbarHost` | Renderer Snackbar | Service-driven |
| `UiDialog` | Dialog surface | Slots/OnClose |
| `UiConfirmDialog` | Confirm component | Confirm/Cancel events |
| `UiDialogHost` | Renderer Dialog service | Service-driven |

---

# 24. خريطة الملفات الكاملة للمكتبة

القائمة التالية توثق كل ملف موجود في `Shared/UiLib` في النسخة التي تمت مراجعتها:

- `Components/Buttons/UiButton.razor` — Razor markup للمكون.
- `Components/Buttons/UiButton.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Buttons/UiButton.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Data/UiDataTable.razor` — Razor markup للمكون.
- `Components/Data/UiDataTable.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Data/UiDataTable.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Dialogs/UiConfirmDialog.razor` — Razor markup للمكون.
- `Components/Dialogs/UiConfirmDialog.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Dialogs/UiConfirmDialog.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Dialogs/UiDialog.razor` — Razor markup للمكون.
- `Components/Dialogs/UiDialog.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Dialogs/UiDialog.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Dialogs/UiDialogHost.razor` — Razor markup للمكون.
- `Components/Dialogs/UiDialogHost.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Dialogs/UiDialogHost.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Display/UiTextLabel.razor` — Razor markup للمكون.
- `Components/Display/UiTextLabel.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Display/UiTextLabel.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Feedback/UiAlert.razor` — Razor markup للمكون.
- `Components/Feedback/UiAlert.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Feedback/UiAlert.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Feedback/UiSnackbarHost.razor` — Razor markup للمكون.
- `Components/Feedback/UiSnackbarHost.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Feedback/UiSnackbarHost.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Feedback/UiStatusAction.razor` — Razor markup للمكون.
- `Components/Feedback/UiStatusAction.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Feedback/UiStatusAction.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Forms/UiForm.razor` — Razor markup للمكون.
- `Components/Forms/UiForm.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Forms/UiForm.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Inputs/UiCheckbox.razor` — Razor markup للمكون.
- `Components/Inputs/UiCheckbox.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Inputs/UiCheckbox.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Inputs/UiInputText.razor` — Razor markup للمكون.
- `Components/Inputs/UiInputText.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Inputs/UiInputText.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Inputs/UiSelect.razor` — Razor markup للمكون.
- `Components/Inputs/UiSelect.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Inputs/UiSelect.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Layout/UiAppShell.razor` — Razor markup للمكون.
- `Components/Layout/UiAppShell.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Layout/UiAppShell.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Layout/UiGrid.razor` — Razor markup للمكون.
- `Components/Layout/UiGrid.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Layout/UiGrid.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Layout/UiPage.razor` — Razor markup للمكون.
- `Components/Layout/UiPage.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Layout/UiPage.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Layout/UiStackLayout.razor` — Razor markup للمكون.
- `Components/Layout/UiStackLayout.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Layout/UiStackLayout.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Media/UiPictureBox.razor` — Razor markup للمكون.
- `Components/Media/UiPictureBox.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Media/UiPictureBox.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Navigation/UiNavLink.razor` — Razor markup للمكون.
- `Components/Navigation/UiNavLink.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Navigation/UiNavLink.razor.css` — CSS Isolation الخاص بالمكون.
- `Components/Surfaces/UiCard.razor` — Razor markup للمكون.
- `Components/Surfaces/UiCard.razor.cs` — Code-behind: Parameters/logic/state للمكون.
- `Components/Surfaces/UiCard.razor.css` — CSS Isolation الخاص بالمكون.
- `Core/Enums/AlertTone.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Enums/ButtonVariant.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Enums/ControlSize.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Enums/GridRatio.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Enums/PagePlacement.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Enums/StackGap.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Enums/SurfaceWidth.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Enums/TextTone.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Enums/TextWeight.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Enums/UiDialogSize.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Enums/UiSnackbarPosition.cs` — Enum عام للتحكم في سلوك/مظهر UiLib.
- `Core/Models/.gitkeep` — ملف للحفاظ على المجلد داخل Git.
- `Core/Models/UiDataColumn.cs` — Model/record عام تستخدمه المكونات أو الخدمات.
- `Core/Models/UiDialogOptions.cs` — Model/record عام تستخدمه المكونات أو الخدمات.
- `Core/Models/UiDialogRequest.cs` — Model/record عام تستخدمه المكونات أو الخدمات.
- `Core/Models/UiDialogResult.cs` — Model/record عام تستخدمه المكونات أو الخدمات.
- `Core/Models/UiSelectOption.cs` — Model/record عام تستخدمه المكونات أو الخدمات.
- `Core/Models/UiSnackbarMessage.cs` — Model/record عام تستخدمه المكونات أو الخدمات.
- `Core/Models/UiSnackbarOptions.cs` — Model/record عام تستخدمه المكونات أو الخدمات.
- `Extensions/ServiceCollectionExtensions.cs` — تسجيل خدمات UiLib في Dependency Injection.
- `Localization/UiLibSharedResources.cs` — Marker class لموارد الترجمة.
- `OAS.UiLib.csproj` — تعريف مشروع Razor Class Library والحزم.
- `Resources/Localization.UiLibSharedResources.ar.resx` — ملف موارد ترجمة.
- `Resources/Localization.UiLibSharedResources.resx` — ملف موارد ترجمة.
- `Services/Dialogs/IUiDialogService.cs` — Service أو interface لخدمة UI عامة.
- `Services/Dialogs/UiDialogService.cs` — Service أو interface لخدمة UI عامة.
- `Services/Feedback/IUiSnackbarService.cs` — Service أو interface لخدمة UI عامة.
- `Services/Feedback/UiSnackbarService.cs` — Service أو interface لخدمة UI عامة.
- `_Imports.razor` — Imports مشتركة وحقن `IStringLocalizer<UiLibSharedResources>` لكل مكونات المكتبة.
- `wwwroot/css/oas-ui-bundle.css` — نقطة الدخول الرئيسية لكل CSS في UiLib؛ تجمع Bootstrap وFont Awesome وDesign Tokens وCSS Isolation.
- `wwwroot/css/bootstrap-loader.css` — تنسيق شاشة تهيئة النظام قبل اكتمال Blazor.
- `wwwroot/css/colors.css` — Design Tokens الخاصة بالألوان.
- `wwwroot/css/sizing.css` — Design Tokens الخاصة بالأحجام.
- `wwwroot/css/variables.css` — Design Tokens العامة للمسافات والزوايا والقياسات.
- `wwwroot/lib/bootstrap/` — نسخة Bootstrap المحلية؛ CSS مستخدم داخل الباندل وJavaScript محفوظ للاستخدام الاختياري مستقبلًا.
- `wwwroot/lib/font-awesome/` — Font Awesome Free محليًا، ويتضمن CSS والخطوط webfonts والتراخيص.
- `wwwroot/lib/THIRD_PARTY_NOTICES.md` — بيان بالمكتبات الخارجية وإصداراتها وطريقة تحديثها.
- `tools/install-ui-libraries.ps1` — سكربت تحديث Bootstrap وFont Awesome من المصادر الرسمية.

---

# 25. خلاصة تدقيق النسخة الحالية

بعد مراجعة كل ملفات `OAS.UiLib` واستخداماتها الحالية داخل النظام:

- المكتبة مستقلة فعليًا عن طبقات Business.
- فصل CSS لكل Component مطبق على المكونات العشرين.
- Translation foundation موجود ويعمل داخليًا عبر `L`.
- Snackbar وDialog مبنيان كخدمات عامة وليسا Business-specific.
- مكونات الإدخال تختلف في مستوى التكامل مع `EditContext`: `UiInputText/UiSelect` أقوى من `UiCheckbox` حاليًا.
- `UiDataTable` مكون عرض بسيط، وليس DataGrid مكتملة.
- `UiAppShell` الحالية ما زالت Shell بسيطة قبل تنفيذ تصميم Sidebar/Workspace الأكثر تقدمًا.
- Design Tokens مركزية، لكن اللون Primary الحالي أزرق في الكود الفعلي.
- Boot loader CSS داخل UiLib، لكن تشغيله يتم قبل Blazor من `App.razor` في API.

هذه الوثيقة تصف **ما هو موجود فعليًا** في النسخة المراجعة، ولا تفترض Features مستقبلية غير منفذة.

---

## 26. قاعدة صيانة هذه الوثيقة

عند إضافة أو تعديل Component داخل `OAS.UiLib` يجب تحديث هذه الوثيقة في نفس Pull Request/Commit، وبالحد الأدنى:

1. تحديث جدول Parameters.
2. تحديث المثال العملي.
3. توثيق أي Enum/Model جديد.
4. توثيق أي Resource Key جديد.
5. توثيق أي Design Token جديد.
6. إضافة الملف إلى خريطة الملفات.
7. توثيق القيود والسلوكيات غير البديهية.

بهذه الطريقة تبقى UiLib قابلة للاستخدام من أي مطور بدون الحاجة إلى قراءة المصدر كاملًا في كل مرة.
