namespace Transport.Domain.Identity;

public static class SystemRoleNames
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string User = "User";

    public static IReadOnlyCollection<string> All { get; } =
        [Admin, Operator, User];
}
