using IME.SpotDataApi.Models.Presentation;

namespace IME.SpotDataApi.Services.MainGroupLevel
{
    public interface ISubGroupService
    {
        Task<GroupListData> GetActiveCommoditiesAsync(int subGroupId);
        Task<MarketConditionsData> GetCommodityActivitiesAsync(int subGroupId);
        Task<UpcomingOffersData> GetOfferHistoryAsync(int subGroupId);
        Task<SubGroupHeaderData> GetSubGroupHeaderDataAsync(int subGroupId);
        Task<List<HierarchyItem>> GetSubGroupHierarchyAsync(int subGroupId);
    }
}

