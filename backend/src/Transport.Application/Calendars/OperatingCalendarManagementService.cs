using Transport.Domain.Calendars;

namespace Transport.Application.Calendars;

public sealed record CalendarCatalogItem(
    Guid Id,
    string Name,
    string DaysOfWeek,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed class OperatingCalendarManagementService(
    IOperatingCalendarRepository calendarRepository,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyCollection<CalendarCatalogItem>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var calendars = await calendarRepository.ListAllAsync(cancellationToken);
        if (calendars.Count == 0)
        {
            // Auto seed default calendars if none exist
            await SeedDefaultCalendarsAsync(cancellationToken);
            calendars = await calendarRepository.ListAllAsync(cancellationToken);
        }

        return calendars.Select(ToCatalogItem).ToArray();
    }

    public async Task<CalendarCatalogItem> CreateCalendarAsync(
        string name,
        DaysOfWeek daysOfWeek,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var calendar = new OperatingCalendar(
            Guid.NewGuid(),
            name,
            daysOfWeek,
            isActive: true,
            createdByUserId,
            now);

        await calendarRepository.AddAsync(calendar, cancellationToken);
        await calendarRepository.SaveChangesAsync(cancellationToken);

        return ToCatalogItem(calendar);
    }

    private async Task SeedDefaultCalendarsAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var systemUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var weekdays = new OperatingCalendar(
            Guid.Parse("a1111111-1111-1111-1111-111111111111"),
            "Hafta İçi Şablonu",
            DaysOfWeek.Weekdays,
            isActive: true,
            systemUserId,
            now);

        var saturday = new OperatingCalendar(
            Guid.Parse("a2222222-2222-2222-2222-222222222222"),
            "Cumartesi Şablonu",
            DaysOfWeek.Saturday,
            isActive: true,
            systemUserId,
            now);

        var sunday = new OperatingCalendar(
            Guid.Parse("a3333333-3333-3333-3333-333333333333"),
            "Pazar / Tatil Şablonu",
            DaysOfWeek.Sunday,
            isActive: true,
            systemUserId,
            now);

        await calendarRepository.AddAsync(weekdays, cancellationToken);
        await calendarRepository.AddAsync(saturday, cancellationToken);
        await calendarRepository.AddAsync(sunday, cancellationToken);
        await calendarRepository.SaveChangesAsync(cancellationToken);
    }

    private static CalendarCatalogItem ToCatalogItem(OperatingCalendar calendar) =>
        new(
            calendar.Id,
            calendar.Name,
            calendar.DaysOfWeek.ToString(),
            calendar.IsActive,
            calendar.CreatedAtUtc);
}
