using OAS.Application.Abstractions.Messaging;
using OAS.Contracts.Identity.Authentication;

namespace OAS.Application.Identity.Authentication.Commands;

public sealed record CompletePasswordSetupCommand(CompletePasswordSetupRequest Request)
    : ICommand<CurrentUserDto>;
