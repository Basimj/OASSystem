OAS Sales Invoice Printing Fix
===============================

IMPORTANT:
Extract THIS archive directly into the project root — the folder that already contains:
OAS.Client, OAS.API, OAS.Contracts, OAS.Print.Desktop, OAS.Printing.Core, etc.

After extraction verify this exact file exists:
OAS.Print.Desktop\Templates\SalesInvoice-A4-Pro.json

Then CLOSE OAS Print if it is running and rebuild/publish OAS.Print.Desktop.
The project file now copies templates on both Build and Publish.

On next OAS Print startup, TemplateStore seeds/updates the local template cache at:
%LOCALAPPDATA%\OAS\Print\Templates

Expected template:
Name: فاتورة مبيعات احترافية A4
DocumentType: SalesInvoice
Code: SALES-INVOICE-A4-PRO
Version: 2
