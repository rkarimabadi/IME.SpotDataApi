using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Notification;
using Microsoft.EntityFrameworkCore;

namespace IME.SpotDataApi.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;

        public NotificationRepository(IDbContextFactory<AppDataContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IEnumerable<NewsNotification>> GetNewsNotificationsAsync(int pageNumber, int pageSize)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.NewsNotifications
                .OrderByDescending(n => n.NewsDateTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<SpotNotification>> GetSpotNotificationsAsync(int pageNumber, int pageSize)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.SpotNotifications
                .OrderByDescending(n => n.NewsDateTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
