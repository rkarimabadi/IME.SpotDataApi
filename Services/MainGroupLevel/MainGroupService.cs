using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Services.Caching;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace IME.SpotDataApi.Services.MainGroupLevel
{
    public interface IMainGroupService
    {
        Task<GroupListData> GetActiveGroupsAsync(int mainGroupId);
        Task<MarketConditionsData> GetGroupActivitiesAsync(int mainGroupId);
        Task<UpcomingOffersData> GetUpcomingOffersAsync(int mainGroupId);
        Task<MarketStatsData> GetMarketShareAsync(int mainGroupId);
        Task<MarketStatsData> GetTradeShareAsync(int mainGroupId);
    }

    /// <summary>
    /// نسخه ریفکتور شده با الگوهای بهینه برای اجرای تمام محاسبات در دیتابیس
    /// و جلوگیری از بارگذاری جداول بزرگ در حافظه.
    /// </summary>
    public class MainGroupService : IMainGroupService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly ICacheService _cacheService;
        private readonly IDateHelper _dateHelper;

        public MainGroupService(IDbContextFactory<AppDataContext> contextFactory, IDateHelper dateHelper, ICacheService cacheService)
        {
            _contextFactory = contextFactory;
            _cacheService = cacheService;
            _dateHelper = dateHelper;
        }
        #region TradeShare
        /// <summary>
        /// نرخ تحقق معاملات را برای یک گروه اصلی مشخص بر اساس تعداد، حجم و ارزش محاسبه می‌کند.
        /// </summary>
        /// <param name="mainGroupId">شناسه گروه اصلی مورد نظر</param>
        /// <returns>داده‌های نرخ تحقق معاملات برای نمایش در ویجت آمار</returns>
        public async Task<MarketStatsData> GetTradeShareAsync(int mainGroupId)
        {

            string cacheKey = $"MainGroup_TradeShare_{mainGroupId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var todayPersian = _dateHelper.GetPersian(DateTime.Now);

                // 1. آمار کل عرضه‌های گروه اصلی در روز جاری را محاسبه می‌کنیم (مخرج کسر).
                var offerStatsQuery = context.Offers
                    .Where(o => o.OfferDate == todayPersian)
                    .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => new { o, c })
                    .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.o, sg })
                    .Join(context.Groups, j => j.sg.ParentId, g => g.Id, (j, g) => new { j.o, g })
                    .Where(x => x.g.ParentId == mainGroupId);

                var totalOfferStats = await offerStatsQuery
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        Count = g.Count(),
                        Volume = g.Sum(x => x.o.OfferVol),
                        Value = g.Sum(x => x.o.OfferVol * x.o.InitPrice / 1000) // به هزار ریال
                    })
                    .FirstOrDefaultAsync();

                if (totalOfferStats == null || totalOfferStats.Count == 0)
                {
                    return new MarketStatsData(); // اگر عرضه‌ای وجود نداشته باشد، داده‌ای برای تحلیل نیست.
                }

                // 2. آمار کل معاملات گروه اصلی در روز جاری را محاسبه می‌کنیم (صورت کسر).
                var tradeStatsQuery = context.TradeReports
                    .Where(t => t.TradeDate == todayPersian)
                    .Join(context.Offers, t => t.OfferId, o => o.Id, (t, o) => new { t, o })
                    .Join(context.Commodities, j => j.o.CommodityId, c => c.Id, (j, c) => new { j.t, c })
                    .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.t, sg })
                    .Join(context.Groups, j => j.sg.ParentId, g => g.Id, (j, g) => new { j.t, g })
                    .Where(x => x.g.ParentId == mainGroupId);

                var totalTradeStats = await tradeStatsQuery
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        Count = g.Select(x => x.t.OfferId).Distinct().Count(), // تعداد عرضه‌های معامله شده
                        Volume = g.Sum(x => x.t.TradeVolume),
                        //Value = g.Sum(x => x.t.TradeValue) // این فیلد از قبل به هزار ریال است
                        Value = g.Sum(x => x.t.TradeVolume * x.t.OfferBasePrice / 1000)
                    })
                    .FirstOrDefaultAsync();

                // 3. نرخ تحقق را برای هر معیار محاسبه می‌کنیم.
                var tradedCount = totalTradeStats?.Count ?? 0;
                var tradedVolume = totalTradeStats?.Volume ?? 0;
                var tradedValue = totalTradeStats?.Value ?? 0;

                var countRealization = totalOfferStats.Count > 0 ? (double)tradedCount / totalOfferStats.Count : 0;
                var volumeRealization = totalOfferStats.Volume > 0 ? (double)tradedVolume / (double)totalOfferStats.Volume : 0;
                var valueRealization = totalOfferStats.Value > 0 ? (double)tradedValue / (double)totalOfferStats.Value : 0;

                // 4. مدل خروجی را با مقادیر و وضعیت‌های محاسبه شده ایجاد می‌کنیم.
                var items = new List<MarketStatItem>
            {
                new() {
                    Value = $"{countRealization:P1}",
                    Label = "تحقق تعدادی",
                    IconCssClass = "bi bi-check2-circle",
                    ThemeCssClass = "count-realization",
                    ValueState = GetTradeRealizationValueState(countRealization)
                },
                new() {
                    Value = $"{volumeRealization:P1}",
                    Label = "تحقق حجمی",
                    IconCssClass = "bi bi-box-seam",
                    ThemeCssClass = "volume-realization",
                    ValueState = GetTradeRealizationValueState(volumeRealization)
                },
                new() {
                    Value = $"{valueRealization:P1}",
                    Label = "تحقق ارزشی",
                    IconCssClass = "bi bi-graph-up-arrow",
                    ThemeCssClass = "value-realization",
                    ValueState = GetTradeRealizationValueState(valueRealization)
                }
            };

                return new MarketStatsData { Items = items };
            }, expirationInMinutes: 15);
        }

        /// <summary>
        /// بر اساس درصد نرخ تحقق، وضعیت (مثبت، منفی یا خنثی) را تعیین می‌کند.
        /// </summary>
        private ValueState GetTradeRealizationValueState(double ratio)
        {
            if (ratio > 0.8) return ValueState.Positive; // نرخ تحقق بالای 80% مثبت است
            if (ratio < 0.5) return ValueState.Negative; // نرخ تحقق زیر 50% منفی است
            return ValueState.Neutral;
        }
        #endregion TradeShare


        #region MarketShare
        /// <summary>
        /// سهم یک گروه اصلی از کل اطلاعیه‌های عرضه روز را محاسبه می‌کند.
        /// </summary>
        /// <param name="mainGroupId">شناسه گروه اصلی مورد نظر</param>
        /// <returns>داده‌های سهم بازار برای نمایش در ویجت آمار</returns>
        public async Task<MarketStatsData> GetMarketShareAsync(int mainGroupId)
        {
            string cacheKey = $"MainGroup_MarketShare_{mainGroupId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var todayPersian = _dateHelper.GetPersian(DateTime.Now);

                var todaysOffersQuery = context.Offers
                    .Where(o => o.OfferDate == todayPersian)
                    .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => new { Offer = o, Commodity = c })
                    .Join(context.SubGroups, j => j.Commodity.ParentId, sg => sg.Id, (j, sg) => new { j.Offer, SubGroup = sg })
                    .Join(context.Groups, j => j.SubGroup.ParentId, g => g.Id, (j, g) => new { j.Offer, Group = g })
                    .Select(x => new
                    {
                        MainGroupId = x.Group.ParentId,
                        Volume = x.Offer.OfferVol,
                        Value = x.Offer.OfferVol * x.Offer.InitPrice / 1000
                    });

                var marketTotals = await todaysOffersQuery
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        TotalCount = g.Count(),
                        TotalVolume = g.Sum(x => x.Volume),
                        TotalValue = g.Sum(x => x.Value)
                    })
                    .FirstOrDefaultAsync();

                if (marketTotals == null || marketTotals.TotalCount == 0)
                {
                    return new MarketStatsData();
                }

                var groupTotals = await todaysOffersQuery
                    .Where(x => x.MainGroupId == mainGroupId)
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        GroupCount = g.Count(),
                        GroupVolume = g.Sum(x => x.Volume),
                        GroupValue = g.Sum(x => x.Value)
                    })
                    .FirstOrDefaultAsync();

                var groupOfferCount = groupTotals?.GroupCount ?? 0;
                var groupOfferVolume = groupTotals?.GroupVolume ?? 0;
                var groupOfferValue = groupTotals?.GroupValue ?? 0;

                var countShare = marketTotals.TotalCount > 0 ? (double)groupOfferCount / (double)marketTotals.TotalCount : 0;
                var volumeShare = marketTotals.TotalVolume > 0 ? (double)groupOfferVolume / (double)marketTotals.TotalVolume : 0;
                var valueShare = marketTotals.TotalValue > 0 ? (double)groupOfferValue / (double)marketTotals.TotalValue : 0;

                var items = new List<MarketStatItem>
            {
                new() {
                    Value = $"{countShare:P1}",
                    Label = "سهم از تعداد",
                    IconCssClass = "bi bi-list-ol",
                    ThemeCssClass = "count-share",
                    ValueState = GetShareValueState(countShare) // انتخاب وضعیت پویا
                },
                new() {
                    Value = $"{volumeShare:P1}",
                    Label = "سهم از حجم",
                    IconCssClass = "bi bi-truck",
                    ThemeCssClass = "volume-share",
                    ValueState = GetShareValueState(volumeShare) // انتخاب وضعیت پویا
                },
                new() {
                    Value = $"{valueShare:P1}",
                    Label = "سهم از ارزش",
                    IconCssClass = "bi bi-cash-stack",
                    ThemeCssClass = "value-share",
                    ValueState = GetShareValueState(valueShare) // انتخاب وضعیت پویا
                }
            };

                return new MarketStatsData { Items = items };
            }, expirationInMinutes: 15);
        }

        /// <summary>
        /// بر اساس درصد سهم بازار، وضعیت (مثبت، منفی یا خنثی) را تعیین می‌کند.
        /// </summary>
        private ValueState GetShareValueState(double share)
        {
            if (share > 0.3) return ValueState.Positive; // سهم بالای 30% مثبت تلقی می‌شود
            if (share < 0.1) return ValueState.Negative; // سهم زیر 10% منفی تلقی می‌شود
            return ValueState.Neutral;
        }
        #endregion MarketShare

        #region ActiveGroups
        public async Task<GroupListData> GetActiveGroupsAsync(int mainGroupId)
        {
            string cacheKey = $"MainGroup_ActiveGroups_{mainGroupId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var todayPersian = _dateHelper.GetPersian(DateTime.Now);

                // راه حل پایدار: واکشی یک لیست مسطح با LEFT JOIN برای تمام گروه‌ها و زیرگروه‌های مرتبط
                var flatQuery = from grp in context.Groups.Where(g => g.ParentId == mainGroupId)
                                join subGroup in context.SubGroups on grp.Id equals subGroup.ParentId into subGroupGrouping
                                from subGroup in subGroupGrouping.DefaultIfEmpty()
                                join commodity in context.Commodities on subGroup.Id equals commodity.ParentId into commodityGrouping
                                from commodity in commodityGrouping.DefaultIfEmpty()
                                join offer in context.Offers on commodity.Id equals offer.CommodityId into offerGrouping
                                from offer in offerGrouping.DefaultIfEmpty()
                                select new
                                {
                                    GroupId = grp.Id,
                                    GroupName = grp.PersianName,
                                    SubGroupName = subGroup != null ? subGroup.PersianName : null,
                                    OfferDate = offer != null ? offer.OfferDate : null
                                };

                var flatData = await flatQuery.ToListAsync();

                // سپس گروه‌بندی و محاسبات در حافظه انجام می‌شود
                var groupsData = flatData
                    .GroupBy(x => new { x.GroupId, x.GroupName })
                    .Select(g =>
                    {
                        var validOffers = g.Where(x => x.OfferDate != null).ToList();
                        var validSubGroups = g.Where(x => x.SubGroupName != null).Select(x => x.SubGroupName).Distinct().ToList();

                        return new
                        {
                            GroupId = g.Key.GroupId,
                            GroupName = g.Key.GroupName,
                            TodayOffersCount = validOffers.Count(o => o.OfferDate == todayPersian),
                            FutureOffersCount = validOffers.Count(o => string.Compare(o.OfferDate, todayPersian) > 0),
                            TopSubGroups = validSubGroups.Take(3)
                        };
                    })
                    .ToList();

                var result = new GroupListData();
                foreach (var group in groupsData)
                {
                    GroupActivityStatus status;
                    int? offerCount = null;

                    if (group.TodayOffersCount > 0)
                    {
                        status = GroupActivityStatus.ActiveToday;
                        offerCount = group.TodayOffersCount;
                    }
                    else if (group.FutureOffersCount > 0)
                    {
                        status = GroupActivityStatus.ActiveFuture;
                        offerCount = group.FutureOffersCount;
                    }
                    else
                    {
                        status = GroupActivityStatus.Inactive;
                    }

                    result.Items.Add(new GroupListItem
                    {
                        Title = group.GroupName,
                        UrlName = group.GroupId.ToString(),
                        Status = status,
                        OfferCount = offerCount,
                        Subtitle = string.Join("، ", group.TopSubGroups)
                    });
                }

                return result;
            }, expirationInMinutes: 15);
        }
        #endregion ActiveGroups

        #region GroupActivities
        public async Task<MarketConditionsData> GetGroupActivitiesAsync(int mainGroupId)
        {
            string cacheKey = $"MainGroup_GroupActivities_{mainGroupId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var todayPersian = _dateHelper.GetPersian(DateTime.Now);

                // بهینه‌سازی کامل: تمام محاسبات در یک کوئری واحد در دیتابیس انجام می‌شود.
                var stats = await context.TradeReports
                    .Where(t => t.TradeDate == todayPersian)
                    .Join(context.Offers, t => t.OfferId, o => o.Id, (t, o) => new { t, o })
                    .Join(context.Commodities, j => j.o.CommodityId, c => c.Id, (j, c) => new { j.t, c })
                    .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.t, sg })
                    .Join(context.Groups, j => j.sg.ParentId, g => g.Id, (j, g) => new { j.t, g })
                    .Where(x => x.g.ParentId == mainGroupId)
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        TotalValue = g.Sum(x => x.t.TradeValue * 1000),
                        TotalVolume = g.Sum(x => x.t.TradeVolume),
                        TotalFinalValue = g.Sum(x => x.t.FinalWeightedAveragePrice * x.t.TradeVolume),
                        TotalBaseValue = g.Sum(x => x.t.OfferBasePrice * x.t.TradeVolume),
                        TotalDemand = g.Sum(x => x.t.DemandVolume),
                        TotalSupply = g.Sum(x => x.t.OfferVolume)
                    })
                    .FirstOrDefaultAsync();

                var items = new List<MarketConditionItem>();
                if (stats != null)
                {
                    items.Add(new MarketConditionItem { Title = "ارزش معاملات", Value = (stats.TotalValue / 10_000_000_000_000M).ToString("F1"), Unit = "همت", IconCssClass = "bi bi-cash-stack", IconBgCssClass = "value" });
                    items.Add(new MarketConditionItem { Title = "حجم معاملات", Value = (stats.TotalVolume).ToString("N0"), Unit = "تن", IconCssClass = "bi bi-truck", IconBgCssClass = "volume" });
                    decimal competitionIndex = stats.TotalBaseValue > 0 ? ((stats.TotalFinalValue - stats.TotalBaseValue) / stats.TotalBaseValue) * 100 : 0;
                    items.Add(new MarketConditionItem { Title = "شاخص رقابت", Value = $"{competitionIndex:+#.##;-#.##;0.0}%", IconCssClass = "bi bi-fire", IconBgCssClass = "competition", ValueState = competitionIndex > 0 ? ValueState.Positive : (competitionIndex < 0 ? ValueState.Negative : ValueState.Neutral) });
                    decimal demandRatio = stats.TotalSupply > 0 ? stats.TotalDemand / stats.TotalSupply : 0;
                    items.Add(new MarketConditionItem { Title = "قدرت تقاضا", Value = $"{demandRatio:F1}x", IconCssClass = "bi bi-people", IconBgCssClass = "demand" });
                }

                return new MarketConditionsData { Items = items };
            }, expirationInMinutes: 15);
        }
        #endregion GroupActivities

        #region UpcomingOffers
        public async Task<UpcomingOffersData> GetUpcomingOffersAsync(int mainGroupId)
        {
            string cacheKey = $"MainGroup_UpcomingOffers_{mainGroupId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var todayPersian = _dateHelper.GetPersian(DateTime.Now);

                // بهینه‌سازی کامل: واکشی داده‌های مورد نیاز در یک کوئری واحد.
                var futureOffersData = await context.Offers
                    .Where(o => string.Compare(o.OfferDate, todayPersian) >= 0)
                    .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => new { o, c })
                    .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.o, j.c, sg })
                    .Join(context.Groups, j => j.sg.ParentId, g => g.Id, (j, g) => new { j.o, j.c, g })
                    .Where(x => x.g.ParentId == mainGroupId)
                    .Join(context.Suppliers, x => x.o.SupplierId, s => s.Id, (x, s) => new
                    {
                        OfferId = x.o.Id,
                        OfferDate = x.o.OfferDate,
                        CommodityName = x.c.PersianName,
                        SupplierName = s.PersianName
                    })
                    .OrderBy(x => x.OfferDate)
                    .Take(10)
                    .ToListAsync();

                var items = futureOffersData.Select(data =>
                {
                    var offerDate = _dateHelper.GetGregorian(data.OfferDate);
                    var pc = new PersianCalendar();
                    return new UpcomingOfferItem
                    {
                        DayOfWeek = GetPersianDayOfWeek(offerDate.DayOfWeek),
                        DayOfMonth = pc.GetDayOfMonth(offerDate).ToString(),
                        Title = data.CommodityName ?? "کالای نامشخص",
                        Subtitle = $"توسط {data.SupplierName ?? "عرضه‌کننده نامشخص"}",
                        Type = UpcomingOfferType.Group,
                        UrlName = data.OfferId.ToString()
                    };
                }).ToList();

                return new UpcomingOffersData { Items = items };
            }, expirationInMinutes: 15);
        }

        private string GetPersianDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Saturday => "شنبه",
                DayOfWeek.Sunday => "یکشنبه",
                DayOfWeek.Monday => "دوشنبه",
                DayOfWeek.Tuesday => "سه‌شنبه",
                DayOfWeek.Wednesday => "چهارشنبه",
                DayOfWeek.Thursday => "پنج‌شنبه",
                DayOfWeek.Friday => "جمعه",
                _ => ""
            };
        }

        #endregion UpcomingOffers
    }
}
