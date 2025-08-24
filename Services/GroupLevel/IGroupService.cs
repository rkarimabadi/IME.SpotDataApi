using IME.SpotDataApi.Models.Presentation;

namespace IME.SpotDataApi.Services.GroupLevel
{
    public interface IGroupService
    {
        Task<GroupListData> GetActiveSubGroupsAsync(int groupId);
        Task<MarketConditionsData> GetSubGroupActivitiesAsync(int groupId);
        Task<UpcomingOffersData> GetUpcomingOffersAsync(int groupId);
        Task<UpcomingOffersData> GetTodayOffersAsync(int groupId);
        Task<GroupHeaderData> GetGroupHeaderDataAsync(int subGroupId);
        Task<List<HierarchyItem>> GetGroupHierarchyAsync(int subGroupId);
    }
}
