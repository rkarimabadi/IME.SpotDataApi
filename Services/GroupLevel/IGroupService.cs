using IME.SpotDataApi.Models.Presentation;

namespace IME.SpotDataApi.Services.GroupLevel
{
    public interface IGroupService
    {
        Task<GroupListData> GetActiveSubGroupsAsync(int groupId);
        Task<MarketConditionsData> GetSubGroupActivitiesAsync(int groupId);
        Task<UpcomingOffersData> GetUpcomingOffersAsync(int groupId);
        Task<UpcomingOffersData> GetTodayOffersAsync(int groupId);
    }
}
