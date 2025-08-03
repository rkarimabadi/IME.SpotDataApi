using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Spot;
using Microsoft.EntityFrameworkCore;

namespace IME.SpotDataApi.Repository
{
    public class TradeReportRepository : ITradeReportRepository
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;

        public TradeReportRepository(IDbContextFactory<AppDataContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<TradeReport?> GetTradeReportByIdAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.TradeReports
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<TradeReport>> GetTradeReportsAsync(int pageNumber, int pageSize)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.TradeReports
                .OrderByDescending(t => t.TradeDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
