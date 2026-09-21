using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

internal static class AccountingPageRequestCompatibility
{
    public static PageRequest Apply(PageRequest request, string? searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(request.Search) || string.IsNullOrWhiteSpace(searchTerm))
            return request;

        return request with { Search = searchTerm.Trim() };
    }
}
