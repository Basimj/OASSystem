namespace OAS.Contracts.Identity.Profile;

public sealed record UpdateMyProfileRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string RowVersion);
