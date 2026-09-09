using OAS.Application.Common.Exceptions;

namespace OAS.Application.Identity.Profile;

internal static class ProfileImagePolicy
{
    public const int MaximumBytes = 2_500_000;

    public static void EnsureValid(string contentType, byte[] content)
    {
        if (content.Length == 0 || content.Length > MaximumBytes)
            throw new RequestValidationException(new Dictionary<string, string[]> { ["Image"] = ["profile_image_size_invalid"] });

        var normalized = contentType.Trim().ToLowerInvariant();
        var valid = normalized switch
        {
            "image/jpeg" => IsJpeg(content),
            "image/png" => IsPng(content),
            "image/webp" => IsWebP(content),
            _ => false
        };

        if (!valid)
            throw new RequestValidationException(new Dictionary<string, string[]> { ["Image"] = ["profile_image_type_invalid"] });
    }

    private static bool IsJpeg(byte[] b) => b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;
    private static bool IsPng(byte[] b) => b.Length >= 8 && b.AsSpan(0, 8).SequenceEqual(new byte[] { 137,80,78,71,13,10,26,10 });
    private static bool IsWebP(byte[] b) => b.Length >= 12 && b.AsSpan(0,4).SequenceEqual("RIFF"u8) && b.AsSpan(8,4).SequenceEqual("WEBP"u8);
}
