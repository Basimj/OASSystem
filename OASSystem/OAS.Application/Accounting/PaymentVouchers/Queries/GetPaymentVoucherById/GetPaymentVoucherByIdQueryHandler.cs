using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.PaymentVouchers.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById;

public sealed class GetPaymentVoucherByIdQueryHandler(
    IReadRepository<PaymentVoucher, Guid> repository,
    IReadRepository<PaymentVoucherLine, Guid> lineRepository,
    PaymentVoucherMapper mapper)
    : IRequestHandler<GetPaymentVoucherByIdQuery, PaymentVoucherDto>
{
    public async Task<PaymentVoucherDto> Handle(
        GetPaymentVoucherByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            throw new NotFoundException(nameof(PaymentVoucher), request.Id);

        var lines = await lineRepository.ListAsync(
            new Specification<PaymentVoucherLine>()
                .Where(x => x.PaymentVoucherId == request.Id)
                .AddSort(nameof(PaymentVoucherLine.LineNumber), OAS.Contracts.Common.Pagination.SortDirection.Ascending),
            cancellationToken);

        return mapper.ToRead(entity, lines);
    }
}
