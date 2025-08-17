using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IME.SpotDataApi.Services.MainGroupLevel
{
    public interface ISubGroupService
    {
        Task<GroupListData> GetActiveCommoditiesAsync(int subGroupId);
        Task<MarketConditionsData> GetCommodityActivitiesAsync(int subGroupId);
        Task<UpcomingOffersData> GetUpcomingOffersAsync(int subGgroupId);
    }

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
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var allOffers = await context.Offers.ToListAsync();
            var suppliers = await context.Suppliers.ToDictionaryAsync(c => c.Id);

            var commoditiesInSubGroup = await context.Commodities
                .Where(g => g.ParentId == subGroupId)
                .ToListAsync();

            var result = new GroupListData();

            foreach (var commodity in commoditiesInSubGroup)
            {

                var offersInGroup = allOffers.Where(o => o.CommodityId == commodity.Id).ToList();

                var todayOffers = offersInGroup.Where(o => o.OfferDate == todayPersian).ToList();
                var futureOffers = offersInGroup.Where(o => string.Compare(o.OfferDate, todayPersian) > 0).ToList();

                GroupActivityStatus status;
                int? offerCount = null;

                if (todayOffers.Any())
                {
                    status = GroupActivityStatus.ActiveToday;
                    offerCount = todayOffers.Count;
                }
                else if (futureOffers.Any())
                {
                    status = GroupActivityStatus.ActiveFuture;
                    offerCount = futureOffers.Count;
                }
                else
                {
                    status = GroupActivityStatus.Inactive;
                }

                var topSuppliers = suppliers.Values
                    .Where(sg => allOffers.Any(o => o.CommodityId == commodity.Id && o.SupplierId == sg.Id))
                    .Take(3)
                    .Select(sg => sg.PersianName);

                result.Items.Add(new GroupListItem
                {
                    Title = commodity.PersianName,
                    UrlName = commodity.Id.ToString(),
                    Status = status,
                    OfferCount = offerCount,
                    Subtitle = string.Join("، ", topSuppliers)
                });
            }

            return result;
        }
        #endregion ActiveCommodities

        #region CommodityActivities
        public async Task<MarketConditionsData> GetCommodityActivitiesAsync(int subGroupId)
        {
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // پیدا کردن تمام کالاها و عرضه‌های مربوط به این گروه اصلی
            var hierarchy = GetHierarchyForSubGroup(context, subGroupId);
            var offersInCommodity = await context.Offers
                .Where(o => o.OfferDate == todayPersian && hierarchy.Contains(o.CommodityId))
                .ToListAsync();
            var offerIdsInCommodity = offersInCommodity.Select(o => o.Id).ToHashSet();
            var tradesInCommodity = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian && offerIdsInCommodity.Contains(t.OfferId))
                .ToListAsync();

            var items = new List<MarketConditionItem>();

            // 1. ارزش معاملات
            decimal totalValue = tradesInCommodity.Sum(t => t.TradeValue * 1000);
            items.Add(new MarketConditionItem { Title = "ارزش معاملات", Value = (totalValue / 10_000_000_000_000M).ToString("F1"), Unit = "همت", IconCssClass = "bi bi-cash-stack", IconBgCssClass = "value" });

            // 2. حجم معاملات
            decimal totalVolume = tradesInCommodity.Sum(t => t.TradeVolume);
            items.Add(new MarketConditionItem { Title = "حجم معاملات", Value = (totalVolume).ToString("F1"), Unit = "تن", IconCssClass = "bi bi-truck", IconBgCssClass = "volume" });

            // 3. شاخص رقابت
            decimal totalFinalValue = tradesInCommodity.Sum(t => t.FinalWeightedAveragePrice * t.TradeVolume);
            decimal totalBaseValue = tradesInCommodity.Sum(t => t.OfferBasePrice * t.TradeVolume);
            decimal competitionIndex = totalBaseValue > 0 ? ((totalFinalValue - totalBaseValue) / totalBaseValue) * 100 : 0;
            items.Add(new MarketConditionItem { Title = "شاخص رقابت", Value = $"{competitionIndex:+#.##;-#.##;0.0}%", IconCssClass = "bi bi-fire", IconBgCssClass = "competition", ValueState = competitionIndex > 0 ? ValueState.Positive : (competitionIndex < 0 ? ValueState.Negative : ValueState.Neutral) });

            // 4. قدرت تقاضا
            decimal totalDemand = tradesInCommodity.Sum(t => t.DemandVolume);
            decimal totalSupply = tradesInCommodity.Sum(t => t.OfferVolume);
            decimal demandRatio = totalSupply > 0 ? totalDemand / totalSupply : 0;
            items.Add(new MarketConditionItem { Title = "قدرت تقاضا", Value = $"{demandRatio:F1}x", IconCssClass = "bi bi-people", IconBgCssClass = "demand" });

            return new MarketConditionsData { Items = items };
        }
        
        private HashSet<int> GetHierarchyForSubGroup(AppDataContext context, int subGroupId)
        {
            var commodityIds = context.Commodities.Where(g => g.ParentId == subGroupId).Select(g => g.Id).ToHashSet();
            return commodityIds;
        }
        #endregion CommodityActivities

        #region UpcomingOffers
        public async Task<UpcomingOffersData> GetUpcomingOffersAsync(int groupId)
        {
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var hierarchy = GetHierarchyForSubGroup(context, groupId);

            var futureOffers = await context.Offers
                .Where(o => string.Compare(o.OfferDate, todayPersian) > 0 && hierarchy.Contains(o.CommodityId))
                .OrderBy(o => o.OfferDate)
                .Take(10) // Limit to a reasonable number of upcoming offers
                .ToListAsync();

            if (!futureOffers.Any())
            {
                return new UpcomingOffersData();
            }

            var commodities = await context.Commodities.ToDictionaryAsync(c => c.Id);
            var suppliers = await context.Suppliers.ToDictionaryAsync(s => s.Id);

            var items = futureOffers.Select(offer =>
            {
                var offerDate = _dateHelper.GetGregorian(offer.OfferDate);
                var pc = new PersianCalendar();

                return new UpcomingOfferItem
                {
                    DayOfWeek = GetPersianDayOfWeek(offerDate.DayOfWeek),
                    DayOfMonth = pc.GetDayOfMonth(offerDate).ToString(),
                    Title = commodities.GetValueOrDefault(offer.CommodityId)?.PersianName ?? "کالای نامشخص",
                    Subtitle = $"توسط {suppliers.GetValueOrDefault(offer.SupplierId)?.PersianName ?? "عرضه‌کننده نامشخص"}"
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
