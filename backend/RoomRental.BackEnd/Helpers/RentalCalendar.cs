namespace RoomRental.BackEnd.Helpers;

public static class RentalCalendar
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    public static DateTime Today(TimeProvider clock) => clock.GetUtcNow().ToOffset(VietnamOffset).Date;

    public static DateTime SelectedDate(DateTime value) => value.Kind == DateTimeKind.Utc
        ? new DateTimeOffset(value).ToOffset(VietnamOffset).Date
        : value.Date;

    // UpdatedAt is written as UTC by ApplicationDbContext, including SQL values with unspecified Kind.
    public static DateTime UtcDateInVietnam(DateTime value) =>
        new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)).ToOffset(VietnamOffset).Date;
}
