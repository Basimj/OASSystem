using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Sales.Services;

public sealed class SalesPostingPeriodService(IReadRepository<FiscalPeriod, Guid> periods) : ISalesPostingPeriodService
{
    public async Task<FiscalPeriod> GetOpenPostingPeriodAsync(
        DateOnly postingDate,
        bool requiresInventory,
        CancellationToken cancellationToken = default)
    {
        var spec = new Specification<FiscalPeriod>()
            .Where(x => x.StartDate <= postingDate && x.EndDate >= postingDate);
        var matches = await periods.ListAsync(spec, cancellationToken);
        var period = matches.OrderBy(x => x.StartDate).FirstOrDefault()
            ?? throw new ConflictException(
                SalesErrorCodes.InvoicePeriodNotFound,
                "لا توجد فترة مالية تغطي تاريخ الترحيل المحدد.");

        if (period.Status == FiscalPeriodStatus.Closed || period.SalesLocked || period.AccountingLocked)
        {
            throw new ConflictException(
                SalesErrorCodes.InvoicePeriodClosed,
                "الفترة المالية مغلقة للمبيعات أو المحاسبة.");
        }

        if (requiresInventory && period.InventoryLocked)
        {
            throw new ConflictException(
                SalesErrorCodes.InvoicePeriodClosed,
                "الفترة المالية مغلقة للمخزون ولا يمكن ترحيل فاتورة تحتوي أصنافًا مخزنية.");
        }

        return period;
    }
}
