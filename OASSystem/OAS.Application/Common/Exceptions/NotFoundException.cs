namespace OAS.Application.Common.Exceptions;

public sealed class NotFoundException(string resourceName, object key)
    : Exception($"{resourceName} with key '{key}' was not found.");
