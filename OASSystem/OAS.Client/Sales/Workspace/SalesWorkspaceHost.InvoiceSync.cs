using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Sales.Mapping;
using OAS.Client.Services.Http;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.UiLib.Core.Models.Sales;

namespace OAS.Client.Sales.Workspace;

public partial class SalesWorkspaceHost
{
    private bool _isConfirmingInvoice;

    // Header-discount operations change row-version and recalculate document lines/totals.
    // Replace the open model with the authoritative DTO returned by the mutation.
    private Task InvoiceHeaderDiscountUpdatedAsync(SalesInvoiceDto dto)
    {
        var tab = ActiveTab;
        if (tab is null || tab.EntityType != SalesEntityType.SalesInvoices ||
            tab.EntityId != dto.Id)
            return Task.CompletedTask;

        if (tab.IsDirty || tab.IsEditMode)
        {
            Snackbar.Warning("تم تعديل الفاتورة في الخادم. احفظ أو ألغِ التعديلات المحلية، ثم حدّث المستند قبل التأكيد.");
            return Task.CompletedTask;
        }

        tab.CompleteSave(dto.Id, dto.InvoiceCode, SalesUiMapper.ToUi(dto));
        Workspace.NotifyStateChanged();
        return Task.CompletedTask;
    }

    private Task OrderHeaderDiscountUpdatedAsync(CustomerOrderDto dto)
    {
        var tab = ActiveTab;
        if (tab is null || tab.EntityType != SalesEntityType.CustomerOrders ||
            tab.EntityId != dto.Id)
            return Task.CompletedTask;

        if (tab.IsDirty || tab.IsEditMode)
        {
            Snackbar.Warning("تغير الطلب في الخادم. احفظ أو ألغِ التعديلات المحلية، ثم حدّث المستند.");
            return Task.CompletedTask;
        }

        tab.CompleteSave(dto.Id, dto.OrderCode, SalesUiMapper.ToUi(dto));
        Workspace.NotifyStateChanged();
        return Task.CompletedTask;
    }

    // Safety net for external concurrency changes: never silently confirm a version
    // whose contents the user has not seen. Discount approval itself is synchronized
    // immediately above, so the normal flow proceeds without closing the invoice.
    private async Task ConfirmInvoiceCurrentAsync(MouseEventArgs args)
    {
        if (_isConfirmingInvoice) return;
        if (ActiveTab is not { } tab ||
            tab.Model is not UiSalesInvoiceFormModel model ||
            !model.Id.HasValue || !EnsureSaved(tab)) return;

        _isConfirmingInvoice = true;
        try
        {
            var latest = await Sales.GetSalesInvoiceByIdAsync(model.Id.Value);
            if (latest is null)
            {
                Snackbar.Error("تعذر قراءة الفاتورة الحالية. حدّث القائمة وأعد المحاولة.");
                return;
            }
            if (!ReferenceEquals(ActiveTab, tab)) return;

            if (!string.Equals(model.RowVersion, latest.RowVersion, StringComparison.Ordinal))
            {
                tab.CompleteSave(latest.Id, latest.InvoiceCode, SalesUiMapper.ToUi(latest));
                Snackbar.Warning("تغيرت بيانات الفاتورة أثناء فتحها، وتم تحديثها تلقائيًا. راجع القيم ثم اضغط «تأكيد».");
                return;
            }

            await ConfirmInvoiceAsync(args);
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(ex.Error);
        }
        catch
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            _isConfirmingInvoice = false;
            Workspace.NotifyStateChanged();
        }
    }
}
