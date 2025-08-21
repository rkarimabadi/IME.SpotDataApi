using IME.SpotDataApi.Models.Presentation;

namespace IME.SpotDataApi.Services.CommodityLevel
{
    public interface ICommodityService
    {
        Task<CommodityAttributesData> GetCommodityAttributesAsync(int commodityId);
        Task<CommodityHeaderData> GetCommodityHeaderDataAsync(int commodityId);
        Task<List<HierarchyItem>> GetCommodityHierarchyAsync(int commodityId);
        Task<DistributedAttributesData> GetDistributedAttributesAsync(int commodityId);
        Task<IEnumerable<MainPlayer>> GetMainPlayersAsync(int commodityId);
        Task<MarketAbsorptionData> GetMarketAbsorptionAsync(int commodityId);
        Task<PriceViewModel> GetPriceTrendsAsync(int commodityId);
        Task<UpcomingOffersData> GetOfferHistoryAsync(int commodityId);
        Task<DistributedAttributesData> GetPlayerDistributionAsync(int commodityId);
    }
}