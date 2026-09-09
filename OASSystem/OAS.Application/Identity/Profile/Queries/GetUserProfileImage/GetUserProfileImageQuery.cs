using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Abstractions;

namespace OAS.Application.Identity.Profile.Queries.GetUserProfileImage;

public sealed record GetUserProfileImageQuery(Guid UserId) : IQuery<UserProfileImageData?>;
