using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
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
                items.Add(new MarketConditionItem { Title = "حجم معاملات", Value = (stats.TotalVolume).ToString("F1"), Unit = "تن", IconCssClass = "bi bi-truck", IconBgCssClass = "volume" });
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
        public async Task<UpcomingOffersData> GetUpcomingOffersAsync(int subGroupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // بازنویسی با الگوی پایدار: کوئری Join ساده برای واکشی داده‌ها.
            // دستور Take(10) در این سطح از پیچیدگی معمولاً مشکلی ایجاد نمی‌کند،
            // اما برای یکپارچگی کامل، آن را هم به حافظه منتقل می‌کنیم.
            var futureOffersData = await context.Offers
                .Join(context.Commodities,
                    offer => offer.CommodityId,
                    commodity => commodity.Id,
                    (offer, commodity) => new { offer, commodity })
                .Where(x => string.Compare(x.offer.OfferDate, todayPersian) > 0 && x.commodity.ParentId == subGroupId)
                .Join(context.Suppliers,
                    x => x.offer.SupplierId,
                    supplier => supplier.Id,
                    (x, supplier) => new
                    {
                        x.offer.OfferDate,
                        CommodityName = x.commodity.PersianName,
                        SupplierName = supplier.PersianName,
                        x.offer.Id
                    })
                .ToListAsync();

            // مرتب‌سازی و انتخاب ۱۰ عرضه اول در حافظه انجام می‌شود.
            var items = futureOffersData
                .OrderBy(data => data.OfferDate)
                .Take(10)
                .Select(data =>
                {
                    var offerDate = _dateHelper.GetGregorian(data.OfferDate);
                    var pc = new PersianCalendar();

                    return new UpcomingOfferItem
                    {
                        DayOfWeek = GetPersianDayOfWeek(offerDate.DayOfWeek),
                        DayOfMonth = pc.GetDayOfMonth(offerDate).ToString(),
                        Title = data.CommodityName ?? "کالای نامشخص",
                        Subtitle = $"توسط {data.SupplierName ?? "عرضه‌کننده نامشخص"}",
                        Type = UpcomingOfferType.Commodity,
                        UrlName = data.Id.ToString()
                    };
                }).ToList();

            return new UpcomingOffersData { Items = items };
        }
        public async Task<UpcomingOffersData> GetTodayOffersAsync(int subGroupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // بازنویسی با الگوی پایدار: کوئری Join ساده برای واکشی داده‌ها.
            // دستور Take(10) در این سطح از پیچیدگی معمولاً مشکلی ایجاد نمی‌کند،
            // اما برای یکپارچگی کامل، آن را هم به حافظه منتقل می‌کنیم.
            var futureOffersData = await context.Offers
                .Join(context.Commodities,
                    offer => offer.CommodityId,
                    commodity => commodity.Id,
                    (offer, commodity) => new { offer, commodity })
                .Where(x => string.Compare(x.offer.OfferDate, todayPersian) == 0 && x.commodity.ParentId == subGroupId)
                .Join(context.Suppliers,
                    x => x.offer.SupplierId,
                    supplier => supplier.Id,
                    (x, supplier) => new
                    {
                        x.offer.OfferDate,
                        CommodityName = x.commodity.PersianName,
                        SupplierName = supplier.PersianName,
                        x.offer.Id
                    })
                .ToListAsync();

            // مرتب‌سازی و انتخاب ۱۰ عرضه اول در حافظه انجام می‌شود.
            var items = futureOffersData
                .OrderBy(data => data.OfferDate)
                .Take(10)
                .Select(data =>
                {
                    var offerDate = _dateHelper.GetGregorian(data.OfferDate);
                    var pc = new PersianCalendar();

                    return new UpcomingOfferItem
                    {
                        DayOfWeek = GetPersianDayOfWeek(offerDate.DayOfWeek),
                        DayOfMonth = pc.GetDayOfMonth(offerDate).ToString(),
                        Title = data.CommodityName ?? "کالای نامشخص",
                        Subtitle = $"توسط {data.SupplierName ?? "عرضه‌کننده نامشخص"}",
                        Type = UpcomingOfferType.Commodity,
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
