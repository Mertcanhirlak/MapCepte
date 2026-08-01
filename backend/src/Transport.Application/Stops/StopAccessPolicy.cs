using Transport.Domain.Stops;

namespace Transport.Application.Stops;

public interface IStopAccessPolicy
{
    bool CanManage(StopAccessContext access, Stop stopEntity);
}

public sealed class StopAccessPolicy : IStopAccessPolicy
{
    public bool CanManage(StopAccessContext access, Stop stopEntity)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(stopEntity);

        return access.UserId != Guid.Empty
            && (access.IsAdmin
                || access.IsOperator
                && stopEntity.CreatedByUserId == access.UserId);
    }
}
