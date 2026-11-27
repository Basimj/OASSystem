using OAS.Contracts.Accounting.CashTransfers;

namespace OAS.Application.Accounting.Abstractions;

public interface ICashTransferService
{
    Task<CashTransferLookupsDto> LookupsAsync(CancellationToken ct, DateOnly? date = null);
    Task<CashTransferPageDto> ListAsync(int page, string? search, CancellationToken ct, string? type = null, int? status = null);
    Task<Guid> SaveAsync(Guid? id, CashTransferRequest request, CancellationToken ct);
    Task<Guid> ActionAsync(Guid id, string action, CashTransferActionRequest request, CancellationToken ct);
}
