using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Models.Public;
using IME.SpotDataApi.Models.Spot;
using Microsoft.EntityFrameworkCore;


namespace IME.SpotDataApi.Services.CommodityLevel
{
    public class CommodityService : ICommodityService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly IDateHelper _dateHelper;

        public CommodityService(IDbContextFactory<AppDataContext> contextFactory, IDateHelper dateHelper)
        {
            _contextFactory = contextFactory;
            _dateHelper = dateHelper;
        }

        public async Task<CommodityHeaderData> GetCommodityHeaderDataAsync(int commodityId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var commodityInfo = await context.Commodities
                .Where(c => c.Id == commodityId)
                .Select(c => new { c.PersianName, c.Symbol })
                .FirstOrDefaultAsync();

            if (commodityInfo == null) return new CommodityHeaderData();

            var latestRing = await context.Offers
                .Where(o => o.CommodityId == commodityId)
                .OrderByDescending(o => o.OfferDate)
                .Select(o => o.OfferRing)
                .FirstOrDefaultAsync();

            return new CommodityHeaderData
            {
                CommodityName = commodityInfo.PersianName,
                Symbol = commodityInfo.Symbol,
                Ring = latestRing ?? "نامشخص"
            };
        }

        public async Task<List<HierarchyItem>> GetCommodityHierarchyAsync(int commodityId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var hierarchy = new List<HierarchyItem>();

            var queryResult = await (from commodity in context.Commodities.Where(c => c.Id == commodityId)
                                     join subGroup in context.SubGroups on commodity.ParentId equals subGroup.Id
                                     join grp in context.Groups on subGroup.ParentId equals grp.Id
                                     join mainGroup in context.MainGroups on grp.ParentId equals mainGroup.Id
                                     select new
                                     {
                                         Commodity = new { commodity.Id, commodity.PersianName },
                                         SubGroup = new { subGroup.Id, subGroup.PersianName },
                                         Group = new { grp.Id, grp.PersianName },
                                         MainGroup = new { mainGroup.Id, mainGroup.PersianName }
                                     }).FirstOrDefaultAsync();

            if (queryResult != null)
            {
                hierarchy.Add(new HierarchyItem { Id = queryResult.MainGroup.Id, Name = queryResult.MainGroup.PersianName });
                hierarchy.Add(new HierarchyItem { Id = queryResult.Group.Id, Name = queryResult.Group.PersianName });
                hierarchy.Add(new HierarchyItem { Id = queryResult.SubGroup.Id, Name = queryResult.SubGroup.PersianName });
                hierarchy.Add(new HierarchyItem { Id = queryResult.Commodity.Id, Name = queryResult.Commodity.PersianName, IsActive = true });
            }

            return hierarchy;
        }

