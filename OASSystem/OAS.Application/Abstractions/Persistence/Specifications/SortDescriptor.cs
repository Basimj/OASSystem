using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Abstractions.Persistence.Specifications;

public sealed record SortDescriptor(string PropertyName, SortDirection Direction);
