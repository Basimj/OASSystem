using OAS.Application.Abstractions.Messaging;
using OAS.Contracts.Identity.Profile;

namespace OAS.Application.Identity.Profile.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(UpdateMyProfileRequest Request) : ICommand;
