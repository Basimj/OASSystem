namespace OAS.Application.Common.Exceptions;

public sealed class ConcurrencyException(string message, Exception? innerException = null) : Exception(message, innerException);
