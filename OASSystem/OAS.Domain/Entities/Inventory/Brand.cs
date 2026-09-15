using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class Brand : AuditableEntity<Guid>
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private Brand()
    {
    }

    public Brand(string code, string name)
    {
        Id = Guid.NewGuid();

        Code = code;
        Name = name;
        IsActive = true;
    }
}