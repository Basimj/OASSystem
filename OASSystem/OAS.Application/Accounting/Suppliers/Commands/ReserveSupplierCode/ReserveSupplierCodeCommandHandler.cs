using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Suppliers.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Suppliers;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;
namespace OAS.Application.Accounting.Suppliers.Commands.ReserveSupplierCode;
public sealed class ReserveSupplierCodeCommandHandler(ISequenceNumberGenerator sequences,IReadRepository<Supplier,Guid> suppliers):IRequestHandler<ReserveSupplierCodeCommand,SupplierCodeReservationDto>
{
    public async Task<SupplierCodeReservationDto> Handle(ReserveSupplierCodeCommand request,CancellationToken ct)
    {
        for(var i=0;i<100;i++)
        {
            var code=SupplierCodeFormatter.Format(await sequences.NextAsync("SupplierCodeSequence",ct));
            if(await suppliers.CountAsync(SupplierSpecifications.ByCode(code),ct)==0) return new(code);
        }
        throw new ConflictException("accounting_supplier_code_exhausted","Unable to reserve a unique supplier code.");
    }
}
