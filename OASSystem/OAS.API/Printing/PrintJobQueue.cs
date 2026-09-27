using System.Collections.Concurrent;
using OAS.Contracts.Printing;

namespace OAS.API.Printing;

public sealed class PrintJobQueue(TimeProvider timeProvider) : IPrintJobQueue
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(45);
    private readonly ConcurrentDictionary<Guid, QueueEntry> _jobs = new();

    public PrintJobCreatedDto Enqueue(string workstationCode, DesktopPrintJobDto job)
    {
        var now = timeProvider.GetUtcNow();
        var normalized = NormalizeWorkstation(workstationCode);
        var entry = new QueueEntry(normalized, job, now);

        if (!_jobs.TryAdd(job.JobId, entry))
            throw new InvalidOperationException("تعذر إضافة مهمة الطباعة.");

        return new PrintJobCreatedDto(job.JobId, normalized, now);
    }

    public DesktopPrintJobDto? TryLeaseNext(string workstationCode)
    {
        var normalized = NormalizeWorkstation(workstationCode);
        var now = timeProvider.GetUtcNow();

        foreach (var pair in _jobs
                     .Where(x => string.Equals(x.Value.WorkstationCode, normalized, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(x => x.Value.QueuedAtUtc))
        {
            var entry = pair.Value;
            lock (entry.SyncRoot)
            {
                if (entry.Completed || entry.Failed)
                    continue;

                if (entry.LeasedUntilUtc is not null && entry.LeasedUntilUtc > now)
                    continue;

                entry.LeasedUntilUtc = now.Add(LeaseDuration);
                entry.AttemptCount++;
                return entry.Job;
            }
        }

        return null;
    }

    public bool Complete(Guid jobId)
    {
        if (!_jobs.TryRemove(jobId, out var entry))
            return false;

        lock (entry.SyncRoot)
            entry.Completed = true;

        return true;
    }

    public bool Fail(Guid jobId, string? error)
    {
        if (!_jobs.TryRemove(jobId, out var entry))
            return false;

        lock (entry.SyncRoot)
        {
            entry.Failed = true;
            entry.LastError = error;
        }

        return true;
    }

    private static string NormalizeWorkstation(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "DEFAULT" : value.Trim();

    private sealed class QueueEntry(string workstationCode, DesktopPrintJobDto job, DateTimeOffset queuedAtUtc)
    {
        public object SyncRoot { get; } = new();
        public string WorkstationCode { get; } = workstationCode;
        public DesktopPrintJobDto Job { get; } = job;
        public DateTimeOffset QueuedAtUtc { get; } = queuedAtUtc;
        public DateTimeOffset? LeasedUntilUtc { get; set; }
        public int AttemptCount { get; set; }
        public bool Completed { get; set; }
        public bool Failed { get; set; }
        public string? LastError { get; set; }
    }
}
