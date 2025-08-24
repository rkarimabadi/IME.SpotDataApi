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
        Task<List<MainPlayer>> GetMainPlayersAsync();
        Task<TradingHallsData> GetTradingHallsAsync();
        Task<MarketProgressData> GetMarketProgressAsync();
        Task<SpotNotificationData> GetSpotNotificationsAsync();
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
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // کوئری اول: محاسبه آمار بر اساس تمام عرضه‌های امروز
            var offerStats = await context.Offers
                .Where(o => o.OfferDate == todayPersian)
                .GroupBy(o => 1)
                .Select(g => new
                {
                    PotentialValue = g.Sum(o => o.InitPrice * (o.OfferVol * 1000)),
                    TotalInitialSupply = g.Sum(o => o.OfferVol)
                })
                .FirstOrDefaultAsync();

            // کوئری دوم: محاسبه آمار بر اساس تمام معاملات امروز
            var tradeStats = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian)
                .GroupBy(t => 1)
                .Select(g => new
                {
                    RealizedValue = g.Sum(t => t.TradeValue * 1000),
                    TotalRegisteredDemand = g.Sum(t => t.DemandVolume)
                })
                .FirstOrDefaultAsync();

            if (offerStats == null || tradeStats == null)
            {
                var emptyPulse = new PulseCardItem { ChangeState = ValueState.Neutral };
                return new MarketPulseData { Items = new List<PulseCardItem> { emptyPulse, emptyPulse } };
            }

            var realizationRatio = offerStats.PotentialValue > 0 ? (tradeStats.RealizedValue / offerStats.PotentialValue) * 100 : 0;
            var demandStrengthRatio = offerStats.TotalInitialSupply > 0 ? tradeStats.TotalRegisteredDemand / offerStats.TotalInitialSupply : 0;

            var realizationItem = new PulseCardItem
            {
                Title = "پتانسیل بازار",
                Value = FormatLargeNumber(tradeStats.RealizedValue),
                Change = $"{realizationRatio:F1}%",
                ChangeLabel = "نرخ تحقق",
                ChangeState = realizationRatio > 50 ? ValueState.Positive : (realizationRatio < 50 && realizationRatio > 0 ? ValueState.Negative : ValueState.Neutral)
            };

            var demandStrengthItem = new PulseCardItem
            {
                Title = "نسبت قدرت تقاضا",
                Value = $"{tradeStats.TotalRegisteredDemand:N0} تن",
                Change = $"x{demandStrengthRatio:F2}",
                ChangeLabel = "ضریب تقاضا",
                ChangeState = demandStrengthRatio > 1 ? ValueState.Positive : (demandStrengthRatio < 1 && demandStrengthRatio > 0 ? ValueState.Negative : ValueState.Neutral)
            };

            return new MarketPulseData { Items = new List<PulseCardItem> { realizationItem, demandStrengthItem } };
        }

        private string FormatLargeNumber(decimal valueInRials)
        {
            if (valueInRials == 0) return "۰";
            var hemtValue = valueInRials / 10_000_000_000_000M;
            return $"{hemtValue:F1} همت";
        }
        #endregion MarketPulse

        #region MarketSentiment
        public async Task<MarketSentimentData> GetMarketSentimentAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // رفع خطا: ابتدا داده‌های خام را از دیتابیس واکشی می‌کنیم
            var todayOffers = await context.Offers
                .Where(o => o.OfferDate == todayPersian)
                .Select(o => new { o.ContractTypeId, o.InitPrice, o.OfferVol })
                .ToListAsync();

            if (!todayOffers.Any()) return new MarketSentimentData { Items = new List<SentimentItem>() };
            
            // سپس گروه‌بندی و محاسبات را در حافظه انجام می‌دهیم
            var valueByCategory = todayOffers
                .GroupBy(o => GetContractCategory(o.ContractTypeId))
                .Select(g => new { Category = g.Key, TotalValue = g.Sum(o => o.InitPrice * o.OfferVol) })
                .ToList();

            var totalValue = valueByCategory.Sum(c => c.TotalValue);
            if (totalValue == 0) return new MarketSentimentData { Items = new List<SentimentItem>() };

            var items = valueByCategory.Select(c => new SentimentItem
            {
                Name = c.Category,
                Percentage = (int)System.Math.Round((c.TotalValue / totalValue) * 100),
                ColorCssVariable = GetCategoryColor(c.Category)
            })
            .Where(i => i.Percentage > 0)
            .OrderByDescending(i => i.Percentage)
            .ToList();

            // Adjust percentages to sum to 100
            if (items.Any())
            {
                var currentSum = items.Sum(i => i.Percentage);
                if (currentSum != 100)
                {
                    items.First().Percentage += (100 - currentSum);
                }
            }

            return new MarketSentimentData { Items = items };
        }

        private string GetContractCategory(int contractTypeId)
        {
            // Logic remains the same
            return contractTypeId switch
            {
                1 or 4 or 5 or 7 or 9 or 10 or 11 or 12 or 13 or 14 or 17 or 24 or 25 or 27 or 28 or 30 => "نقدی",
                2 or 8 or 16 or 18 or 19 => "سلف",
                3 or 15 or 20 or 21 or 31 or 33 or 34 => "نسیه",
                29 or 32 or 35 or 36 => "پریمیوم",
                _ => "سایر"
            };
        }
        private string GetCategoryColor(string category)
        {
            return category switch
            {
                "نقدی" => "var(--primary-color)",
                "سلف" => "var(--success-color)",
                "نسیه" => "var(--warning-color)",
                "پریمیوم" => "var(--info-color)",
                _ => "var(--neutral-color)"
            };
        }
        #endregion MarketSentiment

        #region MarketExcitement      
        public async Task<MarketExcitementData> GetMarketExcitementAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // بهینه‌سازی: استفاده از کلاس داخلی به جای نوع داینامیک برای جلوگیری از خطا
            var excitementStats = await context.Offers
                .Where(o => o.OfferDate == todayPersian)
                .GroupJoin(
                    context.TradeReports.Where(t => t.TradeDate == todayPersian),
                    o => o.Id,
                    t => t.OfferId,
                    (o, t) => new { Offer = o, Trades = t }
                )
                .Select(x => new ExcitementStat
                {
                    InitPrice = x.Offer.InitPrice,
                    OfferVol = x.Offer.OfferVol,
                    InitVolume = x.Offer.InitVolume,
                    FinalPriceSum = x.Trades.Sum(t => t.FinalWeightedAveragePrice * t.TradeVolume),
                    TradeVolumeSum = x.Trades.Sum(t => t.TradeVolume),
                    DemandVolumeSum = x.Trades.Sum(t => t.DemandVolume)
                })
                .ToListAsync();

            if (!excitementStats.Any())
            {
                return new MarketExcitementData { Title = "بازار آرام", Description = "هنوز عرضه‌ای برای تحلیل هیجان بازار ثبت نشده است.", Percentage = 0, Label = "بدون فعالیت" };
            }

            var competitionIndex = CalculateCompetitionIndex(excitementStats);
            var demandCoverageRate = CalculateDemandCoverageRate(excitementStats);
            var completeTradeRate = CalculateCompleteTradeRate(excitementStats);

            var excitementMetrics = new List<(decimal Value, string Label, System.Func<decimal, (string Title, string Description)> GetContent)>
            {
                (competitionIndex, "رقابت", GetCompetitionContent),
                (demandCoverageRate, "تقاضا", GetDemandContent),
                (completeTradeRate, "فروش", GetCompleteTradeContent)
            };

            var topMetric = excitementMetrics.OrderByDescending(m => m.Value).First();
            var content = topMetric.GetContent(topMetric.Value);

            return new MarketExcitementData
            {
                Title = content.Title,
                Description = content.Description,
                Percentage = (int)System.Math.Round(topMetric.Value),
                Label = topMetric.Label
            };
        }

        // متدهای کمکی برای کار با نوع قوی تایپ شده بازنویسی شده‌اند
        private decimal CalculateCompetitionIndex(List<ExcitementStat> stats)
        {
            decimal totalWeightedBasePriceSum = stats.Sum(s => s.InitPrice * s.OfferVol);
            decimal totalOfferVolume = stats.Sum(s => s.OfferVol);
            decimal weightedAvgBasePrice = totalOfferVolume > 0 ? totalWeightedBasePriceSum / totalOfferVolume : 0;

            decimal totalWeightedFinalPriceSum = stats.Sum(s => s.FinalPriceSum);
            decimal totalTradeVolume = stats.Sum(s => s.TradeVolumeSum);
            decimal weightedAvgFinalPrice = totalTradeVolume > 0 ? totalWeightedFinalPriceSum / totalTradeVolume : 0;

            if (weightedAvgBasePrice == 0) return 0;
            return ((weightedAvgFinalPrice - weightedAvgBasePrice) / weightedAvgBasePrice) * 100;
        }

        private decimal CalculateDemandCoverageRate(List<ExcitementStat> stats)
        {
            decimal totalInitialSupply = stats.Sum(s => s.InitVolume);
            if (totalInitialSupply == 0) return 0;
            decimal totalRegisteredDemand = stats.Sum(s => s.DemandVolumeSum);
            return (totalRegisteredDemand / totalInitialSupply) * 100;
        }

        private decimal CalculateCompleteTradeRate(List<ExcitementStat> stats)
        {
            if (!stats.Any()) return 0;
            int successfulOffers = stats.Count(s => s.TradeVolumeSum >= s.OfferVol);
            return ((decimal)successfulOffers / stats.Count) * 100;
        }

        // Content generators remain unchanged
        private (string Title, string Description) GetCompetitionContent(decimal value)
        {
            if (value > 10) return ("رقابت بالا", "رقابت شدید بین خریداران باعث شده میانگین قیمت معاملات به شکل قابل توجهی بالاتر از قیمت پایه عرضه‌ها باشد.");
            if (value > 2) return ("رقابت متعادل", "رقابت معقولی در معاملات امروز شکل گرفته و قیمت‌ها کمی بالاتر از نرخ پایه کشف شده‌اند.");
            return ("رقابت محدود", "رقابت چندانی در معاملات امروز مشاهده نشد و اکثر معاملات نزدیک به قیمت پایه انجام شدند.");
        }
        private (string Title, string Description) GetDemandContent(decimal value)
        {
            if (value > 150) return ("تقاضای انفجاری", "عطش خرید در بازار بسیار بالاست و حجم تقاضای ثبت‌شده به مراتب بیشتر از حجم کالاهای عرضه‌شده است.");
            if (value > 100) return ("تقاضای قوی", "تقاضا در بازار امروز از عرضه پیشی گرفته که نشان‌دهنده چشم‌انداز مثبت خریداران است.");
            return ("تقاضای عادی", "حجم تقاضا در بازار امروز در محدوده حجم عرضه‌ها قرار دارد و توازن نسبی برقرار است.");
        }
        private (string Title, string Description) GetCompleteTradeContent(decimal value)
        {
            if (value > 90) return ("جذب کامل عرضه‌ها", "قدرت جذب بازار در سطح بالایی قرار دارد و تقریبا تمام کالاهای عرضه‌شده با موفقیت به فروش رسیدند.");
            if (value > 70) return ("جذب خوب عرضه‌ها", "بخش عمده‌ای از کالاهای عرضه‌شده در بازار امروز با موفقیت معامله شدند.");
            return ("جذب گزینشی عرضه‌ها", "خریداران در معاملات امروز به صورت گزینشی عمل کرده و تنها بخشی از عرضه‌ها به فروش کامل رسیدند.");
        }
        #endregion MarketExcitement

        #region SupplyRisk
        public async Task<SupplyRiskData> GetSupplyRiskAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var supplierStatsBySubGroup = await context.Offers
                .Where(o => o.OfferDate == todayPersian)
                .Join(context.Commodities, o => o.CommodityId, c => c.Id, (o, c) => new { o, c })
                .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.o, sg })
                .GroupBy(x => x.sg)
                .Select(g => new
                {
                    SubGroupId = g.Key.Id,
                    SubGroupName = g.Key.PersianName,
                    SupplierCount = g.Select(x => x.o.SupplierId).Distinct().Count(),
                    TotalOfferVolume = g.Sum(x => x.o.OfferVol)
                })
                .ToListAsync();

            if (!supplierStatsBySubGroup.Any())
            {
                return new SupplyRiskData { Items = new List<SupplyRiskItem>() };
            }

            var items = new List<SupplyRiskItem>();

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
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var moversData = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian && t.OfferBasePrice > 0 && t.OfferVolume > 0)
                .Join(context.Commodities, t => t.CommodityId, c => c.Id, (t, c) => new { t, c })
                .Join(context.SubGroups, j => j.c.ParentId, sg => sg.Id, (j, sg) => new { j.t, sg })
                .Join(context.Manufacturers, j => j.t.ManufacturerId, m => m.Id, (j, m) => new
                {
                    SubGroupName = j.sg.PersianName,
                    ManufacturerName = m.PersianName,
                    Competition = ((j.t.FinalWeightedAveragePrice - j.t.OfferBasePrice) / j.t.OfferBasePrice) * 100,
                    DemandRatio = j.t.DemandVolume / j.t.OfferVolume
                })
                .ToListAsync();

            var competitionItems = moversData
                .OrderByDescending(d => d.Competition)
                .GroupBy(d => d.SubGroupName) // Ensure unique subgroups
                .Select(g => g.First())
                .Take(2)
                .Select((item, index) => new MarketMoverItem
                {
                    Rank = index + 1,
                    Title = item.SubGroupName,
                    Subtitle = item.ManufacturerName,
                    Value = $"+{item.Competition:F1}%",
                    ValueState = ValueState.Positive
                })
                .ToList();

            var demandItems = moversData
                .OrderByDescending(d => d.DemandRatio)
                .GroupBy(d => d.SubGroupName) // Ensure unique subgroups
                .Select(g => g.First())
                .Take(2)
                .Select((item, index) => new MarketMoverItem
                {
                    Rank = index + 1,
                    Title = item.SubGroupName,
                    Subtitle = item.ManufacturerName,
                    Value = $"{item.DemandRatio:F1}x",
                    ValueState = ValueState.Neutral
                })
                .ToList();

            return new MarketMoversData { CompetitionItems = competitionItems, DemandItems = demandItems };
        }
        #endregion MarketMovers

        #region MainPlayers
        public async Task<List<MainPlayer>> GetMainPlayersAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var trades = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian)
                .Select(t => new { t.SupplierId, t.SellerBrokerId, t.TradeValue })
                .ToListAsync();

            if (!trades.Any()) return new List<MainPlayer>();

            var totalMarketValue = trades.Sum(t => t.TradeValue);
            if (totalMarketValue == 0) return new List<MainPlayer>();

            var topSupplierData = trades
                .GroupBy(t => t.SupplierId)
                .Select(g => new { Id = g.Key, TotalValue = g.Sum(t => t.TradeValue) })
                .OrderByDescending(s => s.TotalValue)
                .FirstOrDefault();

            var topBrokerData = trades
                .GroupBy(t => t.SellerBrokerId)
                .Select(g => new { Id = g.Key, TotalValue = g.Sum(t => t.TradeValue) })
                .OrderByDescending(b => b.TotalValue)
                .FirstOrDefault();

            var mainPlayers = new List<MainPlayer>();

            if (topSupplierData != null)
            {
                var topSupplierName = await context.Suppliers
                    .Where(s => s.Id == topSupplierData.Id)
                    .Select(s => new { s.Id, s.PersianName })
                    .FirstOrDefaultAsync();
                mainPlayers.Add(new MainPlayer
                {
                    Type = MainPlayerType.Supplier, Id = topSupplierName?.Id ?? 0,  Name = topSupplierName?.PersianName ?? "نامشخص", IconCssClass = "bi bi-buildings-fill",
                    MarketShare = (topSupplierData.TotalValue / totalMarketValue) * 100
                });
            }

            if (topBrokerData != null)
            {
                var topBrokerName = await context.Brokers
                    .Where(b => b.Id == topBrokerData.Id)
                    .Select(b => new { b.Id, b.PersianName })
                    .FirstOrDefaultAsync();
                mainPlayers.Add(new MainPlayer
                {
                    Type = MainPlayerType.Broker, Id = topBrokerName?.Id ?? 0, Name = topBrokerName?.PersianName ?? "نامشخص", IconCssClass = "bi bi-person-workspace",
                    MarketShare = (topBrokerData.TotalValue / totalMarketValue) * 100
                });
            }
            return mainPlayers;
        }
        #endregion MainPlayers

        #region TradingHalls
        public async Task<TradingHallsData> GetTradingHallsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // رفع خطا: ابتدا داده‌ها را بدون مرتب‌سازی از دیتابیس واکشی می‌کنیم
            var hallsDataUnsorted = await context.TradeReports
                .Where(t => t.TradeDate == todayPersian)
                .Join(context.Offers, t => t.OfferId, o => o.Id, (t, o) => new { t, o })
                .GroupBy(x => x.o.TradingHallId)
                .Select(g => new
                {
                    HallId = g.Key,
                    TotalValue = g.Sum(x => x.t.TradeValue * 1000),
                    TotalTradedVolume = g.Sum(x => x.t.TradeVolume),
                    TotalOfferedVolume = g.Sum(x => x.o.OfferVol)
                })
                .Join(context.TradingHalls,
                      h => h.HallId,
                      th => th.Id,
                      (h, th) => new
                      {
                          HallName = th.PersianName,
                          h.TotalValue,
                          AbsorptionRate = h.TotalOfferedVolume > 0 ? (h.TotalTradedVolume / h.TotalOfferedVolume) * 100 : 0
                      })
                .ToListAsync();
            
            // سپس مرتب‌سازی را در حافظه انجام می‌دهیم
            var hallsData = hallsDataUnsorted
                .OrderByDescending(h => h.TotalValue)
                .ToList();

            if (!hallsData.Any())
            {
                return new TradingHallsData { Items = new List<TradingHallItem>() };
            }

            var items = hallsData.Select(h =>
            {
                var (icon, bgClass) = GetHallVisuals(h.HallName);
                ValueState state;
                if (h.AbsorptionRate > 85) state = ValueState.Positive;
                else if (h.AbsorptionRate > 60) state = ValueState.Neutral;
                else state = ValueState.Negative;

                return new TradingHallItem
                {
                    Title = h.HallName,
                    IconCssClass = icon,
                    IconBgCssClass = bgClass,
                    Value = FormatLargeNumber(h.TotalValue),
                    Change = $"نسبت فروش به عرضه: {h.AbsorptionRate:F0}%",
                    ChangeState = state
                };
            }).ToList();

            return new TradingHallsData { Items = items };
        }

        private (string IconCssClass, string IconBgCssClass) GetHallVisuals(string hallName)
        {
            if (hallName.Contains("صنعتی")) return ("bi bi-building", "industrial");
            if (hallName.Contains("پتروشیمی")) return ("bi bi-droplet-fill", "petro");
            if (hallName.Contains("کشاورزی")) return ("bi bi-tree-fill", "agri");
            if (hallName.Contains("نفتی")) return ("bi bi-fuel-pump-fill", "oil-prod");
            if (hallName.Contains("سیمان")) return ("bi bi-stack", "industrial");
            if (hallName.Contains("خودرو")) return ("bi bi-car-front-fill", "auto");
            if (hallName.Contains("طلا")) return ("bi bi-coin", "gold");
            if (hallName.Contains("صادراتی")) return ("bi bi-globe-americas", "export");
            if (hallName.Contains("حراج")) return ("bi bi-hammer", "auction");
            if (hallName.Contains("املاک")) return ("bi bi-house-door-fill", "real-estate");
            if (hallName.Contains("پریمیوم")) return ("bi bi-gem", "premium");
            if (hallName.Contains("مناقصه")) return ("bi bi-file-earmark-text-fill", "tender");
            return ("bi bi-grid-fill", "other");
        }
        #endregion TradingHalls

        #region MarketProgress
        public async Task<MarketProgressData> GetMarketProgressAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            var query = from offer in context.Offers.Where(o => o.OfferDate == todayPersian)
                        join commodity in context.Commodities on offer.CommodityId equals commodity.Id
                        join subGroup in context.SubGroups on commodity.ParentId equals subGroup.Id
                        join grp in context.Groups on subGroup.ParentId equals grp.Id
                        join mainGroup in context.MainGroups on grp.ParentId equals mainGroup.Id
                        join trade in context.TradeReports.Where(t => t.TradeDate == todayPersian)
                              on offer.Id equals trade.OfferId into trades
                        from trade in trades.DefaultIfEmpty()
                        select new { mainGroup, offer, trade };

            var progressItemsData = await query
                .GroupBy(x => x.mainGroup)
                .Select(g => new
                {
                    MainGroupName = g.Key.PersianName,
                    TotalOffers = g.Select(x => x.offer.Id).Distinct().Count(),
                    TradedOffers = g.Where(x => x.trade != null).Select(x => x.offer.Id).Distinct().Count()
                })
                .OrderByDescending(d => d.TotalOffers)
                .ToListAsync();

            var items = progressItemsData.Select(item => new MarketProgressDetail
            {
                Name = item.MainGroupName,
                TotalOffers = item.TotalOffers,
                TradedOffers = item.TradedOffers,
                CssClass = GetMainGroupCssClass(item.MainGroupName)
            }).ToList();

            return new MarketProgressData { Items = items };
        }

        private string GetMainGroupCssClass(string mainGroupName)
        {
            if (mainGroupName.Contains("صنعتی")) return "industrial";
            if (mainGroupName.Contains("پتروشیمی")) return "petro";
            if (mainGroupName.Contains("کشاورزی")) return "agri";
            if (mainGroupName.Contains("نفتی")) return "oil-prod";
            if (mainGroupName.Contains("سیمان")) return "industrial";
            if (mainGroupName.Contains("خودرو")) return "auto";
            if (mainGroupName.Contains("طلا")) return "gold";
            if (mainGroupName.Contains("صادراتی")) return "export";
            if (mainGroupName.Contains("حراج")) return "auction";
            if (mainGroupName.Contains("املاک")) return "real-estate";
            if (mainGroupName.Contains("پریمیوم")) return "premium";
            if (mainGroupName.Contains("مناقصه")) return "tender";
            return "other";
        }
        #endregion MarketProgress

        #region SpotNotifications
        public async Task<SpotNotificationData> GetSpotNotificationsAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            var todayNotifications = context.SpotNotifications
                //.Where(n => n.NewsDateTime.Date == DateTime.Today)
                .OrderByDescending(n => n.NewsDateTime)
                .Take(5)
                .ToList();

            var items = todayNotifications.Select(n => new SpotNotificationItem
            {
                Title = n.MainTitle ?? string.Empty,
                Source = n.URL ?? string.Empty,
                Category = GetNotificationCategory(n.MainTitle)
            }).ToList();

            return new SpotNotificationData { Items = items };
        }

        private SpotNotificationCategory GetNotificationCategory(string? title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return SpotNotificationCategory.Other;
            }

            if (title.Contains("اصلاحیه")) return SpotNotificationCategory.Amendment;
            if (title.Contains("پذیرش کالا")) return SpotNotificationCategory.ProductAcceptance;
            if (title.Contains("پذیرش خودرو")) return SpotNotificationCategory.CarAcceptance;
            if (title.Contains("تمدید مجوز")) return SpotNotificationCategory.LicenseRenewal;
            if (title.Contains("ابلاغیه")) return SpotNotificationCategory.Announcement;
            
            return SpotNotificationCategory.Other;
        }
        #endregion SpotNotifications
    }
}
