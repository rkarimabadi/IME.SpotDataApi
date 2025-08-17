using IME.SpotDataApi.Models.Presentation;

namespace IME.SpotDataApi.Services.MainGroupLevel
{
    public interface ISubGroupService
    {
        Task<GroupListData> GetActiveCommoditiesAsync(int subGroupId);
        Task<MarketConditionsData> GetCommodityActivitiesAsync(int subGroupId);
        Task<UpcomingOffersData> GetUpcomingOffersAsync(int subGgroupId);
        Task<UpcomingOffersData> GetTodayOffersAsync(int subGroupId);
    }
}
