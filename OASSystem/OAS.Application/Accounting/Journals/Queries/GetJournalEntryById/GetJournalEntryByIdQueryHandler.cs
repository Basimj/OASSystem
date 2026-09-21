using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Journals.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Journals;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Journals.Queries.GetJournalEntryById;

public sealed class GetJournalEntryByIdQueryHandler(
    IReadRepository<JournalEntry, Guid> repository)
    : IRequestHandler<GetJournalEntryByIdQuery, JournalEntryDto>
{
    public async Task<JournalEntryDto> Handle(
        GetJournalEntryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(JournalEntry), request.Id);
        }

        return JournalEntryMapping.ToDto(entity);
    }
}
