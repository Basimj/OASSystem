using OAS.Application.Abstractions.Numbering;

namespace OAS.Tests.Inventory.Fakes;

public sealed class FakeInventorySequenceNumberGenerator(long start = 1) : ISequenceNumberGenerator
{
    private long _current = start - 1;

    public Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Interlocked.Increment(ref _current));
    }
}
