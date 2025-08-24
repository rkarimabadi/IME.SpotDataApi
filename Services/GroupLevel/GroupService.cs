using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Services.GroupLevel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IME.SpotDataApi.Services.MainGroupLevel
{
    /// <summary>
    /// نسخه ریفکتور شده با الگوهای بهینه و پایدار برای جلوگیری از خطای ترجمه کوئری
    /// و به حداقل رساندن بار پردازشی در حافظه اپلیکیشن.
    /// </summary>
    public class GroupService : IGroupService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly IDateHelper _dateHelper;

        public GroupService(IDbContextFactory<AppDataContext> contextFactory, IDateHelper dateHelper)
        {
            _contextFactory = contextFactory;
            _dateHelper = dateHelper;
        }
        #region GroupHeader and GroupHierarchy
        /// <summary>
        /// اطلاعات هدر یک گروه کالا را واکشی می‌کند.
        /// </summary>
        public async Task<GroupHeaderData> GetGroupHeaderDataAsync(int groupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var headerInfo = await (from _group in context.Groups.Where(g => g.Id == groupId)
                                    join mainGroup in context.MainGroups on _group.ParentId equals mainGroup.Id
                                    select new
                                    {
                                        GroupName = _group.PersianName,
                                        MainGroupId = mainGroup.Id
                                    })
                                    .FirstOrDefaultAsync();

            return new GroupHeaderData
            {
                GroupName = headerInfo.GroupName,
                IconCssClass = GetIconForMainGroup(headerInfo.MainGroupId)
            };
        }

        /// <summary>
        /// ساختار سلسله مراتبی یک زیرگروه کالا را ایجاد می‌کند.
        /// </summary>
        public async Task<List<HierarchyItem>> GetGroupHierarchyAsync(int groupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var hierarchy = new List<HierarchyItem>();

            var queryResult = await (from _group in context.Groups.Where(g => g.Id == groupId)
                                     join mainGroup in context.MainGroups on _group.ParentId equals mainGroup.Id
                                     select new
                                     {
                                         Group = new { _group.Id, _group.PersianName },
                                         MainGroup = new { mainGroup.Id, mainGroup.PersianName }
                                     }).FirstOrDefaultAsync();

            if (queryResult != null)
            {
                hierarchy.Add(new HierarchyItem { Id = queryResult.MainGroup.Id, Type = "MainGroup", Name = queryResult.MainGroup.PersianName, IsActive = true });
                hierarchy.Add(new HierarchyItem { Id = queryResult.Group.Id, Type = "Group", Name = queryResult.Group.PersianName, IsActive = false });
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

        #region ActiveGroups
        public async Task<GroupListData> GetActiveSubGroupsAsync(int groupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // راه حل پایدار: واکشی یک لیست مسطح با LEFT JOIN برای تمام زیرگروه‌ها و کالاهای مرتبط
            var flatQuery = from subGroup in context.SubGroups.Where(sg => sg.ParentId == groupId)
                            join commodity in context.Commodities on subGroup.Id equals commodity.ParentId into commodityGrouping
                            from commodity in commodityGrouping.DefaultIfEmpty()
                            join offer in context.Offers on commodity.Id equals offer.CommodityId into offerGrouping
                            from offer in offerGrouping.DefaultIfEmpty()
                            select new
                            {
                                SubGroupId = subGroup.Id,
                                SubGroupName = subGroup.PersianName,
                                CommodityName = commodity != null ? commodity.PersianName : null,
                                OfferDate = offer != null ? offer.OfferDate : null
                            };

            var flatData = await flatQuery.ToListAsync();

            // سپس گروه‌بندی و محاسبات در حافظه انجام می‌شود
            var subGroupsData = flatData
                .GroupBy(x => new { x.SubGroupId, x.SubGroupName })
                .Select(g =>
                {
                    var validOffers = g.Where(x => x.OfferDate != null).ToList();
                    var validCommodities = g.Where(x => x.CommodityName != null).Select(x => x.CommodityName).Distinct().ToList();

                    return new
                    {
                        SubGroupId = g.Key.SubGroupId,
                        SubGroupName = g.Key.SubGroupName,
                        TodayOffersCount = validOffers.Count(o => o.OfferDate == todayPersian),
                        FutureOffersCount = validOffers.Count(o => string.Compare(o.OfferDate, todayPersian) > 0),
                        TopCommodities = validCommodities.Take(3)
                    };
                })
                .ToList();

            var result = new GroupListData();
            foreach (var subGroup in subGroupsData)
            {
                GroupActivityStatus status;
                int? offerCount = null;

                if (subGroup.TodayOffersCount > 0)
                {
                    status = GroupActivityStatus.ActiveToday;
                    offerCount = subGroup.TodayOffersCount;
                }
                else if (subGroup.FutureOffersCount > 0)
                {
                    status = GroupActivityStatus.ActiveFuture;
                    offerCount = subGroup.FutureOffersCount;
                }
                else
                {
                    status = GroupActivityStatus.Inactive;
                }

                result.Items.Add(new GroupListItem
                {
                    Title = subGroup.SubGroupName,
                    UrlName = subGroup.SubGroupId.ToString(),
                    Status = status,
                    OfferCount = offerCount,
                    Subtitle = string.Join("، ", subGroup.TopCommodities)
                });
            }

            return result;
        }
        #endregion ActiveGroups

        #region GroupActivities
        public async Task<MarketConditionsData> GetSubGroupActivitiesAsync(int groupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // بهینه‌سازی کامل: تمام محاسبات در یک کوئری واحد در دیتابیس انجام می‌شود.
            var stats = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian)
                .Join(context.Offers, t => t.OfferId, o => o.Id, (t, o) => new { t, o })
                .Join(context.Commodities, j => j.o.CommodityId, c => c.Id, (j, c) => new { j.t, c })
                .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.t, sg })
                .Where(x => x.sg.ParentId == groupId)
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
        }
        #endregion GroupActivities

        #region UpcomingOffers
        public async Task<UpcomingOffersData> GetUpcomingOffersAsync(int groupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // بهینه‌سازی کامل: واکشی داده‌های مورد نیاز در یک کوئری واحد.
            var futureOffersData = await context.Offers
                .Where(o => string.Compare(o.OfferDate, todayPersian) > 0)
                .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => new { o, c })
                .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.o, j.c, sg })
                .Where(x => x.sg.ParentId == groupId)
                .Join(context.Suppliers, x => x.o.SupplierId, s => s.Id, (x, s) => new
                {
                    x.o.OfferDate,
                    CommodityName = x.c.PersianName,
                    SupplierName = s.PersianName,
                    x.o.Id
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
                    Type = UpcomingOfferType.SubGroup,
                    UrlName = data.Id.ToString()

                };
            }).ToList();

            return new UpcomingOffersData { Items = items };
        }
        public async Task<UpcomingOffersData> GetTodayOffersAsync(int groupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // بهینه‌سازی کامل: واکشی داده‌های مورد نیاز در یک کوئری واحد.
            var futureOffersData = await context.Offers
                .Where(o => string.Compare(o.OfferDate, todayPersian) == 0)
                .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => new { o, c })
                .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.o, j.c, sg })
                .Where(x => x.sg.ParentId == groupId)
                .Join(context.Suppliers, x => x.o.SupplierId, s => s.Id, (x, s) => new
                {
                    x.o.OfferDate,
                    CommodityName = x.c.PersianName,
                    SupplierName = s.PersianName,
                    x.o.Id
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
                    Type = UpcomingOfferType.SubGroup,
                    UrlName = data.Id.ToString()
                };
            }).ToList();

            return new UpcomingOffersData { Items = items };
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
