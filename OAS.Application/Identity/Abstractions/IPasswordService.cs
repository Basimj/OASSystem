using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Abstractions;

public interface IPasswordService
{
    string Hash(UserAccount user, string password);
    PasswordCheckResult Verify(UserAccount user, string password);
    PasswordCheckResult VerifyHash(UserAccount user, string passwordHash, string password);
    void SimulateVerification(string password);
}
