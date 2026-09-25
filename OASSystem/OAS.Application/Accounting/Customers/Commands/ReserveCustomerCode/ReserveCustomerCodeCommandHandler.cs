using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Customers.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Customers;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;
namespace OAS.Application.Accounting.Customers.Commands.ReserveCustomerCode;
public sealed class ReserveCustomerCodeCommandHandler(ISequenceNumberGenerator sequences,IReadRepository<Customer,Guid> customers):IRequestHandler<ReserveCustomerCodeCommand,CustomerCodeReservationDto>
{
    public async Task<CustomerCodeReservationDto> Handle(ReserveCustomerCodeCommand request,CancellationToken ct)
    {
        for(var i=0;i<100;i++)
        {
            var code=CustomerCodeFormatter.Format(await sequences.NextAsync("CustomerCodeSequence",ct));
            if(await customers.CountAsync(CustomerSpecifications.ByCode(code),ct)==0) return new(code);
        }
        throw new ConflictException("accounting_customer_code_exhausted","Unable to reserve a unique customer code.");
    }
}
