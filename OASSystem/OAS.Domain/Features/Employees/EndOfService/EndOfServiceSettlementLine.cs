using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.EndOfService;

public sealed class EndOfServiceSettlementLine : AuditableEntity<Guid>
{
    private EndOfServiceSettlementLine() { }
    private EndOfServiceSettlementLine(Guid id,Guid settlementId,int seq,EndOfServiceLineType type,string description,string? sourceType,Guid? sourceId,decimal? quantity,decimal? rate,decimal amount,string? debitRole,string? creditRole)
    {if(id==Guid.Empty||settlementId==Guid.Empty||seq<=0||string.IsNullOrWhiteSpace(description)||amount<0||!Enum.IsDefined(type))throw new DomainException("End-of-service line is invalid.");Id=id;EndOfServiceSettlementId=settlementId;LineSequence=seq;LineType=type;Description=description.Trim();SourceDocumentType=N(sourceType);SourceDocumentId=Normalize(sourceId);Quantity=quantity;Rate=rate;Amount=amount;DebitPostingRole=N(debitRole);CreditPostingRole=N(creditRole);}
    public Guid EndOfServiceSettlementId { get; private set; }
    public int LineSequence { get; private set; }
    public EndOfServiceLineType LineType { get; private set; }
    public string? SourceDocumentType { get; private set; }
    public Guid? SourceDocumentId { get; private set; }
    public string Description { get; private set; }=string.Empty;
    public decimal? Quantity { get; private set; }
    public decimal? Rate { get; private set; }
    public decimal Amount { get; private set; }
    public string? DebitPostingRole { get; private set; }
    public string? CreditPostingRole { get; private set; }
    public byte[] RowVersion { get; private set; }=[];
    public static EndOfServiceSettlementLine Create(Guid id,Guid settlementId,int seq,EndOfServiceLineType type,string description,string? sourceType,Guid? sourceId,decimal? quantity,decimal? rate,decimal amount,string? debitRole,string? creditRole)=>new(id,settlementId,seq,type,description,sourceType,sourceId,quantity,rate,amount,debitRole,creditRole);
    private static Guid? Normalize(Guid? v)=>v is { } id&&id!=Guid.Empty?id:null;
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
