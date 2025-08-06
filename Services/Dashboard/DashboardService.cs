using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Models.Spot;
using IME.SpotDataApi.Repository;
using Microsoft.EntityFrameworkCore;

namespace IME.SpotDataApi.Services.Dashboard
{
    public interface IDashboardService
    {
        Task<MarketPulseData> GetMarketPulseAsync();
        Task<MarketSentimentData> GetMarketSentimentAsync();
    }

    /// <summary>
    /// سرویسی که منطق تجاری و محاسباتی مربوط به ویجت‌های داشبورد را پیاده‌سازی می‌کند.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly IDateHelper _dateHelper;

        public DashboardService(
            IDbContextFactory<AppDataContext> contextFactory,
            IDateHelper dateHelper)
        {
            _contextFactory = contextFactory;
            _dateHelper = dateHelper;
        }

        public async Task<MarketPulseData> GetMarketPulseAsync()
        {
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            using var context = _contextFactory.CreateDbContext();
            
            var allOffers = context.Offers.ToList();
            var todayOffers = allOffers.Where(o => o.OfferDate == todayPersian).ToList();
            
            var allTrades = context.TradeReports.ToList();
            var todayTrades = allTrades.Where(t => t.TradeDate == todayPersian).ToList();

            var realizationItem = CreateRealizationRatioItem(todayOffers, todayTrades);
            var demandStrengthItem = CreateDemandStrengthRatioItem(todayOffers, todayTrades);
            return new MarketPulseData
            {
                Items = new List<PulseCardItem> { realizationItem, demandStrengthItem }
            };
        }

        /// <summary>
        /// آیتم کارت "پتانسیل بازار" (نسبت تحقق) را ایجاد می‌کند.
        /// </summary>
        private PulseCardItem CreateRealizationRatioItem(List<Offer> todayOffers, List<TradeReport> todayTrades)
        {
            // محاسبه ارزش بالقوه بر اساس قیمت پایه و حجم عرضه
            decimal potentialValue = todayOffers.Sum(o => o.InitPrice * o.OfferVol);
            // محاسبه ارزش محقق شده بر اساس ارزش معامله گزارش شده (ضرب در ۱۰۰۰)
            decimal realizedValue = todayTrades.Sum(t => t.TradeValue); //todayTrades.Sum(t => t.TradeValue * 1000);
            decimal realizationRatio = potentialValue > 0 ? (realizedValue / potentialValue) * 100 : 0;

            return new PulseCardItem
            {
                Title = "پتانسیل بازار",
                Value = FormatLargeNumber(realizedValue),
                Change = $"{realizationRatio:F1}%",
                ChangeState = realizationRatio > 50 ? ValueState.Positive : (realizationRatio < 50 && realizationRatio > 0 ? ValueState.Negative : ValueState.Neutral)
            };
        }

        
        /// <summary>
        /// آیتم کارت "نسبت قدرت تقاضا" را ایجاد می‌کند.
        /// </summary>
        private PulseCardItem CreateDemandStrengthRatioItem(List<Offer> todayOffers, List<TradeReport> todayTrades)
        {
            // محاسبه حجم کل عرضه اولیه
            decimal totalInitialSupply = todayOffers.Sum(o => o.InitVolume);
            // محاسبه حجم کل تقاضای ثبت شده
            decimal totalRegisteredDemand = todayTrades.Sum(t => t.DemandVolume);
            decimal demandStrengthRatio = totalInitialSupply > 0 ? totalRegisteredDemand / totalInitialSupply : 0;

            return new PulseCardItem
            {
                Title = "نسبت قدرت تقاضا",
                Value = $"{totalRegisteredDemand:N0} تن",
                Change = $"x{demandStrengthRatio:F2}",
                ChangeState = demandStrengthRatio > 1 ? ValueState.Positive : (demandStrengthRatio < 1 && demandStrengthRatio > 0 ? ValueState.Negative : ValueState.Neutral)
            };
        }

        /// <summary>
        /// یک عدد بزرگ (به ریال) را به فرمت "همت" تبدیل می‌کند.
        /// </summary>
        private string FormatLargeNumber(decimal value)
        {
            if (value == 0) return "۰";
            // 1 همت = 10,000,000,000,000 ریال
            var hemtValue = (value * 10000) / 10_000_000_000_000M;
            return $"{hemtValue:F1} همت";
        }

        #region MarketSentiment
        public async Task<MarketSentimentData> GetMarketSentimentAsync()
        {            
            using var context = _contextFactory.CreateDbContext();          
         
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);
            var allOffers = context.Offers.ToList();
            var todayOffers = allOffers.Where(o => o.OfferDate == todayPersian).ToList();

            if (todayOffers.Count == 0)
            {
                return new MarketSentimentData { Items = [] };
            }

            var totalValue = todayOffers.Sum(o => o.InitPrice * o.OfferVol);
            if (totalValue == 0)
            {
                 return new MarketSentimentData { Items = [] };
            }

            var valueByCategory = todayOffers
                .GroupBy(o => GetContractCategory(o.ContractTypeId))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(o => o.InitPrice * o.OfferVol)
                );

            var items = new List<SentimentItem>();
            var categories = new List<(string Name, string Color)>
            {
                ("نقدی", "var(--primary-color)"),
                ("سلف", "var(--success-color)"),
                ("نسیه", "var(--warning-color)"),
                ("پریمیوم", "var(--info-color)")
            };

            foreach (var category in categories)
            {
                var categoryValue = valueByCategory.TryGetValue(category.Name, out decimal value) ? value : 0;
                var percentage = (int)Math.Round((categoryValue / totalValue) * 100);
                if (percentage > 0)
                {
                    items.Add(new SentimentItem
                    {
                        Name = category.Name,
                        Percentage = percentage,
                        ColorCssVariable = category.Color
                    });
                }
            }
            
            return new MarketSentimentData { Items = [.. items.OrderByDescending(i => i.Percentage)] };
        }

        private string GetContractCategory(int contractTypeId)
        {
            return contractTypeId switch
            {
                1 or 4 or 5 or 7 or 9 or 10 or 11 or 12 or 13 or 14 or 17 or 24 or 25 or 27 or 28 or 30 => "نقدی",
                2 or 8 or 16 or 18 or 19 => "سلف",
                3 or 15 or 20 or 21 or 31 or 33 or 34 => "نسیه",
                29 or 32 or 35 or 36 => "پریمیوم",
                _ => "سایر"
            };
        }

        #endregion MarketSentiment
    }
}
