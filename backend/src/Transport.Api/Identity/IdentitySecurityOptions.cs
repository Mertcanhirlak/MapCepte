namespace Transport.Api.Identity;

public sealed class IdentitySecurityOptions
{
    public const string SectionName = "IdentitySecurity";

    public bool AllowWeakPasswordsInDevelopment { get; init; }
}
