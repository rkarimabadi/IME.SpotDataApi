using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Models.Spot;
using IME.SpotDataApi.Repository;
using Microsoft.EntityFrameworkCore;

namespace IME.SpotDataApi.Services.Dashboard
{
    public interface IDashboardService
    {
        Task<MarketPulseData> GetMarketPulseAsync();
        Task<MarketSentimentData> GetMarketSentimentAsync();
        Task<MarketExcitementData> GetMarketExcitementAsync();
        Task<SupplyRiskData> GetSupplyRiskAsync();
        Task<MarketMoversData> GetMarketMoversAsync();
    }

    /// <summary>
    /// سرویسی که منطق تجاری و محاسباتی مربوط به ویجت‌های داشبورد را پیاده‌سازی می‌کند.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly IDateHelper _dateHelper;

        public DashboardService(
            IDbContextFactory<AppDataContext> contextFactory,
            IDateHelper dateHelper)
        {
            _contextFactory = contextFactory;
            _dateHelper = dateHelper;
        }
        #region MarketPulse
        public async Task<MarketPulseData> GetMarketPulseAsync()
        {
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            using var context = _contextFactory.CreateDbContext();
            
            var allOffers = context.Offers.ToList();
            var todayOffers = allOffers.Where(o => o.OfferDate == todayPersian).ToList();
            
            var allTrades = context.TradeReports.ToList();
            var todayTrades = allTrades.Where(t => t.TradeDate == todayPersian).ToList();

            var realizationItem = CreateRealizationRatioItem(todayOffers, todayTrades);
            var demandStrengthItem = CreateDemandStrengthRatioItem(todayOffers, todayTrades);
            return new MarketPulseData
            {
                Items = new List<PulseCardItem> { realizationItem, demandStrengthItem }
            };
        }

        /// <summary>
        /// آیتم کارت "پتانسیل بازار" (نسبت تحقق) را ایجاد می‌کند.
        /// </summary>
        private PulseCardItem CreateRealizationRatioItem(List<Offer> todayOffers, List<TradeReport> todayTrades)
        {
            // محاسبه ارزش بالقوه بر اساس قیمت پایه و حجم عرضه
            decimal potentialValue = todayOffers.Sum(o => o.InitPrice * o.OfferVol);
            // محاسبه ارزش محقق شده بر اساس ارزش معامله گزارش شده (ضرب در ۱۰۰۰)
            decimal realizedValue = todayTrades.Sum(t => t.TradeValue); //todayTrades.Sum(t => t.TradeValue * 1000);
            decimal realizationRatio = potentialValue > 0 ? (realizedValue / potentialValue) * 100 : 0;

            return new PulseCardItem
            {
                Title = "پتانسیل بازار",
                Value = FormatLargeNumber(realizedValue),
                Change = $"{realizationRatio:F1}%",
                ChangeState = realizationRatio > 50 ? ValueState.Positive : (realizationRatio < 50 && realizationRatio > 0 ? ValueState.Negative : ValueState.Neutral)
            };
        }

        
        /// <summary>
        /// آیتم کارت "نسبت قدرت تقاضا" را ایجاد می‌کند.
        /// </summary>
        private PulseCardItem CreateDemandStrengthRatioItem(List<Offer> todayOffers, List<TradeReport> todayTrades)
        {
            // محاسبه حجم کل عرضه اولیه
            decimal totalInitialSupply = todayOffers.Sum(o => o.InitVolume);
            // محاسبه حجم کل تقاضای ثبت شده
            decimal totalRegisteredDemand = todayTrades.Sum(t => t.DemandVolume);
            decimal demandStrengthRatio = totalInitialSupply > 0 ? totalRegisteredDemand / totalInitialSupply : 0;

            return new PulseCardItem
            {
                Title = "نسبت قدرت تقاضا",
                Value = $"{totalRegisteredDemand:N0} تن",
                Change = $"x{demandStrengthRatio:F2}",
                ChangeState = demandStrengthRatio > 1 ? ValueState.Positive : (demandStrengthRatio < 1 && demandStrengthRatio > 0 ? ValueState.Negative : ValueState.Neutral)
            };
        }

        /// <summary>
        /// یک عدد بزرگ (به ریال) را به فرمت "همت" تبدیل می‌کند.
        /// </summary>
        private string FormatLargeNumber(decimal value)
        {
            if (value == 0) return "۰";
            // 1 همت = 10,000,000,000,000 ریال
            var hemtValue = (value * 10000) / 10_000_000_000_000M;
            return $"{hemtValue:F1} همت";
        }

        #endregion MarketPulse

        #region MarketSentiment
        public async Task<MarketSentimentData> GetMarketSentimentAsync()
        {            
            using var context = _contextFactory.CreateDbContext();          
         
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);
            var allOffers = context.Offers.ToList();
            var todayOffers = allOffers.Where(o => o.OfferDate == todayPersian).ToList();

