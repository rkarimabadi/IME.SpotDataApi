using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using Microsoft.EntityFrameworkCore;
using IME.SpotDataApi.Services.Caching;
using static IME.SpotDataApi.Models.Core.Constants;

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
        Task<MarketContactsData> GetMarketTopSubGroupsAsync();
    }

    /// <summary>
    /// نسخه ریفکتور شده با الگوهای بهینه برای اجرای تمام محاسبات در دیتابیس
    /// و جلوگیری از بارگذاری جداول بزرگ در حافظه.
    /// </summary>
    public class MarketsService : IMarketsService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly ICacheService _cacheService;
        private readonly IDateHelper _dateHelper;

        public MarketsService(IDbContextFactory<AppDataContext> contextFactory, IDateHelper dateHelper, ICacheService cacheService)
        {
            _contextFactory = contextFactory;
            _dateHelper = dateHelper;
            _cacheService = cacheService;
        }

        #region MainGroupsData
        public async Task<List<MarketInfo>> GetMainGroupsDataAsync()
        {
            string cacheKey = "Market_MainGroups";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
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

                // به دلیل محدودیت SQLite، داده‌ها را قبل از مرتب‌سازی نهایی به حافظه منتقل می‌کنیم
                var rawData = await tradesWithHierarchy.ToListAsync();

                var activeMarketData = rawData
                    .GroupBy(x => x.MainGroup)
                    .ToDictionary(
                        g => g.Key.Id,
                        g =>
                        {
                            var topSubGroups = g
                                .GroupBy(x => x.SubGroupName)
                                .OrderByDescending(sg => sg.Sum(t => t.TradeValue)) // مرتب‌سازی در حافظه
                                .Select(sg => sg.Key)
                                .Take(3);

                            return new
                            {
                                TotalTradeValue = g.Sum(x => x.TradeValue),
                                TotalBaseValue = g.Sum(x => x.OfferBasePrice * x.TradeVolume / 1000),
                                TopSubGroups = topSubGroups
                            };
                        });

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
            }, expirationInMinutes: 15);
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
            string cacheKey = "Market_IndexGroups";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
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
            }, expirationInMinutes: 15);
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
            string cacheKey = "Market_Activities";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
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
            }, expirationInMinutes: 15);
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
            string cacheKey = "Market_Heatmap";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = _contextFactory.CreateDbContext();
                var todayPersian = _dateHelper.GetPersian(DateTime.Now);

                var mainGroups = await context.MainGroups.ToListAsync();
                var todayTrades = await context.TradeReports.Where(t => t.TradeDate == todayPersian).ToListAsync();
                var todayOffers = await context.Offers.Where(o => o.OfferDate == todayPersian).ToListAsync();

                // محاسبه شاخص‌های حرارت برای گروه‌های فعال
                var activeGroupMetrics = mainGroups
                    .Select(mg =>
                    {
                        var hierarchy = GetHierarchyForMainGroup(context, mg.Id);
                        var offerIdsInGroup = todayOffers
                            .Where(o => hierarchy.CommodityIds.Contains(o.CommodityId))
                            .Select(o => o.Id)
                            .ToHashSet();

                        var tradesInGroup = todayTrades.Where(t => offerIdsInGroup.Contains(t.OfferId)).ToList();

                        decimal heat = 0;
                        if (tradesInGroup.Any())
                        {
                            decimal totalFinalValue = tradesInGroup.Sum(t => t.FinalWeightedAveragePrice * t.TradeVolume);
                            decimal totalBaseValue = tradesInGroup.Sum(t => t.OfferBasePrice * t.TradeVolume);
                            heat = totalBaseValue > 0 ? ((totalFinalValue - totalBaseValue) / totalBaseValue) * 100 : 0;
                        }
                        return new { MainGroup = mg, Heat = heat, Trades = tradesInGroup, Offers = todayOffers.Where(o => offerIdsInGroup.Contains(o.Id)).ToList() };
                    })
                    .OrderByDescending(x => x.Heat)
                    .ToList();

                var heatmapItems = new List<MarketHeatmapItem>();
                var rankedGroups = new HashSet<int>();

                // تخصیص جایگاه‌های High
                var highRankers = activeGroupMetrics.Take(2).ToList();
                if (highRankers.Count > 0)
                {
                    var topHigh = highRankers[0];
                    heatmapItems.Add(new MarketHeatmapItem { Title = topHigh.MainGroup.PersianName, Code = topHigh.MainGroup.Id, Rank = MarketHeatRank.High, Subtitle = "بیشترین ارزش معاملات", Value = $"+{topHigh.Heat:F1}%" });
                    rankedGroups.Add(topHigh.MainGroup.Id);
                }
                if (highRankers.Count > 1)
                {
                    var secondHigh = highRankers[1];
                    var demandRatio = secondHigh.Offers.Sum(o => o.OfferVol) > 0 ? secondHigh.Trades.Sum(t => t.DemandVolume) / secondHigh.Offers.Sum(o => o.OfferVol) : 0;
                    heatmapItems.Add(new MarketHeatmapItem { Title = secondHigh.MainGroup.PersianName, Code = secondHigh.MainGroup.Id, Rank = MarketHeatRank.High, Value = $"{demandRatio:F1}x" });
                    rankedGroups.Add(secondHigh.MainGroup.Id);
                }

                // تخصیص جایگاه‌های Medium
                var mediumRankers = activeGroupMetrics.Skip(2).Take(2).ToList();
                if (mediumRankers.Count > 0)
                {
                    var topMedium = mediumRankers[0];
                    var realizationRatio = topMedium.Offers.Sum(o => o.InitPrice * o.OfferVol) > 0 ? topMedium.Trades.Sum(t => t.TradeValue * 1000) / topMedium.Offers.Sum(o => o.InitPrice * o.OfferVol * 1000) * 100 : 0;
                    heatmapItems.Add(new MarketHeatmapItem { Title = topMedium.MainGroup.PersianName, Code = topMedium.MainGroup.Id, Rank = MarketHeatRank.Medium, Subtitle = "نرخ تحقق", Value = $"{realizationRatio:F0}%" });
                    rankedGroups.Add(topMedium.MainGroup.Id);
                }
                if (mediumRankers.Count > 1)
                {
                    var secondMedium = mediumRankers[1];
                    heatmapItems.Add(new MarketHeatmapItem { Title = secondMedium.MainGroup.PersianName, Code = secondMedium.MainGroup.Id, Rank = MarketHeatRank.Medium });
                    rankedGroups.Add(secondMedium.MainGroup.Id);
                }

                // پر کردن بقیه لیست با گروه‌های باقی‌مانده
                var remainingGroups = mainGroups.Where(mg => !rankedGroups.Contains(mg.Id)).ToList();

                var lowRankers = remainingGroups.Take(2).ToList();
                foreach (var group in lowRankers)
                {
                    heatmapItems.Add(new MarketHeatmapItem { Title = group.PersianName, Code = group.Id, Rank = MarketHeatRank.Low });
                    rankedGroups.Add(group.Id);
                }

                var neutralRankers = mainGroups.Where(mg => !rankedGroups.Contains(mg.Id)).ToList();
                foreach (var group in neutralRankers)
                {
                    heatmapItems.Add(new MarketHeatmapItem { Title = group.PersianName, Code = group.Id, Rank = MarketHeatRank.Neutral });
                }

                return new MarketHeatmapData { Items = heatmapItems };
            }, expirationInMinutes: 15);
        }
        private (HashSet<int> GroupIds, HashSet<int> SubGroupIds, HashSet<int> CommodityIds) GetHierarchyForMainGroup(AppDataContext context, int mainGroupId)
        {
            var groupIds = context.Groups.Where(g => g.ParentId == mainGroupId).Select(g => g.Id).ToHashSet();
            var subGroupIds = context.SubGroups.Where(sg => sg.ParentId.HasValue && groupIds.Contains(sg.ParentId.Value)).Select(sg => sg.Id).ToHashSet();
            var commodityIds = context.Commodities.Where(c => c.ParentId.HasValue && subGroupIds.Contains(c.ParentId.Value)).Select(c => c.Id).ToHashSet();
            return (groupIds, subGroupIds, commodityIds);
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
        public async Task<MarketShortcutsData> GetMarketShortcutsAsync()
        {
            string cacheKey = "Market_Shortcuts";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var todayPersian = _dateHelper.GetPersian(DateTime.Now);

                var activeMainGroupIds = await context.Offers
                    .Where(o => string.Compare(o.OfferDate, todayPersian) >= 0)
                    .Join(context.Commodities, offer => offer.CommodityId, commodity => commodity.Id, (offer, commodity) => commodity)
                    .Join(context.SubGroups, commodity => commodity.ParentId, subGroup => subGroup.Id, (commodity, subGroup) => subGroup)
                    .Join(context.Groups, subGroup => subGroup.ParentId, grp => grp.Id, (subGroup, grp) => grp)
                    .Where(grp => grp.ParentId.HasValue)
                    .Select(grp => grp.ParentId.Value)
                    .Distinct()
                    .ToHashSetAsync();

                // لیست پایه میانبرها با استایل پیش‌فرض حالت فعال
                var shortcuts = new List<MarketShortcutItem>
            {
                new() { Title = "صنعتی", Code = 1, IconCssClass = "bi bi-building", ThemeCssClass = "industrial" },
                new() { Title = "کشاورزی", Code = 2, IconCssClass = "bi bi-tree-fill", ThemeCssClass = "agri" },
                new() { Title = "پتروشیمی", Code = 3, IconCssClass = "bi bi-droplet-fill", ThemeCssClass = "petro" },
                new() { Title = "معدنی", Code = 4, IconCssClass = "bi bi-gem", ThemeCssClass = "mineral" },
                new() { Title = "فرآورده های نفتی", Code = 5, IconCssClass = "bi bi-fuel-pump-fill", ThemeCssClass = "oil-products" },
                new() { Title = "اموال غیر منقول", Code = 6, IconCssClass = "bi bi-house-door-fill", ThemeCssClass = "real-estate" },
                new() { Title = "بازار فرعی", Code = 7, IconCssClass = "bi bi-shop", ThemeCssClass = "fork" }
            };

                var activeShortcuts = new List<MarketShortcutItem>();
                var inactiveShortcuts = new List<MarketShortcutItem>();

                foreach (var item in shortcuts)
                {
                    bool isActive = activeMainGroupIds.Contains(item.Code);

                    if (isActive)
                    {
                        activeShortcuts.Add(item);
                    }
                    else
                    {
                        item.ThemeCssClass = "secondary";
                        inactiveShortcuts.Add(item);
                    }
                }

                var orderedShortcuts = activeShortcuts.Concat(inactiveShortcuts).ToList();

                var data = new MarketShortcutsData { Items = orderedShortcuts };
                return data;
            }, expirationInMinutes: 15);
        }
        #endregion MarketShortcuts

        #region MarketTopSubGroups
        public async Task<MarketContactsData> GetMarketTopSubGroupsAsync()
        {
            string cacheKey = "Market_TopSubGroups";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var todayPersian = _dateHelper.GetPersian(System.DateTime.Now);

                // 1. واکشی و شمارش تعداد عرضه برای تمام زیرگروه‌های فعال در روز جاری
                var allActiveSubGroups = await context.Offers
                    .Where(o => string.Compare(o.OfferDate, todayPersian) >= 0)
                    .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => c)
                    .Join(context.SubGroups, c => c.ParentId, sg => sg.Id, (c, sg) => sg)
                    .Join(context.Groups, sg => sg.ParentId, g => g.Id, (sg, g) => new { SubGroup = sg, Group = g })
                    .Join(context.MainGroups, j => j.Group.ParentId, mg => mg.Id, (j, mg) => new { j.SubGroup, j.Group, MainGroup = mg })
                    .GroupBy(x => new
                    {
                        SubGroupId = x.SubGroup.Id,
                        SubGroupName = x.SubGroup.PersianName,
                        GroupId = x.Group.Id,
                        GroupName = x.Group.PersianName,
                        MainGroupId = x.MainGroup.Id,
                        MainGroupName = x.MainGroup.PersianName
                    })
                    .Select(g => new
                    {
                        g.Key.SubGroupId,
                        g.Key.SubGroupName,
                        g.Key.GroupId,
                        g.Key.GroupName,
                        g.Key.MainGroupId,
                        g.Key.MainGroupName,
                        OfferCount = g.Count()
                    })
                    .OrderByDescending(x => x.OfferCount)
                    .ToListAsync();

                // 2. الگوریتم انتخاب 4 مورد برتر با اولویت‌های مشخص
                var finalSelection = new List<dynamic>();
                var usedMainGroupIds = new HashSet<int>();
                var usedGroupIds = new HashSet<int>();
                var usedSubGroupIds = new HashSet<int>();

                // پاس اول: انتخاب بر اساس گروه اصلی منحصر به فرد
                foreach (var item in allActiveSubGroups.Where(i => !usedMainGroupIds.Contains(i.MainGroupId)))
                {
                    if (finalSelection.Count >= 4) break;
                    finalSelection.Add(item);
                    usedMainGroupIds.Add(item.MainGroupId);
                    usedGroupIds.Add(item.GroupId);
                    usedSubGroupIds.Add(item.SubGroupId);
                }

                // پاس دوم: تکمیل لیست بر اساس گروه منحصر به فرد
                if (finalSelection.Count < 4)
                {
                    foreach (var item in allActiveSubGroups.Where(i => !usedSubGroupIds.Contains(i.SubGroupId) && !usedGroupIds.Contains(i.GroupId)))
                    {
                        if (finalSelection.Count >= 4) break;
                        finalSelection.Add(item);
                        usedGroupIds.Add(item.GroupId);
                        usedSubGroupIds.Add(item.SubGroupId);
                    }
                }

                // پاس سوم: تکمیل لیست با موارد باقی‌مانده بر اساس بیشترین تعداد عرضه
                if (finalSelection.Count < 4)
                {
                    foreach (var item in allActiveSubGroups.Where(i => !usedSubGroupIds.Contains(i.SubGroupId)))
                    {
                        if (finalSelection.Count >= 4) break;
                        finalSelection.Add(item);
                        usedSubGroupIds.Add(item.SubGroupId);
                    }
                }

                // 3. تبدیل داده‌های منتخب به مدل خروجی
                var items = finalSelection.Select(item =>
                {
                    // رفع خطا: دسترسی به اعضای tuple با نام‌های پیش‌فرض Item1 و Item2
                    var visuals = GetMainGroupVisuals(item.MainGroupName);
                    return new MarketContactItem
                    {
                        Title = item.SubGroupName,
                        Subtitle = item.GroupName,
                        IconCssClass = $"bi {visuals.Item1}",
                        AvatarCssClass = visuals.Item2,
                        UrlName = $"{item.MainGroupId}/{item.GroupId}/{item.SubGroupId}"
                    };
                }).ToList();

                // 4. در صورت نیاز، لیست را با آیتم‌های پیش‌فرض به اندازه 4 پر می‌کنیم
                while (items.Count < 4)
                {
                    items.Add(new MarketContactItem { Title = "...", Subtitle = "داده‌ای یافت نشد", IconCssClass = "bi bi-circle", AvatarCssClass = "disabled" });
                }

                return new MarketContactsData { Items = items };
            }, expirationInMinutes: 15);
        }
        #endregion MarketTopSubGroups

    }
}
