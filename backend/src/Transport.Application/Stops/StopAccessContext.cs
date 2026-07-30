namespace Transport.Application.Stops;

public sealed record StopAccessContext(
    Guid UserId,
    bool IsAdmin,
    bool IsOperator);