            if (todayOffers.Count == 0)
            {
                return new MarketSentimentData { Items = [] };
            }

            var totalValue = todayOffers.Sum(o => o.InitPrice * o.OfferVol);
            if (totalValue == 0)
            {
                 return new MarketSentimentData { Items = [] };
            }

            var valueByCategory = todayOffers
                .GroupBy(o => GetContractCategory(o.ContractTypeId))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(o => o.InitPrice * o.OfferVol)
                );

            var items = new List<SentimentItem>();
            var categories = new List<(string Name, string Color)>
            {
                ("نقدی", "var(--primary-color)"),
                ("سلف", "var(--success-color)"),
                ("نسیه", "var(--warning-color)"),
                ("پریمیوم", "var(--info-color)")
            };

            foreach (var category in categories)
            {
                var categoryValue = valueByCategory.TryGetValue(category.Name, out decimal value) ? value : 0;
                var percentage = (int)Math.Round((categoryValue / totalValue) * 100);
                if (percentage > 0)
                {
                    items.Add(new SentimentItem
                    {
                        Name = category.Name,
                        Percentage = percentage,
                        ColorCssVariable = category.Color
                    });
                }
            }
            
            return new MarketSentimentData { Items = [.. items.OrderByDescending(i => i.Percentage)] };
        }

        private string GetContractCategory(int contractTypeId)
        {
            return contractTypeId switch
            {
                1 or 4 or 5 or 7 or 9 or 10 or 11 or 12 or 13 or 14 or 17 or 24 or 25 or 27 or 28 or 30 => "نقدی",
                2 or 8 or 16 or 18 or 19 => "سلف",
                3 or 15 or 20 or 21 or 31 or 33 or 34 => "نسیه",
                29 or 32 or 35 or 36 => "پریمیوم",
                _ => "سایر"
            };
        }



        #endregion MarketSentiment

        #region MarketExcitement
        public async Task<MarketExcitementData> GetMarketExcitementAsync()
        {
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);
            using var context = _contextFactory.CreateDbContext();
            
            var allOffers = context.Offers.ToList();
            var todayOffers = allOffers.Where(o => o.OfferDate == todayPersian).ToList();
            
            var allTrades = context.TradeReports.ToList();
            var todayTrades = allTrades.Where(t => t.TradeDate == todayPersian).ToList();

            if (!todayOffers.Any() || !todayTrades.Any())
            {
                return new MarketExcitementData { Title = "بازار آرام", Description = "هنوز معامله‌ای برای تحلیل هیجان بازار ثبت نشده است.", Percentage = 0, Label = "بدون فعالیت" };
            }

            // محاسبه هر سه شاخص هیجان
            var competitionIndex = CalculateCompetitionIndex(todayOffers, todayTrades);
            var demandCoverageRate = CalculateDemandCoverageRate(todayOffers, todayTrades);
            var completeTradeRate = CalculateCompleteTradeRate(todayOffers, todayTrades);

            // تعریف سطوح مختلف برای هر شاخص
            var excitementMetrics = new List<(decimal Value, string Label, Func<decimal, (string Title, string Description)> GetContent)>
            {
                (competitionIndex, "رقابت", GetCompetitionContent),
                (demandCoverageRate, "تقاضا", GetDemandContent),
                (completeTradeRate, "فروش", GetCompleteTradeContent)
            };

            // پیدا کردن شاخصی که بیشترین مقدار را دارد
            var topMetric = excitementMetrics.OrderByDescending(m => m.Value).First();

            // دریافت عنوان و توصیف داینامیک بر اساس مقدار شاخص برتر
            var content = topMetric.GetContent(topMetric.Value);

            return new MarketExcitementData
            {
                Title = content.Title,
                Description = content.Description,
                Percentage = (int)Math.Round(topMetric.Value),
                Label = topMetric.Label
            };
        }

        #region Excitement Content Generators

        private (string Title, string Description) GetCompetitionContent(decimal value)
        {
            if (value > 10) // آستانه هیجان بالا
                return ("رقابت بالا", "رقابت شدید بین خریداران باعث شده میانگین قیمت معاملات به شکل قابل توجهی بالاتر از قیمت پایه عرضه‌ها باشد.");
            if (value > 2) // آستانه بازار آرام
                return ("رقابت متعادل", "رقابت معقولی در معاملات امروز شکل گرفته و قیمت‌ها کمی بالاتر از نرخ پایه کشف شده‌اند.");
            return ("رقابت محدود", "رقابت چندانی در معاملات امروز مشاهده نشد و اکثر معاملات نزدیک به قیمت پایه انجام شدند.");
        }

        private (string Title, string Description) GetDemandContent(decimal value)
        {
            if (value > 150) // آستانه هیجان بالا (تقاضا > ۱.۵ برابر عرضه)
                return ("تقاضای انفجاری", "عطش خرید در بازار بسیار بالاست و حجم تقاضای ثبت‌شده به مراتب بیشتر از حجم کالاهای عرضه‌شده است.");
            if (value > 100) // آستانه بازار آرام (تقاضا > عرضه)
                return ("تقاضای قوی", "تقاضا در بازار امروز از عرضه پیشی گرفته که نشان‌دهنده چشم‌انداز مثبت خریداران است.");
            return ("تقاضای عادی", "حجم تقاضا در بازار امروز در محدوده حجم عرضه‌ها قرار دارد و توازن نسبی برقرار است.");
        }

        private (string Title, string Description) GetCompleteTradeContent(decimal value)
        {
            if (value > 90) // آستانه هیجان بالا
                return ("جذب کامل عرضه‌ها", "قدرت جذب بازار در سطح بالایی قرار دارد و تقریبا تمام کالاهای عرضه‌شده با موفقیت به فروش رسیدند.");
            if (value > 70) // آستانه بازار آرام
                return ("جذب خوب عرضه‌ها", "بخش عمده‌ای از کالاهای عرضه‌شده در بازار امروز با موفقیت معامله شدند.");
            return ("جذب گزینشی عرضه‌ها", "خریداران در معاملات امروز به صورت گزینشی عمل کرده و تنها بخشی از عرضه‌ها به فروش کامل رسیدند.");
        }

        #endregion

        #region Metric Calculation Methods

        private decimal CalculateCompetitionIndex(List<Offer> offers, List<TradeReport> trades)
        {
            var relevantOffers = offers.Where(o => trades.Any(t => t.OfferId == o.Id)).ToList();
            if (!relevantOffers.Any()) return 0;

            decimal totalWeightedBasePriceSum = relevantOffers.Sum(o => o.InitPrice * o.OfferVol);
            decimal totalOfferVolume = relevantOffers.Sum(o => o.OfferVol);
            decimal weightedAvgBasePrice = totalOfferVolume > 0 ? totalWeightedBasePriceSum / totalOfferVolume : 0;

            decimal totalWeightedFinalPriceSum = trades.Sum(t => t.FinalWeightedAveragePrice * t.TradeVolume);
            decimal totalTradeVolume = trades.Sum(t => t.TradeVolume);
            decimal weightedAvgFinalPrice = totalTradeVolume > 0 ? totalWeightedFinalPriceSum / totalTradeVolume : 0;
            
            if (weightedAvgBasePrice == 0) return 0;
            return ((weightedAvgFinalPrice - weightedAvgBasePrice) / weightedAvgBasePrice) * 100;
        }

        private decimal CalculateDemandCoverageRate(List<Offer> offers, List<TradeReport> trades)
        {
            decimal totalInitialSupply = offers.Sum(o => o.InitVolume);
            if (totalInitialSupply == 0) return 0;
            decimal totalRegisteredDemand = trades.Sum(t => t.DemandVolume);
            return (totalRegisteredDemand / totalInitialSupply) * 100;
        }

        private decimal CalculateCompleteTradeRate(List<Offer> offers, List<TradeReport> trades)
        {
            if (!offers.Any()) return 0;
            var tradesByOfferId = trades.GroupBy(t => t.OfferId).ToDictionary(g => g.Key, g => g.Sum(t => t.TradeVolume));
            int successfulOffers = 0;
            foreach (var offer in offers)
            {
                if (tradesByOfferId.TryGetValue(offer.Id, out var tradedVolume) && tradedVolume >= offer.OfferVol)
                {
                    successfulOffers++;
                }
            }
            return ((decimal)successfulOffers / offers.Count) * 100;
        }

        #endregion

        #endregion MarketExcitement

        #region SupplyRisk

        public async Task<SupplyRiskData> GetSupplyRiskAsync()
        {
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            using var context = _contextFactory.CreateDbContext();
            
            var allOffers = context.Offers.ToList();
            var todayOffers = allOffers.Where(o => o.OfferDate == todayPersian).ToList();

            if (!todayOffers.Any())
            {
                return new SupplyRiskData { Items = new List<SupplyRiskItem>() };
            }

            var commodities = context.Commodities.ToDictionary(c => c.Id);
            var subGroups = context.SubGroups.ToDictionary(s => s.Id);

            var supplierStatsBySubGroup = todayOffers
                .Where(o => commodities.ContainsKey(o.CommodityId) && commodities[o.CommodityId].ParentId.HasValue)
                .GroupBy(o => commodities[o.CommodityId].ParentId.Value)
                .Where(g => subGroups.ContainsKey(g.Key))
                .Select(g => new
                {
                    SubGroupId = g.Key,
                    SubGroupName = subGroups[g.Key].PersianName,
                    SupplierCount = g.Select(x => x.SupplierId).Distinct().Count(),
                    TotalOfferVolume = g.Sum(x => x.OfferVol)
                })
                .ToList();

            if (!supplierStatsBySubGroup.Any())
            {
                return new SupplyRiskData { Items = new List<SupplyRiskItem>() };
            }

            var items = new List<SupplyRiskItem>();

            // پیدا کردن پرریسک‌ترین: کمترین عرضه‌کننده، در صورت تساوی، بیشترین حجم عرضه
            var highestRisk = supplierStatsBySubGroup
                .OrderBy(s => s.SupplierCount)
                .ThenByDescending(s => s.TotalOfferVolume)
                .First();
                
            items.Add(new SupplyRiskItem
            {
                Title = highestRisk.SubGroupName,
                Subtitle = "ریسک بالا",
                RiskLevel = RiskLevel.High,
                Value = $"{highestRisk.SupplierCount} عرضه‌کننده"
            });

            // پیدا کردن کم‌ریسک‌ترین: بیشترین عرضه‌کننده، در صورت تساوی، بیشترین حجم عرضه
            if (supplierStatsBySubGroup.Count > 1)
            {
                var lowestRisk = supplierStatsBySubGroup
                    .OrderByDescending(s => s.SupplierCount)
                    .ThenByDescending(s => s.TotalOfferVolume)
                    .First();

                if (lowestRisk.SubGroupId != highestRisk.SubGroupId)
                {
                    items.Add(new SupplyRiskItem
                    {
                        Title = lowestRisk.SubGroupName,
                        Subtitle = "ریسک پایین",
                        RiskLevel = RiskLevel.Low,
                        Value = $"{lowestRisk.SupplierCount} عرضه‌کننده"
                    });
                }
            }

            return new SupplyRiskData { Items = items };
        }
        #endregion SupplyRisk

        #region MarketMovers
        public async Task<MarketMoversData> GetMarketMoversAsync()
        {
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            using var context = _contextFactory.CreateDbContext();
            
            var allTrades = context.TradeReports.ToList();
            var todayTrades = allTrades.Where(t => t.TradeDate == todayPersian).ToList();            

            if (!todayTrades.Any())
            {
                return new MarketMoversData { CompetitionItems = new List<MarketMoverItem>(), DemandItems = new List<MarketMoverItem>() };
            }
            
            var allOffers = context.Offers.ToDictionary(o => o.Id);
            var commodities = context.Commodities.ToDictionary(c => c.Id);
            var subGroups = context.SubGroups.ToDictionary(s => s.Id);
            var manufacturers = context.Manufacturers.ToDictionary(m => m.Id);

            var moversData = todayTrades
                .Where(t => allOffers.ContainsKey(t.OfferId)) // Ensure offer exists
                .Select(t =>
                {
                    var offer = allOffers[t.OfferId];
                    var commodity = commodities.ContainsKey(offer.CommodityId) ? commodities[offer.CommodityId] : null;
                    var subGroup = commodity != null && commodity.ParentId.HasValue && subGroups.ContainsKey(commodity.ParentId.Value) ? subGroups[commodity.ParentId.Value] : null;
                    var manufacturer = manufacturers.ContainsKey(offer.ManufacturerId) ? manufacturers[offer.ManufacturerId] : null;

                    decimal competition = offer.InitPrice > 0 ? ((t.FinalWeightedAveragePrice - offer.InitPrice) / offer.InitPrice) * 100 : 0;
                    decimal demandRatio = offer.InitVolume > 0 ? t.DemandVolume / offer.InitVolume : 0;

                    return new {
                        SubGroupName = subGroup?.PersianName ?? "نامشخص",
                        ManufacturerName = manufacturer?.PersianName ?? "نامشخص",
                        Competition = competition,
                        DemandRatio = demandRatio
                    };
                })
                .ToList();

            var competitionItems = moversData
                .OrderByDescending(d => d.Competition)
                .Take(2)
                .Select((d, index) => new MarketMoverItem
                {
                    Rank = index + 1,
                    Title = d.SubGroupName,
                    Subtitle = d.ManufacturerName,
                    Value = $"+{d.Competition:F1}%",
                    ValueState = ValueState.Positive
                })
                .ToList();

            var demandItems = moversData
                .OrderByDescending(d => d.DemandRatio)
                .Take(2)
                .Select((d, index) => new MarketMoverItem
                {
                    Rank = index + 1,
                    Title = d.SubGroupName,
                    Subtitle = d.ManufacturerName,
                    Value = $"{d.DemandRatio:F1}x",
                    ValueState = ValueState.Neutral
                })
                .ToList();

            return new MarketMoversData
            {
                CompetitionItems = competitionItems,
                DemandItems = demandItems
            };
        }
        #endregion MarketMovers
    }
}
