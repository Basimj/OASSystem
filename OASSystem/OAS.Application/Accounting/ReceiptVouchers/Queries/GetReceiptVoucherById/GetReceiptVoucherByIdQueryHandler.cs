using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.ReceiptVouchers.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVoucherById;

public sealed class GetReceiptVoucherByIdQueryHandler(
    IReadRepository<ReceiptVoucher, Guid> repository,
    ReceiptVoucherMapper mapper)
    : IRequestHandler<GetReceiptVoucherByIdQuery, ReceiptVoucherDto>
{
    public async Task<ReceiptVoucherDto> Handle(
        GetReceiptVoucherByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ReceiptVoucher), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
