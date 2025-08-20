namespace IME.SpotDataApi.Models.Presentation
{
    public class MarketStatItem
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string IconCssClass { get; set; } = string.Empty;
        public string ThemeCssClass { get; set; } = string.Empty;
        public ValueState ValueState { get; set; } = ValueState.Neutral;
    }

    public class MarketStatsData
    {
        public List<MarketStatItem> Items { get; set; } = new();
    }
}
