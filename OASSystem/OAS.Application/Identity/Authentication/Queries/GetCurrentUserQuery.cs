using OAS.Application.Abstractions.Messaging;
using OAS.Contracts.Identity.Authentication;

namespace OAS.Application.Identity.Authentication.Queries;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserDto>;