        public async Task<PriceViewModel> GetPriceTrendsAsync(int commodityId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var recentTrades = await (from t in context.TradeReports
                                      where t.CommodityId == commodityId && t.FinalWeightedAveragePrice > 0
                                      join o in context.Offers on t.OfferId equals o.Id
                                      join cu in context.CurrencyUnits on o.CurrencyId equals cu.Id
                                      orderby t.TradeDate descending
                                      select new
                                      {
                                          t.TradeDate,
                                          t.FinalWeightedAveragePrice,
                                          t.OfferBasePrice,
                                          t.MaximumPrice,
                                          CurrencyUnit = cu.PersianName
                                      })
                                      .Take(15)
                                      .ToListAsync();

            if (!recentTrades.Any()) return new PriceViewModel();

            var latestTrade = recentTrades.First();
            var previousTrade = recentTrades.Skip(1).FirstOrDefault();

            var changeAmount = (previousTrade != null) ? latestTrade.FinalWeightedAveragePrice - previousTrade.FinalWeightedAveragePrice : 0;
            var changePercentage = (previousTrade != null && previousTrade.FinalWeightedAveragePrice > 0) ? (double)(changeAmount / previousTrade.FinalWeightedAveragePrice) * 100 : 0.0;
            
            var lastTradeDate = _dateHelper.GetGregorian(latestTrade.TradeDate);
            var daysSinceLastTrade = (DateTime.Now - lastTradeDate).Days;

            var competitionRatio = latestTrade.OfferBasePrice > 0 ? latestTrade.FinalWeightedAveragePrice / latestTrade.OfferBasePrice : 0;
            var avg3Trades = recentTrades.Take(3).Average(t => (decimal?)t.FinalWeightedAveragePrice) ?? 0;
            var currencyUnit = latestTrade.CurrencyUnit ?? "ریال";

            return new PriceViewModel
            {
                CurrentPrice = latestTrade.FinalWeightedAveragePrice,
                ChangeAmount = changeAmount,
                ChangePercentage = changePercentage,
                ChangeContext = previousTrade != null ? $"نسبت به عرضه {previousTrade.TradeDate}" : "اولین معامله",
                DateLabel = $"آخرین عرضه ({daysSinceLastTrade} روز پیش)",
                IsOutdated = daysSinceLastTrade > 7,
                PriceHistory = recentTrades.OrderBy(t => t.TradeDate).Select(t => new PriceHistoryPoint
                {
                    DateLabel = t.TradeDate.Substring(5), // "MM/dd" format for Persian date
                    Price = t.FinalWeightedAveragePrice
                }).ToList(),
                Highlights = new List<HighlightViewModel>
                {
                    new() { Title = "قیمت پایه", Value = latestTrade.OfferBasePrice.ToString("N0"), Unit = currencyUnit, IconSvg = "bi bi-tag-fill" },
                    new() { Title = "رقابت", Value = competitionRatio.ToString("F2"), Unit = "برابر", IconColorClass = competitionRatio > 1.05m ? "green" : "", IconSvg = "bi bi-fire" },
                    new() { Title = "بیشینه خرید", Value = latestTrade.MaximumPrice.ToString("N0"), Unit = currencyUnit, IconColorClass = "red", IconSvg = "bi bi-arrows-expand" },
                    new() { Title = "متوسط ۳ عرضه", Value = avg3Trades.ToString("N0"), Unit = currencyUnit, IconSvg = "bi bi-graph-up" }
                }
            };
        }
        public async Task<MarketAbsorptionData> GetMarketAbsorptionAsync(int commodityId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var thirtyDaysAgo = _dateHelper.GetPersian(DateTime.Now.AddDays(-30));
            var stats = await context.TradeReports
                .Where(t => t.CommodityId == commodityId && string.Compare(t.TradeDate, thirtyDaysAgo) >= 0)
                .GroupBy(t => 1)
                .Select(g => new
                {
                    TotalTradeVolume = g.Sum(t => t.TradeVolume),
                    TotalOfferVolume = g.Sum(t => t.OfferVolume)
                })
                .FirstOrDefaultAsync();

            if (stats == null || stats.TotalOfferVolume == 0)
                return new MarketAbsorptionData { Percentage = 0, Label = "بدون عرضه", Description = "در ۳۰ روز گذشته عرضه‌ای برای این کالا ثبت نشده است." };

            var percentage = (int)(stats.TotalTradeVolume / stats.TotalOfferVolume * 100);

            string label = percentage >= 95 ? "بسیار بالا" : percentage > 50 ? "متوسط" : "پایین";
            string description = percentage >= 95 ? "تقریباً تمام حجم عرضه‌شده به فروش رسیده و رقابت شدیدی در معاملات شکل گرفته است."
                               : percentage > 50 ? "بخش قابل توجهی از حجم عرضه‌شده معامله شده است، اما بخشی از عرضه بدون مشتری باقی مانده است."
                               : "کمتر از نیمی از حجم عرضه‌شده به فروش رسیده که نشان‌دهنده تقاضای ضعیف و رکود در معاملات است.";

            return new MarketAbsorptionData { Percentage = percentage, Label = label, Description = description };
        }

