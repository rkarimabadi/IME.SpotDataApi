namespace IME.SpotDataApi.Models.Presentation
{
    public class BrokerHeaderData
    {
        public string BrokerName { get; set; }
    }

    public class CompetitionData
    {
        public double Percentage { get; set; }
        public string Label { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
    public class MarketShareItem
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Subtitle { get; set; }
        public string IconCssClass { get; set; }
        public string ThemeCssClass { get; set; }
    }
    public class RankingItem
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public int Rank { get; set; }
        public string IconCssClass { get; set; }
        public string ThemeCssClass { get; set; }
    }
    public class CommodityGroupShareItem
    {
        public string GroupName { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; }
    }
    public class TopSuppliersData
    {
        public List<SupplierPerformanceItem> ByValue { get; set; }
        public List<SupplierPerformanceItem> ByVolume { get; set; }
        public List<SupplierPerformanceItem> ByCount { get; set; }
    }

    public class SupplierPerformanceItem
    {
        public string SupplierName { get; set; }
        public int SupplierId { get; set; }
        public decimal Value { get; set; } // Raw value for sorting and calculating bar width
        public string DisplayValue { get; set; } // Formatted string for display
    }
    public class SupplierItem
    {
        public int Id { get; set; } // It's good practice to have an ID
        public string Name { get; set; }
    }
    public class StrategicPerformanceItem
    {
        public string CommodityName { get; set; } = "";
        public int CommodityId { get; set; }
        public PerformanceStatus ValuePerformance { get; set; }
        public PerformanceStatus VolumePerformance { get; set; }
    }

    public enum PerformanceStatus { Strong, Weak, NotPresent }





}
