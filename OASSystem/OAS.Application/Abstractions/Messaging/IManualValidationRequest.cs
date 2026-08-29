namespace OAS.Application.Abstractions.Messaging;

/// <summary>
/// Marks requests that deliberately convert validation failures to an explicit result instead of exceptions.
/// The handler is responsible for invoking its validator and returning a failure result.
/// </summary>
public interface IManualValidationRequest { }
