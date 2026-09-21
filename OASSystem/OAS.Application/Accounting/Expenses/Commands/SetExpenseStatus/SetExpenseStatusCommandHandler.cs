using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainExpenseStatus = OAS.Domain.Accounting.Enums.ExpenseStatus;

namespace OAS.Application.Accounting.Expenses.Commands.SetExpenseStatus;

public sealed class SetExpenseStatusCommandHandler(
    IRepository<Expense, Guid> repository,
    IAccountingDocumentPostingService postingService,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<SetExpenseStatusCommand>
{
    public async Task Handle(
        SetExpenseStatusCommand request,
        CancellationToken cancellationToken)
    {
        var expense = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (expense is null)
            throw new NotFoundException(nameof(Expense), request.Id);

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!expense.RowVersion.SequenceEqual(requestedRowVersion))
            throw new ConcurrencyException("The expense has been modified by another user.");

        var targetStatus = (DomainExpenseStatus)(int)request.Request.Status;

        switch (targetStatus)
        {
            case DomainExpenseStatus.Approved:
                if (!await permissionChecker.HasPermissionAsync(AccountingPermissions.Expenses.Approve, cancellationToken))
                    throw new ForbiddenException();
                expense.Approve();
                break;

            case DomainExpenseStatus.Posted:
                if (!await permissionChecker.HasPermissionAsync(AccountingPermissions.Expenses.Post, cancellationToken))
                    throw new ForbiddenException();
                if (!Guid.TryParse(currentUser.UserId, out var userId))
                    throw new ForbiddenException();

                var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
                var journalId = await postingService.PostExpenseAsync(
                    expense, userId, nowUtc, cancellationToken);

                expense.SetJournalEntry(journalId);
                expense.Post(nowUtc);
                break;

            case DomainExpenseStatus.Cancelled:
                expense.Cancel();
                break;

            default:
                throw new InvalidOperationException(
                    $"Expense status '{targetStatus}' is not supported for manual transition.");
        }

        repository.Update(expense);
    }
}