        public async Task<CommodityAttributesData> GetCommodityAttributesAsync(int commodityId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var latestOffer = await context.Offers
                .Where(o => o.CommodityId == commodityId)
                .OrderByDescending(o => o.OfferDate)
                .FirstOrDefaultAsync();

            if (latestOffer == null) return new CommodityAttributesData();

                        // --- Define scopes for queries ---
            var commodityOffers = context.Offers.Where(o => o.CommodityId == commodityId);
            var allOffers = context.Offers;

            // Fetch most common values for the commodity
            //var commonCommodityBuyMethod = await GetMostCommonValueAsync(context.Offers.Where(o => o.CommodityId == commodityId), o => o.BuyMethodId, context.BuyMethods);
            //var commonCommoditySettlement = await GetMostCommonValueAsync(context.Offers.Where(o => o.CommodityId == commodityId), o => o.SettlementTypeId, context.SettlementTypes);
            //var commonCommodityDeliveryPlace = await GetMostCommonValueAsync(context.Offers.Where(o => o.CommodityId == commodityId), o => o.DeliveryPlaceId, context.DeliveryPlaces);
            var commonCommodityBuyMethod = await GetMostCommonValueAsync(commodityOffers, o => o.BuyMethodId, context.BuyMethods);
            var commonCommoditySettlement = await GetMostCommonValueAsync(commodityOffers, o => o.SettlementTypeId, context.SettlementTypes);
            var commonCommodityDeliveryPlace = await GetMostCommonValueAsync(commodityOffers, o => o.DeliveryPlaceId, context.DeliveryPlaces);
            var commonCommodityMeasurementUnit = await GetMostCommonValueAsync(commodityOffers, o => o.MeasureUnitId, context.MeasurementUnits);
            var commonCommodityPrepayment = await GetMostCommonPrimitiveValueAsync(commodityOffers, o => o.PrepaymentPercent);
            var commonCommodityTickSize = await GetMostCommonPrimitiveValueAsync(commodityOffers, o => o.TickSize);


            // Fetch most common values for the entire market
            //var commonMarketBuyMethod = await GetMostCommonValueAsync(context.Offers, o => o.BuyMethodId, context.BuyMethods);
            //var commonMarketSettlement = await GetMostCommonValueAsync(context.Offers, o => o.SettlementTypeId, context.SettlementTypes);
            //var commonMarketDeliveryPlace = await GetMostCommonValueAsync(context.Offers, o => o.DeliveryPlaceId, context.DeliveryPlaces);
            var commonMarketBuyMethod = await GetMostCommonValueAsync(allOffers, o => o.BuyMethodId, context.BuyMethods);
            var commonMarketSettlement = await GetMostCommonValueAsync(allOffers, o => o.SettlementTypeId, context.SettlementTypes);
            var commonMarketDeliveryPlace = await GetMostCommonValueAsync(allOffers, o => o.DeliveryPlaceId, context.DeliveryPlaces);
            var commonMarketMeasurementUnit = await GetMostCommonValueAsync(allOffers, o => o.MeasureUnitId, context.MeasurementUnits);
            var commonMarketPrepayment = await GetMostCommonPrimitiveValueAsync(allOffers, o => o.PrepaymentPercent);
            var commonMarketTickSize = await GetMostCommonPrimitiveValueAsync(allOffers, o => o.TickSize);

            // Get current values from the latest offer
            var currentBuyMethod = (await context.BuyMethods.FindAsync(latestOffer.BuyMethodId))?.PersianName ?? "نامشخص";
            var currentSettlementType = (await context.SettlementTypes.FindAsync(latestOffer.SettlementTypeId))?.PersianName ?? "نامشخص";
            var currentDeliveryPlace = (await context.DeliveryPlaces.FindAsync(latestOffer.DeliveryPlaceId))?.PersianName ?? "نامشخص";
            var currentMeasurementUnit = (await context.MeasurementUnits.FindAsync(latestOffer.MeasureUnitId))?.PersianName ?? "نامشخص";
            
            var items = new List<CommodityAttributeItem>();

            // 1. Buy Method
            bool isBuyMethodAlert = currentBuyMethod != commonCommodityBuyMethod.Name;
            items.Add(new CommodityAttributeItem
            {
                Title = "روش خرید",
                CurrentValue = currentBuyMethod,
                IconCssClass = "bi bi-cart",
                IconBgCssClass = "icon-bg-orange",
                Interpretation = currentBuyMethod == "حراج باز" ? "استفاده از حراج باز نشان‌دهنده تقاضای بسیار بالا و رقابت شدید برای این کالا است." : "این روش خرید استاندارد برای معاملات این کالا است.",
                IsAlert = isBuyMethodAlert,
                CommodityMostCommonValue = commonCommodityBuyMethod.Name,
                MarketMostCommonValue = commonMarketBuyMethod.Name
            });

            // 2. Settlement Type
            bool isSettlementAlert = currentSettlementType != commonMarketSettlement.Name;
            items.Add(new CommodityAttributeItem
            {
                Title = "نوع تسویه",
                CurrentValue = currentSettlementType,
                IconCssClass = "bi bi-credit-card-fill",
                IconBgCssClass = "icon-bg-blue",
                Interpretation = currentSettlementType == "نقدی" ? "تسویه نقدی نشان‌دهنده نقدشوندگی بالا و سلامت مالی خریداران در این بازار است." : "تسویه اعتباری می‌تواند نشان‌دهنده شرایط خاص فروشنده یا نیاز به نقدینگی در سمت خریدار باشد.",
                IsAlert = isSettlementAlert,
                CommodityMostCommonValue = commonCommoditySettlement.Name,
                MarketMostCommonValue = commonMarketSettlement.Name
            });
            // 3. Delivery Place
            bool isDeliveryPlaceAlert = currentDeliveryPlace != commonMarketDeliveryPlace.Name;
            items.Add(new CommodityAttributeItem
            {
                Title = "محل تحویل",
                CurrentValue = currentDeliveryPlace,
                IconCssClass = "bi bi-geo-alt-fill",
                IconBgCssClass = "icon-bg-red",
                Interpretation = "تحویل در این محل، هزینه‌های لجستیک را به خریدار منتقل می‌کند. تفاوت آن با رویه بازار می‌تواند بر قیمت تمام شده تاثیرگذار باشد.",
                IsAlert = isDeliveryPlaceAlert,
                CommodityMostCommonValue = commonCommodityDeliveryPlace.Name,
                MarketMostCommonValue = commonMarketDeliveryPlace.Name
            });
            // 4. Prepayment Percent
            items.Add(new CommodityAttributeItem {
                Title = "درصد پیش‌پرداخت", CurrentValue = $"{latestOffer.PrepaymentPercent}%", IconCssClass = "bi bi-percent", IconBgCssClass = "icon-bg-green",
                Interpretation = "این درصد استاندارد بازار است و نشان‌دهنده نیاز به نقدینگی عادی برای خریداران می‌باشد.",
                IsAlert = latestOffer.PrepaymentPercent != commonMarketPrepayment,
                CommodityMostCommonValue = $"{commonCommodityPrepayment}%", MarketMostCommonValue = $"{commonMarketPrepayment}%"
            });

            // 5. Measurement Unit
            items.Add(new CommodityAttributeItem {
                Title = "واحد اندازه‌گیری", CurrentValue = currentMeasurementUnit, IconCssClass = "bi bi-rulers", IconBgCssClass = "icon-bg-blue",
                Interpretation = "واحد اندازه‌گیری استاندارد برای این کالا که در محاسبات قیمت و حجم استفاده می‌شود.",
                IsAlert = currentMeasurementUnit != commonMarketMeasurementUnit.Name,
                CommodityMostCommonValue = commonCommodityMeasurementUnit.Name, MarketMostCommonValue = commonMarketMeasurementUnit.Name
            });

            // 6. Tick Size
            items.Add(new CommodityAttributeItem {
                Title = "حداقل تغییر قیمت", CurrentValue = latestOffer.TickSize.ToString("N0"), IconCssClass = "bi bi-graph-up", IconBgCssClass = "icon-bg-purple",
                Interpretation = "هر سفارش خرید باید مضربی از این عدد بالاتر از قیمت پایه باشد. این مقدار بر پویایی رقابت در حراج تاثیرگذار است.",
                IsAlert = latestOffer.TickSize != commonMarketTickSize,
                CommodityMostCommonValue = commonCommodityTickSize.ToString("N0"), MarketMostCommonValue = commonMarketTickSize.ToString("N0")
            });

            return new CommodityAttributesData { Items = items };
        }

