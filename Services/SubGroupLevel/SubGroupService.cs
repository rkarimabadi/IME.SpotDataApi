using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Models.Spot;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IME.SpotDataApi.Services.MainGroupLevel
{
    /// <summary>
    /// این نسخه از سرویس برای افزایش پرفورمنس ریفکتور شده است.
    /// تغییرات اصلی شامل بهینه‌سازی کوئری‌های دیتابیس برای کاهش بار و افزایش سرعت است.
    /// </summary>
    public class SubGroupService : ISubGroupService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly IDateHelper _dateHelper;

        public SubGroupService(IDbContextFactory<AppDataContext> contextFactory, IDateHelper dateHelper)
        {
            _contextFactory = contextFactory;
            _dateHelper = dateHelper;
        }
        #region SubGroupHeader and SubGroupHierarchy
        /// <summary>
        /// اطلاعات هدر یک زیرگروه کالا را واکشی می‌کند.
        /// </summary>
        public async Task<SubGroupHeaderData> GetSubGroupHeaderDataAsync(int subGroupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var headerInfo = await (from subGroup in context.SubGroups.Where(sg => sg.Id == subGroupId)
                                    join grp in context.Groups on subGroup.ParentId equals grp.Id
                                    join mainGroup in context.MainGroups on grp.ParentId equals mainGroup.Id
                                    select new
                                    {
                                        SubGroupName = subGroup.PersianName,
                                        GroupName = grp.PersianName,
                                        MainGroupId = mainGroup.Id
                                    })
                                    .FirstOrDefaultAsync();

            return new SubGroupHeaderData
            {
                SubGroupName = headerInfo.SubGroupName,
                GroupName = headerInfo.GroupName,
                IconCssClass = GetIconForMainGroup(headerInfo.MainGroupId)
            };
        }

        /// <summary>
        /// ساختار سلسله مراتبی یک زیرگروه کالا را ایجاد می‌کند.
        /// </summary>
        public async Task<List<HierarchyItem>> GetSubGroupHierarchyAsync(int subGroupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var hierarchy = new List<HierarchyItem>();

            var queryResult = await (from subGroup in context.SubGroups.Where(sg => sg.Id == subGroupId)
                                     join grp in context.Groups on subGroup.ParentId equals grp.Id
                                     join mainGroup in context.MainGroups on grp.ParentId equals mainGroup.Id
                                     select new
                                     {
                                         SubGroup = new { subGroup.Id, subGroup.PersianName },
                                         Group = new { grp.Id, grp.PersianName },
                                         MainGroup = new { mainGroup.Id, mainGroup.PersianName }
                                     }).FirstOrDefaultAsync();

            if (queryResult != null)
            {
                hierarchy.Add(new HierarchyItem { Id = queryResult.MainGroup.Id, Type = "MainGroup", Name = queryResult.MainGroup.PersianName, IsActive = true });
                hierarchy.Add(new HierarchyItem { Id = queryResult.Group.Id, Type = "Group", Name = queryResult.Group.PersianName, IsActive = true });
                hierarchy.Add(new HierarchyItem { Id = queryResult.SubGroup.Id, Type = "SubGroup", Name = queryResult.SubGroup.PersianName, IsActive = false });
            }

            return hierarchy;
        }

        private string GetIconForMainGroup(int mainGroupId)
        {
            return mainGroupId switch
            {
                1 => "bi bi-building",         // صنعتی
                2 => "bi bi-tree-fill",        // کشاورزی
                3 => "bi bi-droplet-fill",     // پتروشیمی
                4 => "bi bi-gem",              // معدنی
                5 => "bi bi-fuel-pump-fill",   // فرآورده های نفتی
                6 => "bi bi-house-door-fill",  // اموال غیر منقول
                7 => "bi bi-shop",             // بازار فرعی
                _ => "bi bi-box"               // آیکون پیش‌فرض
            };
        }
        #endregion

        #region ActiveCommodities
        public async Task<GroupListData> GetActiveCommoditiesAsync(int subGroupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // راه حل پایدار: برای جلوگیری از خطای ترجمه کوئری، ابتدا یک لیست مسطح
            // از کالاها و عرضه‌های مرتبط با آن‌ها را با یک LEFT JOIN ساده واکشی می‌کنیم.
            var flatQuery = from commodity in context.Commodities.Where(c => c.ParentId == subGroupId)
                            join offerInfo in (
                                context.Offers.Join(
                                    context.Suppliers,
                                    o => o.SupplierId,
                                    s => s.Id,
                                    (o, s) => new { Offer = o, SupplierName = s.PersianName }
                                )
                            ) on commodity.Id equals offerInfo.Offer.CommodityId into grouping
                            from offerInfo in grouping.DefaultIfEmpty() // این دستور LEFT JOIN را ایجاد می‌کند
                            select new
                            {
                                CommodityId = commodity.Id,
                                CommodityName = commodity.PersianName,
                                OfferDate = offerInfo != null ? offerInfo.Offer.OfferDate : null,
                                SupplierName = offerInfo != null ? offerInfo.SupplierName : null
                            };

            var flatData = await flatQuery.ToListAsync();

            // سپس عملیات گروه‌بندی را در حافظه انجام می‌دهیم که سریع و قابل اطمینان است.
            var commoditiesData = flatData
                .GroupBy(x => new { x.CommodityId, x.CommodityName })
                .Select(g =>
                {
                    // ردیف‌های نال ناشی از LEFT JOIN برای کالاهایی که عرضه نداشته‌اند را فیلتر می‌کنیم
                    var validOffers = g.Where(x => x.OfferDate != null).ToList();

                    return new
                    {
                        CommodityId = g.Key.CommodityId,
                        CommodityName = g.Key.CommodityName,
                        TodayOffersCount = validOffers.Count(o => o.OfferDate == todayPersian),
                        FutureOffersCount = validOffers.Count(o => string.Compare(o.OfferDate, todayPersian) > 0),
                        TopSuppliers = validOffers.Select(o => o.SupplierName).Distinct().Take(3)
                    };
                })
                .ToList();

            var result = new GroupListData();
            foreach (var commodity in commoditiesData)
            {
                GroupActivityStatus status;
                int? offerCount = null;

                if (commodity.TodayOffersCount > 0)
                {
                    status = GroupActivityStatus.ActiveToday;
                    offerCount = commodity.TodayOffersCount;
                }
                else if (commodity.FutureOffersCount > 0)
                {
                    status = GroupActivityStatus.ActiveFuture;
                    offerCount = commodity.FutureOffersCount;
                }
                else
                {
                    status = GroupActivityStatus.Inactive;
                }

                result.Items.Add(new GroupListItem
                {
                    Title = commodity.CommodityName,
                    UrlName = commodity.CommodityId.ToString(),
                    Status = status,
                    OfferCount = offerCount,
                    Subtitle = string.Join("، ", commodity.TopSuppliers)
                });
            }

            return result;
        }
        #endregion ActiveCommodities

        #region CommodityActivities
        public async Task<MarketConditionsData> GetCommodityActivitiesAsync(int subGroupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // بازنویسی با الگوی پایدار: ابتدا داده‌های مورد نیاز را با Join واکشی می‌کنیم.
            var tradesData = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian)
                .Join(context.Offers,
                    trade => trade.OfferId,
                    offer => offer.Id,
                    (trade, offer) => new { trade, offer })
                .Join(context.Commodities,
                    joined => joined.offer.CommodityId,
                    commodity => commodity.Id,
                    (joined, commodity) => new { joined.trade, commodity }) // فقط فیلدهای لازم را نگه می‌داریم
                .Where(x => x.commodity.ParentId == subGroupId)
                .Select(x => new {
                    x.trade.TradeValue,
                    x.trade.TradeVolume,
                    x.trade.FinalWeightedAveragePrice,
                    x.trade.OfferBasePrice,
                    x.trade.DemandVolume,
                    x.trade.OfferVolume
                })
                .ToListAsync();

            var items = new List<MarketConditionItem>();
            if (tradesData.Any())
            {
                // سپس محاسبات تجمعی را در حافظه انجام می‌دهیم.
                var stats = new
                {
                    TotalValue = tradesData.Sum(x => x.TradeValue * 1000),
                    TotalVolume = tradesData.Sum(x => x.TradeVolume),
                    TotalFinalValue = tradesData.Sum(x => x.FinalWeightedAveragePrice * x.TradeVolume),
                    TotalBaseValue = tradesData.Sum(x => x.OfferBasePrice * x.TradeVolume),
                    TotalDemand = tradesData.Sum(x => x.DemandVolume),
                    TotalSupply = tradesData.Sum(x => x.OfferVolume)
                };

                // 1. ارزش معاملات
                items.Add(new MarketConditionItem { Title = "ارزش معاملات", Value = (stats.TotalValue / 10_000_000_000_000M).ToString("F1"), Unit = "همت", IconCssClass = "bi bi-cash-stack", IconBgCssClass = "value" });
                // 2. حجم معاملات
                items.Add(new MarketConditionItem { Title = "حجم معاملات", Value = (stats.TotalVolume).ToString("N0"), Unit = "تن", IconCssClass = "bi bi-truck", IconBgCssClass = "volume" });
                // 3. شاخص رقابت
                decimal competitionIndex = stats.TotalBaseValue > 0 ? ((stats.TotalFinalValue - stats.TotalBaseValue) / stats.TotalBaseValue) * 100 : 0;
                items.Add(new MarketConditionItem { Title = "شاخص رقابت", Value = $"{competitionIndex:+#.##;-#.##;0.0}%", IconCssClass = "bi bi-fire", IconBgCssClass = "competition", ValueState = competitionIndex > 0 ? ValueState.Positive : (competitionIndex < 0 ? ValueState.Negative : ValueState.Neutral) });
                // 4. قدرت تقاضا
                decimal demandRatio = stats.TotalSupply > 0 ? stats.TotalDemand / stats.TotalSupply : 0;
                items.Add(new MarketConditionItem { Title = "قدرت تقاضا", Value = $"{demandRatio:F1}x", IconCssClass = "bi bi-people", IconBgCssClass = "demand" });
            }

            return new MarketConditionsData { Items = items };
        }
        #endregion CommodityActivities

        #region UpcomingOffers
        public async Task<UpcomingOffersData> GetOfferHistoryAsync(int subGroupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // --- 1. واکشی عرضه‌های آینده ---
            var futureOffersQuery = context.Offers
                .Where(o => string.Compare(o.OfferDate, todayPersian) > 0)
                .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => new { Offer = o, Commodity = c })
                .Where(x => x.Commodity.ParentId == subGroupId)
                .Select(x => x.Offer)
                .OrderBy(o => o.OfferDate);

            var futureItems = await ProcessOffersQuery(futureOffersQuery, OfferDateType.Future, context);

            // --- 2. واکشی عرضه‌های امروز ---
            var todayOffersQuery = context.Offers
                .Where(o => o.OfferDate == todayPersian)
                .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => new { Offer = o, Commodity = c })
                .Where(x => x.Commodity.ParentId == subGroupId)
                .Select(x => x.Offer);

            var todayItems = await ProcessOffersQuery(todayOffersQuery, OfferDateType.Today, context);

            // --- 3. واکشی عرضه‌های گذشته ---
            var pastOffersQuery = context.Offers
                .Where(o => string.Compare(o.OfferDate, todayPersian) < 0)
                .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => new { Offer = o, Commodity = c })
                .Where(x => x.Commodity.ParentId == subGroupId)
                .Select(x => x.Offer)
                .OrderByDescending(o => o.OfferDate)
                .Take(15);

            var pastItems = await ProcessOffersQuery(pastOffersQuery, OfferDateType.Past, context);

            // --- 4. ترکیب نتایج ---
            var allItems = todayItems.Concat(futureItems).Concat(pastItems).ToList();

            return new UpcomingOffersData { Items = allItems };
        }

        private async Task<List<UpcomingOfferItem>> ProcessOffersQuery(IQueryable<Models.Spot.Offer> query, OfferDateType dateType, AppDataContext context)
        {
            var pc = new PersianCalendar();

            var offerData = await query
                .Join(context.Suppliers, o => o.SupplierId, s => s.Id, (o, s) => new { Offer = o, Supplier = s })
                .Join(context.Commodities, j => j.Offer.CommodityId, c => c.Id, (j, c) => new
                {
                    j.Offer.Id,
                    j.Offer.OfferDate,
                    CommodityName = c.PersianName,
                    SupplierName = j.Supplier.PersianName
                })
                .ToListAsync();

            return offerData.Select(data =>
            {
                var offerDate = _dateHelper.GetGregorian(data.OfferDate);
                return new UpcomingOfferItem
                {
                    Title = data.CommodityName,
                    Subtitle = data.SupplierName,
                    DayOfWeek = GetPersianDayOfWeek(offerDate.DayOfWeek),
                    DayOfMonth = pc.GetDayOfMonth(offerDate).ToString("D2"),
                    OfferDateType = dateType,
                    Type = UpcomingOfferType.Commodity,
                    UrlName = data.Id.ToString() // Using Offer ID for unique URL
                };
            }).ToList();
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
