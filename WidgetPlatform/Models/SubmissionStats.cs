namespace WidgetPlatform.Models;

public sealed record WidgetCount(Guid WidgetId, string Title, int Count);
public sealed record CountryCount(string Country, int Count);

public sealed record SubmissionStats(
    int Total,
    int Last7Days,
    IReadOnlyList<WidgetCount> ByWidget,
    IReadOnlyList<CountryCount> ByCountry);