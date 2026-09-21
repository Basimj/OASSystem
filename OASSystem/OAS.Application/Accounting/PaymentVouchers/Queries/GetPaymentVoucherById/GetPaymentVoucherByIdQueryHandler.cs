using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.PaymentVouchers.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById;

public sealed class GetPaymentVoucherByIdQueryHandler(
    IReadRepository<PaymentVoucher, Guid> repository,
    PaymentVoucherMapper mapper)
    : IRequestHandler<GetPaymentVoucherByIdQuery, PaymentVoucherDto>
{
    public async Task<PaymentVoucherDto> Handle(
        GetPaymentVoucherByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(PaymentVoucher), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
