using OAS.Application.Abstractions.Messaging;

namespace OAS.Application.Identity.Profile.Commands.SetMyProfileImage;

public sealed record SetMyProfileImageCommand(string ContentType, byte[] Content) : ICommand;
