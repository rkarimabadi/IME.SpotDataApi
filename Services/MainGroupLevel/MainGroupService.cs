using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IME.SpotDataApi.Services.MainGroupLevel
{
    public interface IMainGroupService
    {
        Task<GroupListData> GetActiveGroupsAsync(int mainGroupId);
        Task<MarketConditionsData> GetGroupActivitiesAsync(int mainGroupId);
        Task<UpcomingOffersData> GetUpcomingOffersAsync(int mainGroupId);
    }

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
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var allOffers = await context.Offers.ToListAsync();
            var commodities = await context.Commodities.ToDictionaryAsync(c => c.Id);
            var subGroups = await context.SubGroups.ToDictionaryAsync(s => s.Id);

            var groupsInMainGroup = await context.Groups
                .Where(g => g.ParentId == mainGroupId)
                .ToListAsync();

            var result = new GroupListData();

            foreach (var group in groupsInMainGroup)
            {
                var subGroupIdsInGroup = context.SubGroups
                    .Where(sg => sg.ParentId == group.Id)
                    .Select(sg => sg.Id)
                    .ToHashSet();

                var commodityIdsInGroup = context.Commodities
                    .Where(c => c.ParentId.HasValue && subGroupIdsInGroup.Contains(c.ParentId.Value))
                    .Select(c => c.Id)
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

                var topSubGroups = subGroups.Values
                    .Where(sg => sg.ParentId == group.Id)
                    .Take(3)
                    .Select(sg => sg.PersianName);

                result.Items.Add(new GroupListItem
                {
                    Title = group.PersianName,
                    UrlName = group.Id.ToString(),
                    Status = status,
                    OfferCount = offerCount,
                    Subtitle = string.Join("، ", topSubGroups)
                });
            }

            return result;
        }
        #endregion ActiveGroups

        #region GroupActivities
        public async Task<MarketConditionsData> GetGroupActivitiesAsync(int mainGroupId)
        {
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // پیدا کردن تمام کالاها و عرضه‌های مربوط به این گروه اصلی
            var hierarchy = GetHierarchyForMainGroup(context, mainGroupId);
            var offersInGroup = await context.Offers
                .Where(o => o.OfferDate == todayPersian && hierarchy.CommodityIds.Contains(o.CommodityId))
                .ToListAsync();
            var offerIdsInGroup = offersInGroup.Select(o => o.Id).ToHashSet();
            var tradesInGroup = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian && offerIdsInGroup.Contains(t.OfferId))
                .ToListAsync();

            var items = new List<MarketConditionItem>();

            // 1. ارزش معاملات
            decimal totalValue = tradesInGroup.Sum(t => t.TradeValue * 1000);
            items.Add(new MarketConditionItem { Title = "ارزش معاملات", Value = (totalValue / 10_000_000_000_000M).ToString("F1"), Unit = "همت", IconCssClass = "bi bi-cash-stack", IconBgCssClass = "value" });

            // 2. حجم معاملات
            decimal totalVolume = tradesInGroup.Sum(t => t.TradeVolume);
            items.Add(new MarketConditionItem { Title = "حجم معاملات", Value = (totalVolume).ToString("F1"), Unit = "تن", IconCssClass = "bi bi-truck", IconBgCssClass = "volume" });

            // 3. شاخص رقابت
            decimal totalFinalValue = tradesInGroup.Sum(t => t.FinalWeightedAveragePrice * t.TradeVolume);
            decimal totalBaseValue = tradesInGroup.Sum(t => t.OfferBasePrice * t.TradeVolume);
            decimal competitionIndex = totalBaseValue > 0 ? ((totalFinalValue - totalBaseValue) / totalBaseValue) * 100 : 0;
            items.Add(new MarketConditionItem { Title = "شاخص رقابت", Value = $"{competitionIndex:+#.##;-#.##;0.0}%", IconCssClass = "bi bi-fire", IconBgCssClass = "competition", ValueState = competitionIndex > 0 ? ValueState.Positive : (competitionIndex < 0 ? ValueState.Negative : ValueState.Neutral) });

            // 4. قدرت تقاضا
            decimal totalDemand = tradesInGroup.Sum(t => t.DemandVolume);
            decimal totalSupply = tradesInGroup.Sum(t => t.OfferVolume);
            decimal demandRatio = totalSupply > 0 ? totalDemand / totalSupply : 0;
            items.Add(new MarketConditionItem { Title = "قدرت تقاضا", Value = $"{demandRatio:F1}x", IconCssClass = "bi bi-people", IconBgCssClass = "demand" });

            return new MarketConditionsData { Items = items };
        }
        
        private (HashSet<int> GroupIds, HashSet<int> SubGroupIds, HashSet<int> CommodityIds) GetHierarchyForMainGroup(AppDataContext context, int mainGroupId)
        {
            var groupIds = context.Groups.Where(g => g.ParentId == mainGroupId).Select(g => g.Id).ToHashSet();
            var subGroupIds = context.SubGroups.Where(sg => sg.ParentId.HasValue && groupIds.Contains(sg.ParentId.Value)).Select(sg => sg.Id).ToHashSet();
            var commodityIds = context.Commodities.Where(c => c.ParentId.HasValue && subGroupIds.Contains(c.ParentId.Value)).Select(c => c.Id).ToHashSet();
            return (groupIds, subGroupIds, commodityIds);
        }
        #endregion GroupActivities

        #region UpcomingOffers
                public async Task<UpcomingOffersData> GetUpcomingOffersAsync(int mainGroupId)
        {
            using var context = _contextFactory.CreateDbContext();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var hierarchy = GetHierarchyForMainGroup(context, mainGroupId);

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
