namespace Transport.Application.TransitLines;

public sealed record TransitLineAccessContext(
    Guid UserId,
    bool IsAdmin,
    bool IsOperator);
