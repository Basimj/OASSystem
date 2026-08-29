namespace OAS.Application.Abstractions.Messaging;

public interface IAuthorizedRequest
{
    IReadOnlyCollection<string> RequiredPermissions { get; }
}
