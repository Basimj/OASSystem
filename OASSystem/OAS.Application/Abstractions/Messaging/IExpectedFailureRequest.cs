namespace OAS.Application.Abstractions.Messaging;

/// <summary>
/// Allows pipeline behaviors to return an expected failure without throwing an exception.
/// </summary>
public interface IExpectedFailureRequest<out TResponse>
{
    TResponse Forbidden(string errorCode);
}
