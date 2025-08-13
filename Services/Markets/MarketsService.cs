using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Models.Spot;
using Microsoft.EntityFrameworkCore;
using System;
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
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var todayTrades = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian)
                .ToListAsync();
            
            var commodities = await context.Commodities.ToDictionaryAsync(c => c.Id);
            var subGroups = await context.SubGroups.ToDictionaryAsync(s => s.Id);
            var groups = await context.Groups.ToDictionaryAsync(g => g.Id);
            var mainGroups = await context.MainGroups.ToListAsync();

            // محاسبه داده‌های بازار فقط برای گروه‌هایی که امروز معامله داشته‌اند
            var activeMarketData = todayTrades
                .Select(trade => {
                    var commodity = commodities.GetValueOrDefault(trade.CommodityId);
                    var subGroup = commodity?.ParentId.HasValue == true ? subGroups.GetValueOrDefault(commodity.ParentId.Value) : null;
                    var group = subGroup?.ParentId.HasValue == true ? groups.GetValueOrDefault(subGroup.ParentId.Value) : null;
                    var mainGroup = group?.ParentId.HasValue == true ? mainGroups.FirstOrDefault(mg => mg.Id == group.ParentId.Value) : null;
                    return new { Trade = trade, MainGroup = mainGroup, SubGroup = subGroup };
                })
                .Where(x => x.MainGroup != null)
                .GroupBy(x => x.MainGroup)
                .Select(g => {
                    var mainGroup = g.Key;
                    var tradesInGroup = g.Select(x => x.Trade).ToList();
                    
                    decimal totalTradeValue = tradesInGroup.Sum(t => t.TradeValue);
                    decimal totalBaseValue = tradesInGroup.Sum(t => t.OfferBasePrice * t.TradeVolume / 1000);
                    decimal marketHeat = totalBaseValue > 0 ? ((totalTradeValue - totalBaseValue) / totalBaseValue) * 100 : 0;

                    var topSubGroups = g
                        .Where(x => x.SubGroup != null)
                        .GroupBy(x => x.SubGroup.PersianName)
                        .OrderByDescending(sg => sg.Sum(t => t.Trade.TradeValue))
                        .Take(3)
                        .Select(sg => sg.Key)
                        .ToList();

                    return new {
                        MainGroupId = mainGroup.Id,
                        MarketHeat = marketHeat,
                        Subtitles = topSubGroups
                    };
                })
                .ToDictionary(d => d.MainGroupId);

            // ایجاد لیست نهایی با تمام گروه‌های اصلی
            return mainGroups.Select(mainGroup => {
                string subtitle;
                string heatLevel;
                string heatLabel;

                if (activeMarketData.TryGetValue(mainGroup.Id, out var data))
                {
                    // اگر گروه فعال بوده، از داده‌های محاسبه‌شده استفاده کن
                    subtitle = string.Join("، ", data.Subtitles);
                    (heatLevel, heatLabel) = GetHeatLevel(data.MarketHeat);
                }
                else
                {
                    // اگر گروه فعال نبوده، از مقادیر پیش‌فرض استفاده کن
                    subtitle = GetDefaultSubtitle(mainGroup.PersianName);
                    heatLevel = "neutral";
                    heatLabel = "خنثی";
                }

                var (iconClass, iconContainerClass) = GetMainGroupVisuals(mainGroup.PersianName);
                
                return new MarketInfo(
                    mainGroup.PersianName,
                    mainGroup.Id,
                    subtitle,
                    mainGroup.KeyName, // UrlName
                    iconClass,
                    iconContainerClass,
                    heatLevel,
                    heatLabel
                );
            }).ToList();
        }

        private (string HeatLevel, string HeatLabel) GetHeatLevel(decimal heat)
        {
            if (heat > 10) return ("high", "داغ");
            if (heat > 3) return ("medium", "متوسط");
            if (heat < -3) return ("low", "آرام"); // اصلاح برای در نظر گرفتن رقابت منفی
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
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var todayOffers = await context.Offers
                .Where(o => o.OfferDate == todayPersian)
                .ToListAsync();

            if (!todayOffers.Any())
            {
                return new CommodityStatusData();
            }

            var commodities = await context.Commodities.ToDictionaryAsync(c => c.Id);
            var subGroups = await context.SubGroups.ToDictionaryAsync(s => s.Id);
            var groups = await context.Groups.ToDictionaryAsync(g => g.Id);
            var mainGroups = await context.MainGroups.ToDictionaryAsync(m => m.Id);
            
            var todayTrades = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian)
                .ToListAsync();

            // ایجاد یک ساختار برای اتصال هر عرضه به گروه و گروه اصلی خود
            var offersWithHierarchy = todayOffers
                .Select(offer => {
                    var commodity = commodities.GetValueOrDefault(offer.CommodityId);
                    var subGroup = commodity?.ParentId.HasValue == true ? subGroups.GetValueOrDefault(commodity.ParentId.Value) : null;
                    var group = subGroup?.ParentId.HasValue == true ? groups.GetValueOrDefault(subGroup.ParentId.Value) : null;
                    var mainGroup = group?.ParentId.HasValue == true ? mainGroups.GetValueOrDefault(group.ParentId.Value) : null;
                    return new { Offer = offer, Group = group, MainGroup = mainGroup };
                })
                .Where(x => x.Group != null && x.MainGroup != null)
                .ToList();

            // پیدا کردن ۵ گروه برتر بر اساس تعداد عرضه
            var topGroups = offersWithHierarchy
                .GroupBy(x => x.Group)
                .Select(g => new { Group = g.Key, OfferCount = g.Count(), OfferIds = g.Select(x => x.Offer.Id).ToHashSet() })
                .OrderByDescending(x => x.OfferCount)
                .Take(5)
                .ToList();

            var items = new List<CommodityStatusItem>();

            foreach (var topGroup in topGroups)
            {
                var mainGroup = mainGroups.GetValueOrDefault(topGroup.Group.ParentId ?? 0);

                // محاسبه وضعیت تقاضا برای گروه
                var tradesForGroup = todayTrades.Where(t => topGroup.OfferIds.Contains(t.OfferId)).ToList();
                var totalDemand = tradesForGroup.Sum(t => t.DemandVolume);
                var totalSupply = tradesForGroup.Sum(t => t.OfferVolume);
                
                items.Add(new CommodityStatusItem
                {
                    Name = topGroup.Group.PersianName,
                    MainGroupName = mainGroup?.PersianName ?? "نامشخص",
                    OfferCount = topGroup.OfferCount,
                    DemandStatus = GetDemandStatus(totalDemand, totalSupply)
                });
            }

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
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var todayOffers = await context.Offers
                .Where(o => o.OfferDate == todayPersian)
                .ToListAsync();
            
            var todayTradedOfferIds = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian)
                .Select(t => t.OfferId)
                .Distinct()
                .ToListAsync();

            var todayTrades = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian)
                .ToListAsync();

            var commodities = await context.Commodities.ToDictionaryAsync(c => c.Id);
            var subGroups = await context.SubGroups.ToDictionaryAsync(s => s.Id);
            var groups = await context.Groups.ToDictionaryAsync(g => g.Id);
            var mainGroups = await context.MainGroups.ToDictionaryAsync(m => m.Id);

            var activities = todayOffers
                .Select(offer => {
                    var commodity = commodities.GetValueOrDefault(offer.CommodityId);
                    var subGroup = commodity?.ParentId.HasValue == true ? subGroups.GetValueOrDefault(commodity.ParentId.Value) : null;
                    var group = subGroup?.ParentId.HasValue == true ? groups.GetValueOrDefault(subGroup.ParentId.Value) : null;
                    var mainGroup = group?.ParentId.HasValue == true ? mainGroups.GetValueOrDefault(group.ParentId.Value) : null;
                    return new { Offer = offer, MainGroup = mainGroup };
                })
                .Where(x => x.MainGroup != null)
                .GroupBy(x => x.MainGroup)
                .Select(g => {
                    var mainGroup = g.Key;
                    var offersInGroup = g.Select(x => x.Offer).ToList();
                    var tradedOffersCount = offersInGroup.Count(o => todayTradedOfferIds.Contains(o.Id));
                    var totalOffersCount = offersInGroup.Count;
                    
                    var offerIdsInGroup = offersInGroup.Select(o => o.Id).ToHashSet();
                    var totalValue = todayTrades
                        .Where(t => offerIdsInGroup.Contains(t.OfferId))
                        .Sum(t => t.TradeValue * 1000);

                    return new {
                        MainGroup = mainGroup,
                        TotalOffers = totalOffersCount,
                        TradedOffers = tradedOffersCount,
                        TotalValue = totalValue
                    };
                })
                .OrderByDescending(x => x.TotalOffers)
                .Take(3)
                .Select(x => {
                    var percentage = x.TotalOffers > 0 ? (double)x.TradedOffers / x.TotalOffers : 0;
                    return new MarketActivity(
                        x.MainGroup.PersianName,
                        GetMainGroupVisuals(x.MainGroup.PersianName).IconContainerClass,
                        percentage,
                        $"{percentage:P0} انجام شده",
                        FormatLargeNumber(x.TotalValue)
                    );
                })
                .ToList();

            // اگر کمتر از ۳ گروه فعال بود، لیست را با آیتم‌های خالی پر کن
            while (activities.Count < 3)
            {
                activities.Add(new MarketActivity("نامشخص", "disabled", 0, "بدون عرضه", "۰"));
            }

            return activities;
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
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var mainGroups = await context.MainGroups.ToListAsync();
            var todayTrades = await context.TradeReports.Where(t => t.TradeDate == todayPersian).ToListAsync();
            var todayOffers = await context.Offers.Where(o => o.OfferDate == todayPersian).ToListAsync();

            // محاسبه شاخص‌های حرارت برای گروه‌های فعال
            var activeGroupMetrics = mainGroups
                .Select(mg => {
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
            foreach(var group in lowRankers)
            {
                heatmapItems.Add(new MarketHeatmapItem { Title = group.PersianName, Code = group.Id, Rank = MarketHeatRank.Low });
                rankedGroups.Add(group.Id);
            }

            var neutralRankers = mainGroups.Where(mg => !rankedGroups.Contains(mg.Id)).ToList();
            foreach(var group in neutralRankers)
            {
                heatmapItems.Add(new MarketHeatmapItem { Title = group.PersianName, Code = group.Id, Rank = MarketHeatRank.Neutral });
            }

            return new MarketHeatmapData { Items = heatmapItems };
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
            using var context = _contextFactory.CreateDbContext();
            return context.MainGroups.Select(x => new ItemInfo(x.Id, x.PersianName)).ToListAsync();
        }
        #endregion marketlist

        #region MarketShortcuts
        public Task<MarketShortcutsData> GetMarketShortcutsAsync()
        {
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
