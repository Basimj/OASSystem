using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Mapping;
using OAS.Application.CRUD.Services;
using OAS.Application.Sales.SalesInvoices.Commands;
using OAS.Application.Sales.SalesInvoices.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.SalesInvoices;

public sealed class SalesInvoiceApplicationService(
    ISender sender,
    ICrudMapper<SalesInvoice, Guid, SalesInvoiceDto, CreateSalesInvoiceRequest, UpdateSalesInvoiceRequest> mapper)
    : CrudApplicationService<SalesInvoice, Guid, SalesInvoiceDto, CreateSalesInvoiceRequest, UpdateSalesInvoiceRequest>(sender, mapper)
{
    public override Task<PagedResult<SalesInvoiceDto>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new GetSalesInvoicesQuery(request), cancellationToken);

    public override Task<SalesInvoiceDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        sender.Send(new GetSalesInvoiceByIdQuery(id), cancellationToken);

    public override async Task<SalesInvoiceDto> CreateAsync(CreateSalesInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await sender.Send(new CreateSalesInvoiceCommand(request), cancellationToken);
        return await sender.Send(new GetSalesInvoiceByIdQuery(entity.Id), cancellationToken);
    }

    public override async Task<SalesInvoiceDto> UpdateAsync(Guid id, UpdateSalesInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await sender.Send(new UpdateSalesInvoiceCommand(id, request), cancellationToken);
        return await sender.Send(new GetSalesInvoiceByIdQuery(entity.Id), cancellationToken);
    }

    public override Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromException(new ConflictException("sales_invoice_delete_forbidden", "لا يتم حذف فاتورة المبيعات حذفًا نهائيًا؛ استخدم الإلغاء قبل الترحيل."));

    public Task<SalesCodeReservationDto> ReserveCodeAsync(DateOnly invoiceDate, CancellationToken cancellationToken = default) =>
        sender.Send(new ReserveSalesInvoiceCodeCommand(invoiceDate), cancellationToken);

    public Task<SalesInvoiceDto> CreateFromOrderAsync(Guid orderId, CreateSalesInvoiceFromOrderRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new CreateSalesInvoiceFromOrderCommand(orderId, request), cancellationToken);

    public Task<SalesInvoiceDto> ConfirmAsync(Guid id, ConfirmSalesInvoiceRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new ConfirmSalesInvoiceCommand(id, request), cancellationToken);

    public Task<SalesInvoicePostingResultDto> PostAsync(Guid id, PostSalesInvoiceRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new PostSalesInvoiceCommand(id, request), cancellationToken);

    public Task<SalesInvoiceDto> CancelAsync(Guid id, CancelSalesInvoiceRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new CancelSalesInvoiceCommand(id, request), cancellationToken);

    public Task<SalesPostingPreValidationDto> PreValidatePostingAsync(Guid id, CancellationToken cancellationToken = default) =>
        sender.Send(new PreValidateSalesInvoicePostingQuery(id), cancellationToken);

    public Task<SalesInvoicePaymentSummaryDto> GetPaymentSummaryAsync(Guid id, CancellationToken cancellationToken = default) =>
        sender.Send(new GetSalesInvoicePaymentSummaryQuery(id), cancellationToken);
}
