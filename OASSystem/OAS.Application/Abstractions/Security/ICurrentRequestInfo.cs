
namespace OAS.Application.Abstractions.Security;

public interface ICurrentRequestInfo
{
    string? Device { get; }
}