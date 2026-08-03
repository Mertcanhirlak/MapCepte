using Transport.Domain.TransitLines;

namespace Transport.Application.TransitLines;

public interface ITransitLineAccessPolicy
{
    bool CanManage(TransitLineAccessContext access, TransitLine transitLineEntity);
    bool CanRead(TransitLineAccessContext access, TransitLine transitLineEntity);
}

public sealed class TransitLineAccessPolicy : ITransitLineAccessPolicy
{
    public bool CanManage(TransitLineAccessContext access, TransitLine transitLineEntity)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(transitLineEntity);

        return access.UserId != Guid.Empty
            && (access.IsAdmin
                || (access.IsOperator && transitLineEntity.OwnerUserId == access.UserId));
    }

    public bool CanRead(TransitLineAccessContext access, TransitLine transitLineEntity)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(transitLineEntity);

        if (access.IsAdmin)
        {
            return true;
        }

        if (access.IsOperator && transitLineEntity.OwnerUserId == access.UserId)
        {
            return true;
        }

        return transitLineEntity.Status == TransitLineStatus.Published;
    }
}
