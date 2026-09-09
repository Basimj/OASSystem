namespace OAS.Application.Identity.Abstractions;

public sealed record UserProfileImageData(
    Guid UserId,
    string ContentType,
    byte[] Content,
    DateTimeOffset UpdatedAtUtc);

public interface IUserProfileImageStore
{
    Task<UserProfileImageData?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveAsync(UserProfileImageData image, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}
