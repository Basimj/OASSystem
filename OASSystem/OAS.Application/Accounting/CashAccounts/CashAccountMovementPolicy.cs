using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts;

public static class CashAccountMovementPolicy
{
    public static void EnsureAllowed(CashAccount cash, bool isReceipt)
    {
        if (isReceipt ? !cash.AllowsReceipt : !cash.AllowsPayment)
            throw new ConflictException("cash_account_movement_not_allowed", isReceipt
                ? "الصندوق المحدد مخصص للصرف فقط ولا يسمح بالقبض."
                : "الصندوق المحدد مخصص للقبض فقط ولا يسمح بالصرف.");
    }
}
