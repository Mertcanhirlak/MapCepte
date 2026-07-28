namespace Transport.Infrastructure.Routing;

public sealed class RoutingOptions
{
    public const string SectionName = "Routing";

    public required Uri BaseUrl { get; init; }

    public string Profile { get; init; } = "driving";
}
