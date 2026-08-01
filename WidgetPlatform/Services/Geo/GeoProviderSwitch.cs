namespace WidgetPlatform.Services.Geo
{
    /// Demo/test only: lets us take the primary provider "down" on purpose
    public sealed class GeoProviderSwitch
    {
        public bool PrimaryIsUp { get; set; } = true;
    }
}
