using Transport.Domain.Identity;

namespace Transport.Application.Identity;

public sealed record UserAuthenticationData(
    User User,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
