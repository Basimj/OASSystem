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

    // A save from either editor updates the other open tab. Never overwrite unsaved
    // local work: the existing concurrency guard will require a reload instead.
    private async Task RefreshOpenLinkedInvoiceTabsAsync(Guid orderId)
    {
        foreach (var tab in Workspace.Tabs.Where(tab =>
                     tab.EntityType == SalesEntityType.SalesInvoices &&
                     tab.Model is UiSalesInvoiceFormModel form &&
                     string.Equals(form.CustomerOrderId, orderId.ToString(), StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            if (tab.IsDirty || tab.IsEditMode)
            {
                Snackbar.Warning("تغيّرت بيانات الفاتورة المرتبطة من طلب العميل. احفظ أو ألغِ التغييرات المحلية وحدّث الفاتورة.");
                continue;
            }
            if (tab.EntityId is not Guid id) continue;
            try
            {
                var fresh = await Sales.GetSalesInvoiceByIdAsync(id);
                if (fresh is not null) tab.CompleteSave(fresh.Id, fresh.InvoiceCode, SalesUiMapper.ToUi(fresh));
            }
            catch (ApiClientException)
            {
                Snackbar.Warning("حُفظ الطلب، لكن تعذر تحديث تبويب الفاتورة المفتوح. حدّثه قبل التأكيد.");
            }
        }
    }

    private async Task RefreshOpenLinkedOrderTabsAsync(Guid? orderId)
    {
        if (!orderId.HasValue) return;
        foreach (var tab in Workspace.Tabs.Where(tab =>
                     tab.EntityType == SalesEntityType.CustomerOrders &&
                     tab.EntityId == orderId).ToArray())
        {
            if (tab.IsDirty || tab.IsEditMode)
            {
                Snackbar.Warning("تغيّرت بيانات الطلب من الفاتورة. احفظ أو ألغِ التغييرات المحلية وحدّث الطلب.");
                continue;
            }
            try
            {
                var fresh = await Sales.GetCustomerOrderByIdAsync(orderId.Value);
                if (fresh is not null) tab.CompleteSave(fresh.Id, fresh.OrderCode, SalesUiMapper.ToUi(fresh));
            }
            catch (ApiClientException)
            {
                Snackbar.Warning("حُفظت الفاتورة، لكن تعذر تحديث تبويب الطلب المفتوح. حدّثه قبل المتابعة.");
            }
        }
    }

    // Header-discount operations change row-version and recalculate document lines/totals.
    // Replace the open model with the authoritative DTO returned by the mutation.
    private async Task InvoiceHeaderDiscountUpdatedAsync(SalesInvoiceDto dto)
    {
        var tab = ActiveTab;
        if (tab is null || tab.EntityType != SalesEntityType.SalesInvoices ||
            tab.EntityId != dto.Id)
            return;

        if (tab.IsDirty || tab.IsEditMode)
        {
            Snackbar.Warning("تم تعديل الفاتورة في الخادم. احفظ أو ألغِ التعديلات المحلية، ثم حدّث المستند قبل التأكيد.");
            return;
        }

        tab.CompleteSave(dto.Id, dto.InvoiceCode, SalesUiMapper.ToUi(dto));
        await RefreshOpenLinkedOrderTabsAsync(dto.CustomerOrderId);
        Workspace.NotifyStateChanged();
    }

    private async Task OrderHeaderDiscountUpdatedAsync(CustomerOrderDto dto)
    {
        var tab = ActiveTab;
        if (tab is null || tab.EntityType != SalesEntityType.CustomerOrders ||
            tab.EntityId != dto.Id)
            return;

        if (tab.IsDirty || tab.IsEditMode)
        {
            Snackbar.Warning("تغير الطلب في الخادم. احفظ أو ألغِ التعديلات المحلية، ثم حدّث المستند.");
            return;
        }

        tab.CompleteSave(dto.Id, dto.OrderCode, SalesUiMapper.ToUi(dto));
        await RefreshOpenLinkedInvoiceTabsAsync(dto.Id);
        Workspace.NotifyStateChanged();
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
