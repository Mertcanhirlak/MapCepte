using Microsoft.EntityFrameworkCore;
using Transport.Application.Calendars;
using Transport.Domain.Calendars;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Calendars;

public sealed class EfOperatingCalendarRepository(TransportDbContext dbContext)
    : IOperatingCalendarRepository
{
    public async Task<IReadOnlyCollection<OperatingCalendar>> ListAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.OperatingCalendars
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<OperatingCalendar?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.OperatingCalendars
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        OperatingCalendar calendarEntity,
        CancellationToken cancellationToken)
    {
        await dbContext.OperatingCalendars.AddAsync(calendarEntity, cancellationToken);
    }

    public async Task<bool> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
