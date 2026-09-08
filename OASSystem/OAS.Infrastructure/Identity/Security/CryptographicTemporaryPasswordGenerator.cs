using System.Security.Cryptography;
using OAS.Application.Identity.Abstractions;

namespace OAS.Infrastructure.Identity.Security;

public sealed class CryptographicTemporaryPasswordGenerator : ITemporaryPasswordGenerator
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Specials = "!@#$%*-_=+?";
    private const int Length = 18;

    public string Generate()
    {
        Span<char> buffer = stackalloc char[Length];
        buffer[0] = Pick(Upper);
        buffer[1] = Pick(Lower);
        buffer[2] = Pick(Digits);
        buffer[3] = Pick(Specials);

        var all = Upper + Lower + Digits + Specials;
        for (var i = 4; i < buffer.Length; i++) buffer[i] = Pick(all);

        for (var i = buffer.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }

        return new string(buffer);
    }

    private static char Pick(string characters) => characters[RandomNumberGenerator.GetInt32(characters.Length)];
}
