using OAS.Application.Abstractions.Messaging;
using OAS.Contracts.Identity.Authentication;
using OAS.Contracts.Identity.Profile;

namespace OAS.Application.Identity.Profile.Commands.ChangeMyPassword;

public sealed record ChangeMyPasswordCommand(ChangeMyPasswordRequest Request) : ICommand<CurrentUserDto>;
