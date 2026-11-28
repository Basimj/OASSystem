using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

// Operational grouping; financial balances remain on individual CashAccounts.
public sealed class CashBox : Entity<Guid>
{
    private CashBox() { }
    internal CashBox(Guid id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }
    internal void Rename(string name) => Name = name;
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
}
