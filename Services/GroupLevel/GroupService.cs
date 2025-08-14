using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IME.SpotDataApi.Services.MainGroupLevel
{
    public interface IGroupService
    {
        Task<GroupListData> GetActiveSubGroupsAsync(int groupId);
        Task<MarketConditionsData> GetSubGroupActivitiesAsync(int groupId);
        Task<UpcomingOffersData> GetUpcomingOffersAsync(int groupId);
    }

    public class GroupService : IGroupService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly IDateHelper _dateHelper;

        public GroupService(IDbContextFactory<AppDataContext> contextFactory, IDateHelper dateHelper)
        {
            _contextFactory = contextFactory;
            _dateHelper = dateHelper;
        }

        #region ActiveGroups
        public async Task<GroupListData> GetActiveSubGroupsAsync(int groupId)
        {
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var allOffers = await context.Offers.ToListAsync();
            var commodities = await context.Commodities.ToDictionaryAsync(c => c.Id);

            var subGroupsInMainGroup = await context.SubGroups
                .Where(g => g.ParentId == groupId)
                .ToListAsync();

            var result = new GroupListData();

            foreach (var subGroup in subGroupsInMainGroup)
            {
                var commodityIdsInGroup = context.Commodities
                    .Where(sg => sg.ParentId == subGroup.Id)
                    .Select(sg => sg.Id)
                    .ToHashSet();

                var offersInGroup = allOffers.Where(o => commodityIdsInGroup.Contains(o.CommodityId)).ToList();

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

                var topCommodities = commodities.Values
                    .Where(sg => sg.ParentId == subGroup.Id)
                    .Take(3)
                    .Select(sg => sg.PersianName);

                result.Items.Add(new GroupListItem
                {
                    Title = subGroup.PersianName,
                    UrlName = subGroup.Id.ToString(),
                    Status = status,
                    OfferCount = offerCount,
                    Subtitle = string.Join("، ", topCommodities)
                });
            }

            return result;
        }
        #endregion ActiveGroups

        #region GroupActivities
        public async Task<MarketConditionsData> GetSubGroupActivitiesAsync(int groupId)
        {
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // پیدا کردن تمام کالاها و عرضه‌های مربوط به این گروه اصلی
            var hierarchy = GetHierarchyForGroup(context, groupId);
            var offersInSubGroup = await context.Offers
                .Where(o => o.OfferDate == todayPersian && hierarchy.CommodityIds.Contains(o.CommodityId))
                .ToListAsync();
            var offerIdsInSubGroup = offersInSubGroup.Select(o => o.Id).ToHashSet();
            var tradesInSubGroup = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian && offerIdsInSubGroup.Contains(t.OfferId))
                .ToListAsync();

            var items = new List<MarketConditionItem>();

            // 1. ارزش معاملات
            decimal totalValue = tradesInSubGroup.Sum(t => t.TradeValue * 1000);
            items.Add(new MarketConditionItem { Title = "ارزش معاملات", Value = (totalValue / 10_000_000_000_000M).ToString("F1"), Unit = "همت", IconCssClass = "bi bi-cash-stack", IconBgCssClass = "value" });

            // 2. حجم معاملات
            decimal totalVolume = tradesInSubGroup.Sum(t => t.TradeVolume);
            items.Add(new MarketConditionItem { Title = "حجم معاملات", Value = (totalVolume).ToString("F1"), Unit = "تن", IconCssClass = "bi bi-truck", IconBgCssClass = "volume" });

            // 3. شاخص رقابت
            decimal totalFinalValue = tradesInSubGroup.Sum(t => t.FinalWeightedAveragePrice * t.TradeVolume);
            decimal totalBaseValue = tradesInSubGroup.Sum(t => t.OfferBasePrice * t.TradeVolume);
            decimal competitionIndex = totalBaseValue > 0 ? ((totalFinalValue - totalBaseValue) / totalBaseValue) * 100 : 0;
            items.Add(new MarketConditionItem { Title = "شاخص رقابت", Value = $"{competitionIndex:+#.##;-#.##;0.0}%", IconCssClass = "bi bi-fire", IconBgCssClass = "competition", ValueState = competitionIndex > 0 ? ValueState.Positive : (competitionIndex < 0 ? ValueState.Negative : ValueState.Neutral) });

            // 4. قدرت تقاضا
            decimal totalDemand = tradesInSubGroup.Sum(t => t.DemandVolume);
            decimal totalSupply = tradesInSubGroup.Sum(t => t.OfferVolume);
            decimal demandRatio = totalSupply > 0 ? totalDemand / totalSupply : 0;
            items.Add(new MarketConditionItem { Title = "قدرت تقاضا", Value = $"{demandRatio:F1}x", IconCssClass = "bi bi-people", IconBgCssClass = "demand" });

            return new MarketConditionsData { Items = items };
        }
        
        private (HashSet<int> SubGroupIds, HashSet<int> CommodityIds) GetHierarchyForGroup(AppDataContext context, int groupId)
        {
            var subGroupIds = context.SubGroups.Where(g => g.ParentId == groupId).Select(g => g.Id).ToHashSet();
            var commodityIds = context.Commodities.Where(sg => sg.ParentId.HasValue && subGroupIds.Contains(sg.ParentId.Value)).Select(sg => sg.Id).ToHashSet();
            return (subGroupIds, commodityIds);
        }
        #endregion GroupActivities

        #region UpcomingOffers
        public async Task<UpcomingOffersData> GetUpcomingOffersAsync(int groupId)
        {
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var hierarchy = GetHierarchyForGroup(context, groupId);

            var futureOffers = await context.Offers
                .Where(o => string.Compare(o.OfferDate, todayPersian) > 0 && hierarchy.CommodityIds.Contains(o.CommodityId))
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
