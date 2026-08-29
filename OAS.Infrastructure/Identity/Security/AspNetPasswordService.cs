using Microsoft.AspNetCore.Identity;
using OAS.Application.Identity.Abstractions;
using OAS.Domain.Identity.Entities;

namespace OAS.Infrastructure.Identity.Security;

public sealed class AspNetPasswordService : IPasswordService
{
    private readonly PasswordHasher<UserAccount> _hasher = new();
    private readonly UserAccount _dummyUser;
    private readonly string _dummyHash;

    public AspNetPasswordService()
    {
        _dummyUser = UserAccount.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"), "dummy-user", "Dummy", "User", null, false);
        _dummyHash = _hasher.HashPassword(_dummyUser, "OAS-Dummy-Password!42");
    }

    public string Hash(UserAccount user, string password) => _hasher.HashPassword(user, password);

    public PasswordCheckResult Verify(UserAccount user, string password) => VerifyHash(user, user.PasswordHash, password);

    public PasswordCheckResult VerifyHash(UserAccount user, string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(user, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed
        };

    public void SimulateVerification(string password) =>
        _ = _hasher.VerifyHashedPassword(_dummyUser, _dummyHash, password);
}
