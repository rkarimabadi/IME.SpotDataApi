using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IME.SpotDataApi.Services.Search
{
    public interface ISearchService
    {
        Task<SearchResultsData> GlobalSearchAsync(string term);
    }

    public class SearchService : ISearchService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;

        public SearchService(IDbContextFactory<AppDataContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<SearchResultsData> GlobalSearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return new SearchResultsData();
            }

            using var context = await _contextFactory.CreateDbContextAsync();
            var results = new List<SearchResultItem>();
            var searchTerm = term.Trim();

            // جستجو در کالاها
            var commodities = await context.Commodities
                .Where(c => c.PersianName.Contains(searchTerm) || c.Symbol.Contains(searchTerm))
                .Select(c => new SearchResultItem { Id = c.Id, Title = c.PersianName, Subtitle = "کالا", ResultType = SearchResultType.Commodity })
                .Take(5)
                .ToListAsync();
            results.AddRange(commodities);

            // جستجو در گروه‌ها
            var groups = await context.Groups
                .Where(g => g.PersianName.Contains(searchTerm))
                .Select(g => new SearchResultItem { Id = g.Id, Title = g.PersianName, Subtitle = "گروه کالایی", ResultType = SearchResultType.Group })
                .Take(5)
                .ToListAsync();
            results.AddRange(groups);

            // جستجو در کارگزاران
            var brokers = await context.Brokers
                .Where(b => b.PersianName.Contains(searchTerm))
                .Select(b => new SearchResultItem { Id = b.Id, Title = b.PersianName, Subtitle = "کارگزار", ResultType = SearchResultType.Broker })
                .Take(5)
                .ToListAsync();
            results.AddRange(brokers);

            // جستجو در عرضه‌کنندگان
            var suppliers = await context.Suppliers
                .Where(s => s.PersianName.Contains(searchTerm))
                .Select(s => new SearchResultItem { Id = s.Id, Title = s.PersianName, Subtitle = "عرضه‌کننده", ResultType = SearchResultType.Supplier })
                .Take(5)
                .ToListAsync();
            results.AddRange(suppliers);

            // می‌توانید جستجو در سایر موجودیت‌ها را به همین شکل اضافه کنید

            return new SearchResultsData { Items = results.OrderBy(r => r.Title).ToList() };
        }
    }
}
