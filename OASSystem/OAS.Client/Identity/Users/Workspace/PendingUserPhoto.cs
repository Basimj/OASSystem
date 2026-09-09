namespace OAS.Client.Identity.Users.Workspace;

public sealed record PendingUserPhoto(string FileName, string ContentType, byte[] Content)
{
    public string PreviewUrl => $"data:{ContentType};base64,{Convert.ToBase64String(Content)}";
}
