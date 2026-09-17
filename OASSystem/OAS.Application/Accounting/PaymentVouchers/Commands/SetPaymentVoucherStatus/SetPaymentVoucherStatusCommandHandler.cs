using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Domain.Accounting.Entities;
using DomainPaymentVoucherStatus = OAS.Domain.Accounting.Enums.PaymentVoucherStatus;

namespace OAS.Application.Accounting.PaymentVouchers.Commands.SetPaymentVoucherStatus;

public sealed class SetPaymentVoucherStatusCommandHandler(
    IRepository<PaymentVoucher, Guid> repository,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<SetPaymentVoucherStatusCommand>
{
    public async Task Handle(
        SetPaymentVoucherStatusCommand request,
        CancellationToken cancellationToken)
    {
        var voucher = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (voucher is null)
        {
            throw new NotFoundException(nameof(PaymentVoucher), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!voucher.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The payment voucher has been modified by another user.");
        }

        var targetStatus = (DomainPaymentVoucherStatus)(int)request.Request.Status;

        switch (targetStatus)
        {
            case DomainPaymentVoucherStatus.Approved:
                if (!await permissionChecker.HasPermissionAsync(AccountingPermissions.PaymentVouchers.Approve, cancellationToken))
                    throw new ForbiddenException();
                voucher.Approve();
                break;

            case DomainPaymentVoucherStatus.Posted:
                if (!await permissionChecker.HasPermissionAsync(AccountingPermissions.PaymentVouchers.Post, cancellationToken))
                    throw new ForbiddenException();
                if (!Guid.TryParse(currentUser.UserId, out var userId))
                    throw new ForbiddenException();
                voucher.Post(userId, timeProvider.GetUtcNow().UtcDateTime);
                break;

            case DomainPaymentVoucherStatus.Cancelled:
                voucher.Cancel();
                break;

            default:
                throw new InvalidOperationException($"Payment voucher status '{targetStatus}' is not supported for manual transition.");
        }

        repository.Update(voucher);
    }
}
