using Microsoft.AspNetCore.Authorization;

namespace Transport.Api.Authorization;

public sealed record PermissionRequirement(string Permission)
    : IAuthorizationRequirement;
