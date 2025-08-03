using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Spot;
using Microsoft.EntityFrameworkCore;

namespace IME.SpotDataApi.Repository
{
    public class OfferRepository : IOfferRepository
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;

        public OfferRepository(IDbContextFactory<AppDataContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Offer?> GetOfferByIdAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Offers
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Offer>> GetOffersAsync(int pageNumber, int pageSize)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Offers
                .OrderByDescending(o => o.OfferDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
