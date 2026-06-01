namespace KnowledgeOps.Application.Dashboard;

/// <summary>
/// Immutable value object representing the dashboard query period in UTC.
/// </summary>
public sealed record DashboardDateRange
{
    public DateTimeOffset From { get; }
    public DateTimeOffset To { get; }

    private DashboardDateRange(DateTimeOffset from, DateTimeOffset to)
    {
        if (from > to)
            throw new ArgumentException("From must be less than or equal to To.", nameof(from));

        From = from;
        To = to;
    }

    /// <summary>
    /// Creates a DashboardDateRange from optional from/to parameters.
    /// Decision D3: Default period is last 30 days.
    /// If only from provided: [from, now]. If only to provided: [to-30d, to].
    /// </summary>
    public static DashboardDateRange Create(DateTime? from, DateTime? to)
    {
        var now = DateTimeOffset.UtcNow;

        if (from.HasValue && to.HasValue)
        {
            var fromUtc = new DateTimeOffset(from.Value, TimeSpan.Zero);
            var toUtc = new DateTimeOffset(to.Value, TimeSpan.Zero);
            return new DashboardDateRange(fromUtc, toUtc);
        }

        if (from.HasValue)
        {
            var fromUtc = new DateTimeOffset(from.Value, TimeSpan.Zero);
            return new DashboardDateRange(fromUtc, now);
        }

        if (to.HasValue)
        {
            var toUtc = new DateTimeOffset(to.Value, TimeSpan.Zero);
            return new DashboardDateRange(toUtc.AddDays(-30), toUtc);
        }

        return CreateDefault();
    }

    /// <summary>Returns the default period: last 30 days ending now.</summary>
    public static DashboardDateRange CreateDefault()
    {
        var now = DateTimeOffset.UtcNow;
        return new DashboardDateRange(now.AddDays(-30), now);
    }
}
