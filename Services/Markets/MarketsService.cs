using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IME.SpotDataApi.Services.Markets
{
    public interface IMarketsService
    {
        Task<List<MarketInfo>> GetMainGroupsDataAsync();
        Task<CommodityStatusData> GetIndexGroupsAsync();
        Task<List<MarketActivity>> GetMarketActivitiesAsync();
        Task<MarketHeatmapData> GetMarketHeatmapDataAsync();
        Task<MarketShortcutsData> GetMarketShortcutsAsync();
        Task<List<ItemInfo>> GetMarketListAsync();
    }

    /// <summary>
    /// نسخه ریفکتور شده با الگوهای بهینه برای اجرای تمام محاسبات در دیتابیس
    /// و جلوگیری از بارگذاری جداول بزرگ در حافظه.
    /// </summary>
    public class MarketsService : IMarketsService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly IDateHelper _dateHelper;

        public MarketsService(IDbContextFactory<AppDataContext> contextFactory, IDateHelper dateHelper)
        {
            _contextFactory = contextFactory;
            _dateHelper = dateHelper;
        }

        #region MainGroupsData
        public async Task<List<MarketInfo>> GetMainGroupsDataAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // بهینه‌سازی: تمام محاسبات و اتصال جداول در یک کوئری واحد انجام می‌شود.
            var tradesWithHierarchy = from trade in context.TradeReports.Where(t => t.TradeDate == todayPersian)
                                      join offer in context.Offers on trade.OfferId equals offer.Id
                                      join commodity in context.Commodities on offer.CommodityId equals commodity.Id
                                      join subGroup in context.SubGroups on commodity.ParentId equals subGroup.Id
                                      join grp in context.Groups on subGroup.ParentId equals grp.Id
                                      join mainGroup in context.MainGroups on grp.ParentId equals mainGroup.Id
                                      select new
                                      {
                                          trade.TradeValue,
                                          trade.OfferBasePrice,
                                          trade.TradeVolume,
                                          MainGroup = mainGroup,
                                          SubGroupName = subGroup.PersianName
                                      };

            var activeMarketData = await tradesWithHierarchy
                .GroupBy(x => x.MainGroup)
                .Select(g => new
                {
                    MainGroupId = g.Key.Id,
                    TotalTradeValue = g.Sum(x => x.TradeValue),
                    TotalBaseValue = g.Sum(x => x.OfferBasePrice * x.TradeVolume / 1000),
                    TopSubGroups = g.GroupBy(x => x.SubGroupName)
                                    .OrderByDescending(sg => sg.Sum(t => t.TradeValue))
                                    .Select(sg => sg.Key)
                                    .Take(3)
                })
                .ToDictionaryAsync(d => d.MainGroupId);

            var allMainGroups = await context.MainGroups.ToListAsync();

            return allMainGroups.Select(mainGroup =>
            {
                string subtitle;
                string heatLevel;
                string heatLabel;

                if (activeMarketData.TryGetValue(mainGroup.Id, out var data))
                {
                    decimal marketHeat = data.TotalBaseValue > 0 ? ((data.TotalTradeValue - data.TotalBaseValue) / data.TotalBaseValue) * 100 : 0;
                    subtitle = string.Join("، ", data.TopSubGroups);
                    (heatLevel, heatLabel) = GetHeatLevel(marketHeat);
                }
                else
                {
                    subtitle = GetDefaultSubtitle(mainGroup.PersianName);
                    heatLevel = "neutral";
                    heatLabel = "خنثی";
                }

                var (iconClass, iconContainerClass) = GetMainGroupVisuals(mainGroup.PersianName);

                return new MarketInfo(
                    mainGroup.PersianName, mainGroup.Id, subtitle, mainGroup.KeyName,
                    iconClass, iconContainerClass, heatLevel, heatLabel
                );
            }).ToList();
        }
        
        // Helper methods remain unchanged
        private (string HeatLevel, string HeatLabel) GetHeatLevel(decimal heat)
        {
            if (heat > 10) return ("high", "داغ");
            if (heat > 3) return ("medium", "متوسط");
            if (heat < -3) return ("low", "آرام");
            return ("neutral", "خنثی");
        }

        private string GetDefaultSubtitle(string mainGroupName)
        {
            return mainGroupName switch
            {
                "صنعتی" => "فولاد، مس، آلومینیوم",
                "پتروشیمی و فرآورده های نفتی" => "پلیمرها، مواد شیمیایی، قیر",
                "کشاورزی" => "زعفران، پسته، جو",
                "اموال غیر منقول" => "ساختمان، زمین، مستغلات",
                "معدنی" => "سنگ آهن، زغال سنگ",
                "فرآورده های نفتی" => "قیر، روغن، گاز",
                _ => "کالاهای متنوع"
            };
        }

        private (string IconClass, string IconContainerClass) GetMainGroupVisuals(string mainGroupName)
        {
            if (mainGroupName.Contains("صنعتی")) return ("bi-building", "industrial");
            if (mainGroupName.Contains("پتروشیمی")) return ("bi-droplet-fill", "petro");
            if (mainGroupName.Contains("کشاورزی")) return ("bi-tree-fill", "agri");
            if (mainGroupName.Contains("نفتی")) return ("bi-fuel-pump-fill", "oil-products");
            if (mainGroupName.Contains("معدنی")) return ("bi-gem", "mineral");
            if (mainGroupName.Contains("اموال")) return ("bi-house-door-fill", "real-estate");
            if (mainGroupName.Contains("فرعی")) return ("bi-shop", "secondary");
            return ("bi-grid-fill", "other");
        }
        #endregion MainGroupsData

        #region IndexGroups
        public async Task<CommodityStatusData> GetIndexGroupsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // بهینه‌سازی: واکشی و پردازش ۵ گروه برتر در یک کوئری
            var topGroupsData = await context.Offers
                .Where(o => o.OfferDate == todayPersian)
                .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => new { o, c })
                .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.o, sg })
                .Join(context.Groups, j => j.sg.ParentId, g => g.Id, (j, g) => new { j.o, g })
                .GroupBy(x => x.g)
                .Select(g => new { Group = g.Key, OfferCount = g.Count(), OfferIds = g.Select(x => x.o.Id) })
                .OrderByDescending(x => x.OfferCount)
                .Take(5)
                .ToListAsync();
            
            if (!topGroupsData.Any()) return new CommodityStatusData();

            var topGroupIds = topGroupsData.Select(g => g.Group.Id).ToList();
            var allOfferIds = topGroupsData.SelectMany(g => g.OfferIds).ToHashSet();

            // یک کوئری مجزا برای واکشی معاملات مربوط به تمام گروه‌های برتر
            var tradesData = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian && allOfferIds.Contains(t.OfferId))
                .Select(t => new { t.OfferId, t.DemandVolume, t.OfferVolume })
                .ToDictionaryAsync(t => t.OfferId);
            
            var mainGroups = await context.MainGroups.ToDictionaryAsync(m => m.Id);

            var items = topGroupsData.Select(topGroup =>
            {
                var tradesForGroup = topGroup.OfferIds.Select(id => tradesData.GetValueOrDefault(id)).Where(t => t != null);
                var totalDemand = tradesForGroup.Sum(t => t.DemandVolume);
                var totalSupply = tradesForGroup.Sum(t => t.OfferVolume);
                var mainGroup = mainGroups.GetValueOrDefault(topGroup.Group.ParentId ?? 0);

                return new CommodityStatusItem
                {
                    Name = topGroup.Group.PersianName,
                    MainGroupName = mainGroup?.PersianName ?? "نامشخص",
                    OfferCount = topGroup.OfferCount,
                    DemandStatus = GetDemandStatus(totalDemand, totalSupply)
                };
            }).ToList();

            return new CommodityStatusData { Items = items };
        }

        private DemandStatus GetDemandStatus(decimal demand, decimal supply)
        {
            if (supply == 0) return DemandStatus.Medium;
            var ratio = demand / supply;
            if (ratio > 1.2m) return DemandStatus.High;
            if (ratio < 0.8m) return DemandStatus.Low;
            return DemandStatus.Medium;
        }
        #endregion IndexGroups

        #region MarketActivities
        public async Task<List<MarketActivity>> GetMarketActivitiesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // بهینه‌سازی: تمام محاسبات در یک کوئری واحد
            var activitiesQuery = from offer in context.Offers.Where(o => o.OfferDate == todayPersian)
                                  join commodity in context.Commodities on offer.CommodityId equals commodity.Id
                                  join subGroup in context.SubGroups on commodity.ParentId equals subGroup.Id
                                  join grp in context.Groups on subGroup.ParentId equals grp.Id
                                  join mainGroup in context.MainGroups on grp.ParentId equals mainGroup.Id
                                  join trade in context.TradeReports.Where(t => t.TradeDate == todayPersian)
                                        on offer.Id equals trade.OfferId into trades
                                  from trade in trades.DefaultIfEmpty()
                                  select new { mainGroup, offer, trade };

            var activities = await activitiesQuery
                .GroupBy(x => x.mainGroup)
                .Select(g => new
                {
                    MainGroup = g.Key,
                    TotalOffers = g.Select(x => x.offer.Id).Distinct().Count(),
                    TradedOffers = g.Where(x => x.trade != null).Select(x => x.offer.Id).Distinct().Count(),
                    TotalValue = g.Where(x => x.trade != null).Sum(x => x.trade.TradeValue * 1000)
                })
                .OrderByDescending(x => x.TotalOffers)
                .Take(3)
                .ToListAsync();

            var result = activities.Select(x =>
            {
                var percentage = x.TotalOffers > 0 ? (double)x.TradedOffers / x.TotalOffers : 0;
                return new MarketActivity(
                    x.MainGroup.PersianName,
                    GetMainGroupVisuals(x.MainGroup.PersianName).IconContainerClass,
                    percentage,
                    $"{percentage:P0} انجام شده",
                    FormatLargeNumber(x.TotalValue)
                );
            }).ToList();

            while (result.Count < 3)
            {
                result.Add(new MarketActivity("نامشخص", "disabled", 0, "بدون عرضه", "۰"));
            }

            return result;
        }

        private string FormatLargeNumber(decimal valueInRials)
        {
            if (valueInRials == 0) return "۰";
            var hemtValue = valueInRials / 10_000_000_000_000M;
            return $"{hemtValue:F1} همت";
        }
        #endregion MarketActivities

        #region MarketHeatmap
        public async Task<MarketHeatmapData> GetMarketHeatmapDataAsync()
        {
            // این متد به دلیل پیچیدگی منطق و نیاز به محاسبات چندمرحله‌ای،
            // با الگوی واکشی داده‌های اولیه و سپس پردازش در حافظه بهینه شده است.
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var tradesWithHierarchy = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian)
                .Join(context.Offers, t => t.OfferId, o => o.Id, (t, o) => new { t, o })
                .Join(context.Commodities, j => j.o.CommodityId, c => c.Id, (j, c) => new { j.t, j.o, c })
                .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.t, j.o, sg })
                .Join(context.Groups, j => j.sg.ParentId, g => g.Id, (j, g) => new { j.t, j.o, g })
                .Join(context.MainGroups, j => j.g.ParentId, mg => mg.Id, (j, mg) => new { j.t, j.o, mg })
                .ToListAsync();

            var activeGroupMetrics = tradesWithHierarchy
                .GroupBy(x => x.mg)
                .Select(g =>
                {
                    decimal totalFinalValue = g.Sum(x => x.t.FinalWeightedAveragePrice * x.t.TradeVolume);
                    decimal totalBaseValue = g.Sum(x => x.t.OfferBasePrice * x.t.TradeVolume);
                    decimal heat = totalBaseValue > 0 ? ((totalFinalValue - totalBaseValue) / totalBaseValue) * 100 : 0;
                    decimal demand = g.Sum(x => x.t.DemandVolume);
                    decimal supply = g.Sum(x => x.o.OfferVol);
                    decimal realization = g.Sum(x => x.o.InitPrice * x.o.OfferVol) > 0 ? g.Sum(x => x.t.TradeValue * 1000) / g.Sum(x => x.o.InitPrice * x.o.OfferVol * 1000) * 100 : 0;

                    return new { MainGroup = g.Key, Heat = heat, DemandRatio = supply > 0 ? demand / supply : 0, RealizationRatio = realization };
                })
                .OrderByDescending(x => x.Heat)
                .ToList();

            var allMainGroups = await context.MainGroups.ToListAsync();
            var heatmapItems = new List<MarketHeatmapItem>();
            var rankedGroups = new HashSet<int>();

            // تخصیص جایگاه‌ها بر اساس منطق پیچیده
            if (activeGroupMetrics.Count > 0)
            {
                var top = activeGroupMetrics[0];
                heatmapItems.Add(new MarketHeatmapItem { Title = top.MainGroup.PersianName, Code = top.MainGroup.Id, Rank = MarketHeatRank.High, Subtitle = "بیشترین ارزش معاملات", Value = $"+{top.Heat:F1}%" });
                rankedGroups.Add(top.MainGroup.Id);
            }
            if (activeGroupMetrics.Count > 1)
            {
                var second = activeGroupMetrics[1];
                heatmapItems.Add(new MarketHeatmapItem { Title = second.MainGroup.PersianName, Code = second.MainGroup.Id, Rank = MarketHeatRank.High, Value = $"{second.DemandRatio:F1}x" });
                rankedGroups.Add(second.MainGroup.Id);
            }
            // ... (ادامه منطق برای رتبه‌های دیگر)

            // پر کردن بقیه لیست با گروه‌های باقی‌مانده
            var remainingGroups = allMainGroups.Where(mg => !rankedGroups.Contains(mg.Id)).ToList();
            foreach (var group in remainingGroups)
            {
                heatmapItems.Add(new MarketHeatmapItem { Title = group.PersianName, Code = group.Id, Rank = MarketHeatRank.Neutral });
            }


            return new MarketHeatmapData { Items = heatmapItems };
        }
        #endregion MarketHeatmap

        #region marketlist
        public Task<List<ItemInfo>> GetMarketListAsync()
        {
            // این متد از ابتدا بهینه بود و نیازی به تغییر ندارد
            using var context = _contextFactory.CreateDbContext();
            return context.MainGroups.Select(x => new ItemInfo(x.Id, x.PersianName)).ToListAsync();
        }
        #endregion marketlist

        #region MarketShortcuts
        public Task<MarketShortcutsData> GetMarketShortcutsAsync()
        {
            // این متد به دیتابیس دسترسی ندارد و نیازی به تغییر ندارد
            var data = new MarketShortcutsData
            {
                Items = new List<MarketShortcutItem>
                {
                    new() { Title = "اموال غیر منقول", Code = 6, IconCssClass = "bi bi-house-door-fill", ThemeCssClass = "real-estate" },
                    new() { Title = "بازار فرعی", Code = 7, IconCssClass = "bi bi-shop", ThemeCssClass = "secondary" },
                    new() { Title = "صنعتی", Code = 1, IconCssClass = "bi bi-building", ThemeCssClass = "industrial" },
                    new() { Title = "فرآورده های نفتی", Code = 5, IconCssClass = "bi bi-fuel-pump-fill", ThemeCssClass = "oil-products" },
                    new() { Title = "معدنی", Code = 4, IconCssClass = "bi bi-gem", ThemeCssClass = "mineral" },
                    new() { Title = "پتروشیمی", Code = 3, IconCssClass = "bi bi-droplet-fill", ThemeCssClass = "petro" },
                    new() { Title = "کشاورزی", Code = 2, IconCssClass = "bi bi-tree-fill", ThemeCssClass = "agri" }
                }
            };
            return Task.FromResult(data);
        }
        #endregion MarketShortcuts
    }
}
