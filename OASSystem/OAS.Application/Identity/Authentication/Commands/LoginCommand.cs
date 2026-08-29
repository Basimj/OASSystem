using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authentication.Models;
using OAS.Contracts.Identity.Authentication;

namespace OAS.Application.Identity.Authentication.Commands;

public sealed record LoginCommand(LoginRequest Request)
    : INonTransactionalCommand<LoginResult>, IManualValidationRequest;