            // Helper method to find the most common value in a given query
        private async Task<(int Id, string Name)> GetMostCommonValueAsync<TEntity>(
            IQueryable<Offer> query,
            System.Linq.Expressions.Expression<Func<Offer, int>> keySelector,
            DbSet<TEntity> dbSet) where TEntity : BaseInfo
        {
            var common = await query
                .GroupBy(keySelector)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            if (common == null) return (0, "نامشخص");

            var entity = await dbSet.FindAsync(common.Id);
            return (common.Id, entity?.PersianName ?? "نامشخص");
        }
        private async Task<T> GetMostCommonPrimitiveValueAsync<T>(
            IQueryable<Offer> query,
            System.Linq.Expressions.Expression<Func<Offer, T>> keySelector) where T : struct
        {
            var common = await query
                .GroupBy(keySelector)
                .Select(g => new { Value = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            return common?.Value ?? default(T);
        }
        public async Task<IEnumerable<MainPlayer>> GetMainPlayersAsync(int commodityId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var thirtyDaysAgo = _dateHelper.GetPersian(DateTime.Now.AddDays(-30));

            var tradesData = await (from t in context.TradeReports.Where(t => t.CommodityId == commodityId && string.Compare(t.TradeDate, thirtyDaysAgo) >= 0)
                                    join s in context.Suppliers on t.SupplierId equals s.Id
                                    join b in context.Brokers on t.SellerBrokerId equals b.Id
                                    select new { t.TradeValue, SupplierName = s.PersianName, BrokerName = b.PersianName })
                                    .ToListAsync();

            if (!tradesData.Any()) return Enumerable.Empty<MainPlayer>();

            var totalValue = tradesData.Sum(t => t.TradeValue);
            if (totalValue == 0) return Enumerable.Empty<MainPlayer>();

            var topSupplier = tradesData
                .GroupBy(t => t.SupplierName)
                .Select(g => new { Name = g.Key, Value = g.Sum(t => t.TradeValue) })
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();

            var topBroker = tradesData
                .GroupBy(t => t.BrokerName)
                .Select(g => new { Name = g.Key, Value = g.Sum(t => t.TradeValue) })
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();

            var players = new List<MainPlayer>();
            if (topSupplier != null)
            {
                players.Add(new MainPlayer
                {
                    Type = "برترین عرضه‌کننده",
                    Name = topSupplier.Name,
                    IconCssClass = "bi bi-buildings-fill",
                    MarketShare = topSupplier.Value / totalValue * 100
                });
            }
            if (topBroker != null)
            {
                players.Add(new MainPlayer
                {
                    Type = "برترین کارگزار",
                    Name = topBroker.Name,
                    IconCssClass = "bi bi-person-workspace",
                    MarketShare = topBroker.Value / totalValue * 100
                });
            }

            return players;
        }

        public async Task<DistributedAttributesData> GetDistributedAttributesAsync(int commodityId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var thirtyDaysAgo = _dateHelper.GetPersian(DateTime.Now.AddDays(-30));
            var items = new List<DistributedAttributeItem>();

            // --- 1. Contract Type ---
            var contractTrades = await (from t in context.TradeReports.Where(t => t.CommodityId == commodityId && string.Compare(t.TradeDate, thirtyDaysAgo) >= 0)
                                        join ct in context.ContractTypes on t.ContractTypeId equals ct.Id
                                        select new { t.TradeVolume, ContractTypeName = ct.PersianName })
                                        .ToListAsync();
            if (contractTrades.Any())
            {
                var totalVolume = contractTrades.Sum(t => t.TradeVolume);
                var distribution = contractTrades
                    .GroupBy(t => t.ContractTypeName)
                    .Select(g => new AttributeValueShare { Name = g.Key, Percentage = totalVolume > 0 ? (double)(g.Sum(t => t.TradeVolume) / totalVolume) * 100 : 0, ColorCssClass = "color-blue" })
                    .OrderByDescending(s => s.Percentage)
                    .ToList();
                
                var dominantValue = distribution.FirstOrDefault()?.Name ?? "نامشخص";
                var marketMostCommon = await GetMostCommonValueAsync(context.Offers, o => o.ContractTypeId, context.ContractTypes);

                items.Add(new DistributedAttributeItem {
                    Title = "نوع قرارداد", ValueDistribution = distribution, IconCssClass = "bi bi-file-text-fill", IconBgCssClass = "icon-bg-blue",
                    Interpretation = $"تسلط {dominantValue} نشان‌دهنده گرایش اصلی بازار این کالا است.",
                    IsAlert = dominantValue != marketMostCommon.Name,
                    DominantValue = dominantValue, MarketMostCommonValue = marketMostCommon.Name
                });
            }

            // --- 2. Settlement Type ---
            var settlementOffers = await (from o in context.Offers.Where(o => o.CommodityId == commodityId && string.Compare(o.OfferDate, thirtyDaysAgo) >= 0)
                                          join st in context.SettlementTypes on o.SettlementTypeId equals st.Id
                                          select new { SettlementTypeName = st.PersianName })
                                          .ToListAsync();
            if (settlementOffers.Any())
            {
                var totalOffers = settlementOffers.Count;
                var distribution = settlementOffers
                    .GroupBy(o => o.SettlementTypeName)
                    .Select(g => new AttributeValueShare { Name = g.Key, Percentage = ((double)g.Count() / totalOffers) * 100, ColorCssClass = "color-orange" })
                    .OrderByDescending(s => s.Percentage)
                    .ToList();
                
                var dominantValue = distribution.FirstOrDefault()?.Name ?? "نامشخص";
                var marketMostCommon = await GetMostCommonValueAsync(context.Offers, o => o.SettlementTypeId, context.SettlementTypes);

                items.Add(new DistributedAttributeItem {
                    Title = "نوع تسویه", ValueDistribution = distribution, IconCssClass = "bi bi-credit-card-fill", IconBgCssClass = "icon-bg-orange",
                    Interpretation = $"غالب بودن تسویه {dominantValue} می‌تواند بر نیاز به نقدینگی خریداران تاثیرگذار باشد.",
                    IsAlert = dominantValue != marketMostCommon.Name,
                    DominantValue = dominantValue, MarketMostCommonValue = marketMostCommon.Name
                });
            }

            // --- 3. Packaging Type ---
            var packagingOffers = await (from o in context.Offers.Where(o => o.CommodityId == commodityId && string.Compare(o.OfferDate, thirtyDaysAgo) >= 0)
                                         join pt in context.PackagingTypes on o.PackagingTypeId equals pt.Id
                                         where pt.Id != 0 // Assuming 0 is for not specified
                                         select new { PackagingTypeName = pt.PersianName })
                                         .ToListAsync();
            if (packagingOffers.Any())
            {
                var totalOffers = packagingOffers.Count;
                var distribution = packagingOffers
                    .GroupBy(o => o.PackagingTypeName)
                    .Select(g => new AttributeValueShare { Name = g.Key, Percentage = ((double)g.Count() / totalOffers) * 100, ColorCssClass = "color-purple" })
                    .OrderByDescending(s => s.Percentage)
                    .ToList();

                var dominantValue = distribution.FirstOrDefault()?.Name ?? "نامشخص";
                var marketMostCommon = await GetMostCommonValueAsync(context.Offers.Where(o => o.PackagingTypeId != 0), o => o.PackagingTypeId, context.PackagingTypes);
                
                items.Add(new DistributedAttributeItem {
                    Title = "نوع بسته‌بندی", ValueDistribution = distribution, IconCssClass = "bi bi-box-seam", IconBgCssClass = "icon-bg-purple",
                    Interpretation = $"غالب بودن بسته‌بندی {dominantValue} نشان می‌دهد که خریداران اصلی چه گروهی هستند (صنعتی یا خرد).",
                    IsAlert = dominantValue != marketMostCommon.Name,
                    DominantValue = dominantValue, MarketMostCommonValue = marketMostCommon.Name
                });
            }

            return new DistributedAttributesData { Items = items };
        }   
    }
}
