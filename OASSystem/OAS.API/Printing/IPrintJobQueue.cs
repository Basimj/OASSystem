using OAS.Contracts.Printing;

namespace OAS.API.Printing;

public interface IPrintJobQueue
{
    PrintJobCreatedDto Enqueue(string workstationCode, DesktopPrintJobDto job);
    DesktopPrintJobDto? TryLeaseNext(string workstationCode);
    bool Complete(Guid jobId);
    bool Fail(Guid jobId, string? error);
}
