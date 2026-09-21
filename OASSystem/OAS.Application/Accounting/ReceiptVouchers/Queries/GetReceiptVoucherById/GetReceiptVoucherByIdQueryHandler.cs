using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.ReceiptVouchers.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVoucherById;

public sealed class GetReceiptVoucherByIdQueryHandler(
    IReadRepository<ReceiptVoucher, Guid> repository,
    IReadRepository<ReceiptVoucherLine, Guid> lineRepository,
    ReceiptVoucherMapper mapper)
    : IRequestHandler<GetReceiptVoucherByIdQuery, ReceiptVoucherDto>
{
    public async Task<ReceiptVoucherDto> Handle(
        GetReceiptVoucherByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            throw new NotFoundException(nameof(ReceiptVoucher), request.Id);

        var lines = await lineRepository.ListAsync(
            new Specification<ReceiptVoucherLine>()
                .Where(x => x.ReceiptVoucherId == request.Id)
                .AddSort(nameof(ReceiptVoucherLine.LineNumber), OAS.Contracts.Common.Pagination.SortDirection.Ascending),
            cancellationToken);

        return mapper.ToRead(entity, lines);
    }
}
