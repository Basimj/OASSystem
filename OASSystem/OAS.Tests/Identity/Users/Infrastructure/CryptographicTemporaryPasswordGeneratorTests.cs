using NUnit.Framework;
using OAS.Infrastructure.Identity.Security;

namespace OAS.Tests.Identity.Users.Infrastructure;

[TestFixture]
public sealed class CryptographicTemporaryPasswordGeneratorTests
{
    [Test]
    public void Generate_ReturnsPasswordThatMatchesRequiredCharacterClasses()
    {
        var generator = new CryptographicTemporaryPasswordGenerator();

        var password = generator.Generate();

        Assert.Multiple(() =>
        {
            Assert.That(password.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(password.Any(char.IsUpper), Is.True);
            Assert.That(password.Any(char.IsLower), Is.True);
            Assert.That(password.Any(char.IsDigit), Is.True);
            Assert.That(password.Any(ch => !char.IsLetterOrDigit(ch)), Is.True);
        });
    }

    [Test]
    public void Generate_RepeatedCalls_DoNotReturnSameValue()
    {
        var generator = new CryptographicTemporaryPasswordGenerator();

        var first = generator.Generate();
        var second = generator.Generate();

        Assert.That(second, Is.Not.EqualTo(first));
    }
}
