namespace WidgetPlatform.Services.Geo
{
    public class GeoEnricher
    {
        private readonly IEnumerable<IGeoProvider> _providers;
        private readonly ILogger<GeoEnricher> _logger;


        public GeoEnricher(IEnumerable<IGeoProvider> providers, ILogger<GeoEnricher> logger)
        {
            _providers = providers;
            _logger = logger;
        }

        public async Task<GeoLocation?> LookupAsync(string? ip, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return null;

            foreach (var provider in _providers)
            {
                try
                {
                    var geo = await provider.LookupAsync(ip, ct);
                    if (geo is not null)
                    {
                        _logger.LogInformation("Geo resolved by {Provider}", provider.Name);
                        return geo;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Provider} failed, trying the next one", provider.Name);
                }
            }

            _logger.LogWarning("No geo provider could resolve {Ip}", ip);
            return null;
        }
    }
}
