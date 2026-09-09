using OAS.Application.Abstractions.Messaging;
using OAS.Contracts.Identity.Profile;

namespace OAS.Application.Identity.Profile.Queries.GetMyProfile;

public sealed record GetMyProfileQuery : IQuery<MyProfileDto>;
