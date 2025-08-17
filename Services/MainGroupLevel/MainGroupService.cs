using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
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
    }

    /// <summary>
    /// نسخه ریفکتور شده با الگوهای بهینه برای اجرای تمام محاسبات در دیتابیس
    /// و جلوگیری از بارگذاری جداول بزرگ در حافظه.
    /// </summary>
    public class MainGroupService : IMainGroupService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly IDateHelper _dateHelper;

        public MainGroupService(IDbContextFactory<AppDataContext> contextFactory, IDateHelper dateHelper)
        {
            _contextFactory = contextFactory;
            _dateHelper = dateHelper;
        }

        #region ActiveGroups
        public async Task<GroupListData> GetActiveGroupsAsync(int mainGroupId)
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
        }
        #endregion ActiveGroups

        #region GroupActivities
        public async Task<MarketConditionsData> GetGroupActivitiesAsync(int mainGroupId)
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
                items.Add(new MarketConditionItem { Title = "حجم معاملات", Value = (stats.TotalVolume).ToString("F1"), Unit = "تن", IconCssClass = "bi bi-truck", IconBgCssClass = "volume" });
                decimal competitionIndex = stats.TotalBaseValue > 0 ? ((stats.TotalFinalValue - stats.TotalBaseValue) / stats.TotalBaseValue) * 100 : 0;
                items.Add(new MarketConditionItem { Title = "شاخص رقابت", Value = $"{competitionIndex:+#.##;-#.##;0.0}%", IconCssClass = "bi bi-fire", IconBgCssClass = "competition", ValueState = competitionIndex > 0 ? ValueState.Positive : (competitionIndex < 0 ? ValueState.Negative : ValueState.Neutral) });
                decimal demandRatio = stats.TotalSupply > 0 ? stats.TotalDemand / stats.TotalSupply : 0;
                items.Add(new MarketConditionItem { Title = "قدرت تقاضا", Value = $"{demandRatio:F1}x", IconCssClass = "bi bi-people", IconBgCssClass = "demand" });
            }

            return new MarketConditionsData { Items = items };
        }
        #endregion GroupActivities

        #region UpcomingOffers
        public async Task<UpcomingOffersData> GetUpcomingOffersAsync(int mainGroupId)
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
