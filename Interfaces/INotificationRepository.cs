using IME.SpotDataApi.Models.Notification;

namespace IME.SpotDataApi.Interfaces
{
    public interface INotificationRepository
    {
        Task<IEnumerable<NewsNotification>> GetNewsNotificationsAsync(int pageNumber, int pageSize);
        Task<IEnumerable<SpotNotification>> GetSpotNotificationsAsync(int pageNumber, int pageSize);
    }
}
