
namespace OAS.Application.Abstractions.Numbering;

public interface ISequenceNumberGenerator
{
    Task<long> NextAsync(
        string sequenceName,
        CancellationToken cancellationToken = default);
}
