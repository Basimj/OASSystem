using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainReceiptVoucherStatus = OAS.Domain.Accounting.Enums.ReceiptVoucherStatus;

namespace OAS.Application.Accounting.ReceiptVouchers.Commands.SetReceiptVoucherStatus;

public sealed class SetReceiptVoucherStatusCommandHandler(
    IRepository<ReceiptVoucher, Guid> repository,
    IReadRepository<ReceiptVoucherLine, Guid> lineRepository,
    IAccountingDocumentPostingService postingService,
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
            throw new NotFoundException(nameof(ReceiptVoucher), request.Id);

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!voucher.RowVersion.SequenceEqual(requestedRowVersion))
            throw new ConcurrencyException("The receipt voucher has been modified by another user.");

        var targetStatus = (DomainReceiptVoucherStatus)(int)request.Request.Status;

        IReadOnlyList<ReceiptVoucherLine>? lines = null;
        if (targetStatus is DomainReceiptVoucherStatus.Approved or DomainReceiptVoucherStatus.Posted)
        {
            lines = await lineRepository.ListAsync(
                new Specification<ReceiptVoucherLine>()
                    .Where(x => x.ReceiptVoucherId == voucher.Id)
                    .AddSort(nameof(ReceiptVoucherLine.LineNumber), OAS.Contracts.Common.Pagination.SortDirection.Ascending),
                cancellationToken);

            ValidateReadyForApproval(voucher, lines);
        }

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

                var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
                var journalId = await postingService.PostReceiptVoucherAsync(
                    voucher, lines!, userId, nowUtc, cancellationToken);

                voucher.SetJournalEntry(journalId);
                voucher.Post(userId, nowUtc);
                break;

            case DomainReceiptVoucherStatus.Cancelled:
                voucher.Cancel();
                break;

            default:
                throw new InvalidOperationException(
                    $"Receipt voucher status '{targetStatus}' is not supported for manual transition.");
        }

        repository.Update(voucher);
    }

    private static void ValidateReadyForApproval(
        ReceiptVoucher voucher,
        IReadOnlyList<ReceiptVoucherLine> lines)
    {
        if (lines.Count == 0)
            throw new ConflictException("voucher_lines_required", "The voucher must contain at least one line before approval or posting.");

        // Legacy vouchers are intentionally allowed during the Expand phase.
        if (!voucher.BaseCurrencyId.HasValue)
            return;

        if (lines.Any(x =>
            !x.CurrencyId.HasValue ||
            !x.CounterpartyAccountId.HasValue ||
            !x.SettlementAccountId.HasValue ||
            !x.BaseAmount.HasValue || x.BaseAmount.Value <= 0 ||
            !x.ExchangeRate.HasValue || x.ExchangeRate.Value <= 0 ||
            !x.ExchangeRateDate.HasValue ||
            !x.ExchangeRateType.HasValue ||
            !x.ExchangeRateSource.HasValue))
        {
            throw new ConflictException(
                "voucher_settlement_line_incomplete",
                "All settlement lines must contain resolved party, settlement, currency and exchange-rate snapshots.");
        }

        var expected = lines.Sum(x => x.BaseAmount!.Value);
        if (!voucher.BaseTotalAmount.HasValue || voucher.BaseTotalAmount.Value != expected)
        {
            throw new ConflictException(
                "voucher_base_total_mismatch",
                "Voucher base total does not match the sum of settlement line base amounts.");
        }
    }
}
