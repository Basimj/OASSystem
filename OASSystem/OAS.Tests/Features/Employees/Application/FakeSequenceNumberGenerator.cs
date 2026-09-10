using OAS.Application.Abstractions.Numbering;

namespace OAS.Tests.Features.Employees.Application;

public sealed class FakeSequenceNumberGenerator(long start = 100) : ISequenceNumberGenerator
{
    private long _current = start - 1;

    public Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(sequenceName, "EmployeeNumberSequence", StringComparison.Ordinal))
            throw new InvalidOperationException($"Unexpected sequence: {sequenceName}");

        return Task.FromResult(Interlocked.Increment(ref _current));
    }
}
