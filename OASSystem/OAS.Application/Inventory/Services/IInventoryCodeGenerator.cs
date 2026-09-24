namespace OAS.Application.Inventory.Services;

public interface IInventoryCodeGenerator
{
    Task<string> NextAsync(string kind, CancellationToken cancellationToken = default);
}
