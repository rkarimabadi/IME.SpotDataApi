using IME.SpotDataApi.Models.Presentation;
namespace IME.SpotDataApi.Services.BrokerLevel
{
    public interface IBrokerService
    {
        Task<BrokerHeaderData> GetBrokerHeaderAsync(int brokerId);
        Task<CompetitionData> GetCompetitionRatioAsync(int brokerId, int days = 90);
        Task<CompetitionData> GetSuccessRateAsync(int brokerId, int days = 90);
        Task<List<MarketShareItem>> GetMarketShareAsync(int brokerId, int days = 90);
        Task<List<RankingItem>> GetRankingAsync(int brokerId, int days = 90);
        Task<List<CommodityGroupShareItem>> GetCommodityGroupShareAsync(int brokerId, int days = 90);
        Task<UpcomingOffersData> GetBrokerOffersAsync(int brokerId);
        Task<TopSuppliersData> GetTopSuppliersAsync(int brokerId, int days = 90);
        Task<List<SupplierItem>> GetAllSuppliersAsync(int brokerId, int days = 90);
        Task<List<StrategicPerformanceItem>> GetStrategicPerformanceAsync(int brokerId, int days = 90);
    }
}