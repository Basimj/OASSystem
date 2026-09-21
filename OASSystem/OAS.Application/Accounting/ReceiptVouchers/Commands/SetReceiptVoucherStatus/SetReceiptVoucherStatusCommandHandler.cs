using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Domain.Accounting.Entities;
using DomainReceiptVoucherStatus = OAS.Domain.Accounting.Enums.ReceiptVoucherStatus;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.SetReceiptVoucherStatus;

public sealed class SetReceiptVoucherStatusCommandHandler(
    IRepository<ReceiptVoucher, Guid> repository,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<SetReceiptVoucherStatusCommand>
{
    public async Task Handle(
        SetReceiptVoucherStatusCommand request,
        CancellationToken cancellationToken)
    {
        var voucher = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (voucher is null)
        {
            throw new NotFoundException(nameof(ReceiptVoucher), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!voucher.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The receipt voucher has been modified by another user.");
        }

        var targetStatus = (DomainReceiptVoucherStatus)(int)request.Request.Status;

        switch (targetStatus)
        {
            case DomainReceiptVoucherStatus.Approved:
                if (!await permissionChecker.HasPermissionAsync(AccountingPermissions.ReceiptVouchers.Approve, cancellationToken))
                    throw new ForbiddenException();
                voucher.Approve();
                break;

            case DomainReceiptVoucherStatus.Posted:
                if (!await permissionChecker.HasPermissionAsync(AccountingPermissions.ReceiptVouchers.Post, cancellationToken))
                    throw new ForbiddenException();
                if (!Guid.TryParse(currentUser.UserId, out var userId))
                    throw new ForbiddenException();
                voucher.Post(userId, timeProvider.GetUtcNow().UtcDateTime);
                break;

            case DomainReceiptVoucherStatus.Cancelled:
                voucher.Cancel();
                break;

            default:
                throw new InvalidOperationException($"Receipt voucher status '{targetStatus}' is not supported for manual transition.");
        }

        repository.Update(voucher);
    }
}
