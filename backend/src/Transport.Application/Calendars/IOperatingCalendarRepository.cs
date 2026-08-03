using Transport.Domain.Calendars;

namespace Transport.Application.Calendars;

public interface IOperatingCalendarRepository
{
    Task<IReadOnlyCollection<OperatingCalendar>> ListAllAsync(CancellationToken cancellationToken);

    Task<OperatingCalendar?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(OperatingCalendar calendarEntity, CancellationToken cancellationToken);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
