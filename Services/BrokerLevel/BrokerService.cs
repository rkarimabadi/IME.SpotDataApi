// File: IME.SpotDataApi/Services/Broker/BrokerService.cs

using IME.SpotDataApi.Data; // Assuming AppDataContext is here
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Models.Spot;
using IME.SpotDataApi.Services.Caching;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace IME.SpotDataApi.Services.BrokerLevel
{
    public class BrokerService : IBrokerService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        private readonly ICacheService _cacheService;
        private readonly IDateHelper _dateHelper;

        public BrokerService(IDbContextFactory<AppDataContext> contextFactory, IDateHelper dateHelper, ICacheService cacheService)
        {
            _contextFactory = contextFactory;
            _cacheService = cacheService;
            _dateHelper = dateHelper;
        }

        public async Task<BrokerHeaderData> GetBrokerHeaderAsync(int brokerId)
        {
            string cacheKey = $"Broker_Header_{brokerId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var broker = await context.Brokers
                    .Where(b => b.Id == brokerId)
                    .Select(b => b.PersianName)
                    .FirstOrDefaultAsync();

                return new BrokerHeaderData
                {
                    BrokerName = broker ?? "کارگزار یافت نشد"
                };
            }, expirationInMinutes: 720);
        }
        public async Task<CompetitionData> GetCompetitionRatioAsync(int brokerId, int days = 90)
        {
            string cacheKey = $"Broker_CompetitionRatio_{brokerId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var dateFrom = _dateHelper.GetPersian(DateTime.Now.AddDays(-days));

                // واکشی تمام نسبت‌های رقابت برای معاملات موفق
                var competitionStats = await context.TradeReports
                    .Where(t => t.SellerBrokerId == brokerId &&
                                  string.Compare(t.TradeDate, dateFrom) >= 0 &&
                                  t.OfferBasePrice > 0)
                    .Select(t => (double)(t.FinalWeightedAveragePrice - t.OfferBasePrice) / (double)t.OfferBasePrice)
                    .ToListAsync();

                // اگر معامله‌ای وجود نداشته باشد
                if (!competitionStats.Any())
                {
                    return new CompetitionData
                    {
                        Percentage = 0,
                        Title = "قدرت فروش",
                        Label = "بدون معامله",
                        Description = "در بازه زمانی انتخاب شده، معامله‌ای برای این کارگزار ثبت نشده است تا قدرت فروش آن ارزیابی شود."
                    };
                }

                // محاسبه میانگین و تبدیل به درصد
                var avgCompetition = competitionStats.Average() * 100;

                // --- منطق جدید برای انتخاب متن توصیفی ---
                var (label, description) = avgCompetition switch
                {
                    > 10 => ("عالی", "این کارگزاری با ثبت رقابت‌های سنگین، تسلط کامل خود را در فروش کالاها با حداکثر قیمت ممکن به نمایش می‌گذارد و یک بازیگر کلیدی در بازارهای پرتقاضا است."),
                    > 5 => ("قوی", "بیانگر توانایی بالای کارگزار در بازاریابی عرضه‌ها و ایجاد یک محیط رقابتی است که منجر به فروش کالاها با قیمتی کاملاً بالاتر از نرخ پایه می‌شود."),
                    > 2 => ("متوسط", "عملکردی استاندارد و قابل قبول که نشان می‌دهد کارگزار در اغلب موارد موفق به ایجاد رقابت و فروش کالا بالاتر از قیمت پایه شده است."),
                    > 0 => ("پایین", "رقابت ثبت‌شده ناچیز است. این امر می‌تواند ناشی از فعالیت در بازارهای کم‌تقاضا یا عدم موفقیت در جذب مشتریان برای رقابت بر سر کالاها باشد."),
                    _ => ("ضعیف", "فروش کالاها به طور میانگین پایین‌تر از قیمت پایه، یک سیگنال منفی جدی است که می‌تواند نشان‌دهنده رکود شدید در کالاهای کارگزار یا تلاش برای فروش به هر قیمتی باشد.")
                };

                return new CompetitionData
                {
                    Percentage = avgCompetition,
                    Label = label,
                    Title = "قدرت فروش",
                    Description = description
                };
            }, expirationInMinutes: 720);
        }
        public async Task<CompetitionData> GetSuccessRateAsync(int brokerId, int days = 90)
        {
            string cacheKey = $"Broker_SuccessRate_{brokerId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var dateFrom = _dateHelper.GetPersian(DateTime.Now.AddDays(-days));

                // تمام عرضه‌های کارگزار در بازه زمانی مشخص
                var offers = context.Offers.Where(o => o.BrokerId == brokerId && string.Compare(o.OfferDate, dateFrom) >= 0);

                var totalOfferCount = await offers.CountAsync();

                // اگر عرضه‌ای وجود نداشته باشد
                if (totalOfferCount == 0)
                {
                    return new CompetitionData
                    {
                        Percentage = 0,
                        Title = "بهره‌وری در معاملات",
                        Label = "بدون عرضه",
                        Description = "در بازه زمانی انتخاب شده، عرضه‌ای توسط این کارگزار انجام نشده است تا بتوان بهره‌وری آن را سنجید."
                    };
                }

                // شمارش عرضه‌هایی که برای آن‌ها معامله ثبت شده است
                var tradedOfferCount = await offers
                    .Where(o => context.TradeReports.Any(t => t.OfferId == o.Id))
                    .CountAsync();

                // محاسبه نرخ موفقیت
                var successRate = (double)tradedOfferCount / totalOfferCount * 100;

                // --- منطق جدید برای انتخاب متن توصیفی ---
                var (label, description) = successRate switch
                {
                    > 95 => ("فوق‌العاده", "نرخ موفقیت نزدیک به ۱۰۰٪ نشان‌دهنده حداکثر بهره‌وری و تضمین معامله است. این کارگزار در تبدیل عرضه به معامله یک متخصص تمام‌عیار محسوب می‌شود."),
                    > 85 => ("بسیار قوی", "بیانگر کارایی بالا و شناخت عمیق از بازار است. اکثر عرضه‌های این کارگزار با موفقیت به معامله منجر شده و از اعتبار بالایی برخوردار است."),
                    > 70 => ("خوب", "عملکردی قابل اعتماد و استاندارد که نشان می‌دهد این کارگزار در بخش بزرگی از فعالیت‌های خود موفق به ثبت معامله شده است."),
                    > 50 => ("قابل بهبود", "نرخ موفقیت متوسط است. در حالی که بخشی از عرضه‌ها به معامله ختم شده، فرصت قابل توجهی برای بهبود در قیمت‌گذاری یا زمان‌بندی عرضه‌ها وجود دارد."),
                    _ => ("ضعیف", "کمتر از نیمی از عرضه‌ها به معامله منجر شده است. این موضوع می‌تواند ناشی از قیمت‌گذاری غیررقابتی، عدم تقاضا برای کالاها یا استراتژی‌های فروش ناکارآمد باشد.")
                };

                return new CompetitionData
                {
                    Percentage = successRate,
                    Label = label,
                    Title = "بهره‌وری در معاملات",
                    Description = description
                };
            }, expirationInMinutes: 720);
        }
        public async Task<List<MarketShareItem>> GetMarketShareAsync(int brokerId, int days = 90)
        {
            string cacheKey = $"Broker_MarketShare_{brokerId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var dateFrom = _dateHelper.GetPersian(DateTime.Now.AddDays(-days));

                var allTrades = context.TradeReports.Where(t => string.Compare(t.TradeDate, dateFrom) >= 0);

                var totalMarketStats = await allTrades
                    .GroupBy(t => 1)
                    .Select(g => new
                    {
                        TotalValue = g.Sum(t => t.TradeValue),   // In 1000 Rials
                        TotalVolume = g.Sum(t => t.TradeVolume) // In Ton
                    }).FirstOrDefaultAsync();

                var brokerStats = await allTrades
                    .Where(t => t.SellerBrokerId == brokerId)
                    .GroupBy(t => 1)
                    .Select(g => new
                    {
                        BrokerValue = g.Sum(t => t.TradeValue),
                        BrokerVolume = g.Sum(t => t.TradeVolume)
                    }).FirstOrDefaultAsync();

                if (totalMarketStats == null) return new List<MarketShareItem>();

                var valueShare = totalMarketStats.TotalValue > 0 ? (brokerStats?.BrokerValue ?? 0) / totalMarketStats.TotalValue * 100 : 0;
                var volumeShare = totalMarketStats.TotalVolume > 0 ? (brokerStats?.BrokerVolume ?? 0) / totalMarketStats.TotalVolume * 100 : 0;

                return new List<MarketShareItem>
            {
                new MarketShareItem
                {
                    Title = "سهم از ارزش معاملات",
                    Value = $"{valueShare:F1}%",
                    Subtitle = $"معادل {FormatValue(brokerStats?.BrokerValue ?? 0)}",
                    IconCssClass = "bi bi-cash-stack green",
                    ThemeCssClass = "value-card"
                },
                new MarketShareItem
                {
                    Title = "سهم از حجم معاملات",
                    Value = $"{volumeShare:F1}%",
                    Subtitle = $"معادل {FormatVolume(brokerStats?.BrokerVolume ?? 0)}",
                    IconCssClass = "bi bi-stack blue",
                    ThemeCssClass = "volume-card"
                }
            };
            }, expirationInMinutes: 720);
        }
        public async Task<List<RankingItem>> GetRankingAsync(int brokerId, int days = 90)
        {
            string cacheKey = $"Broker_Ranking_{brokerId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var dateFrom = _dateHelper.GetPersian(DateTime.Now.AddDays(-days));

                // --- بخش اول: رتبه‌بندی بر اساس ارزش و حجم (مانند قبل) ---
                var brokerPerformance = await context.TradeReports
                    .Where(t => string.Compare(t.TradeDate, dateFrom) >= 0)
                    .GroupBy(t => t.SellerBrokerId)
                    .Select(g => new
                    {
                        BrokerId = g.Key,
                        TotalValue = g.Sum(t => t.TradeValue),
                        TotalVolume = g.Sum(t => t.TradeVolume)
                    })
                    .ToListAsync();

                var valueRanking = brokerPerformance
                    .OrderByDescending(b => b.TotalValue)
                    .Select((b, i) => new { b.BrokerId, Rank = i + 1 })
                    .FirstOrDefault(b => b.BrokerId == brokerId);

                var volumeRanking = brokerPerformance
                    .OrderByDescending(b => b.TotalVolume)
                    .Select((b, i) => new { b.BrokerId, Rank = i + 1 })
                    .FirstOrDefault(b => b.BrokerId == brokerId);

                // --- بخش جدید: رتبه‌بندی بر اساس فعالیت (نرخ موفقیت) ---
                var offersInPeriod = context.Offers.Where(o => string.Compare(o.OfferDate, dateFrom) >= 0);

                // محاسبه تعداد کل عرضه‌ها برای هر کارگزار
                var totalOffersByBroker = await offersInPeriod
                    .GroupBy(o => o.BrokerId)
                    .Select(g => new { BrokerId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(k => k.BrokerId, v => v.Count);

                // محاسبه تعداد عرضه‌های موفق برای هر کارگزار
                var tradedOffersByBroker = await offersInPeriod
                    .Where(o => context.TradeReports.Any(t => t.OfferId == o.Id))
                    .GroupBy(o => o.BrokerId)
                    .Select(g => new { BrokerId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(k => k.BrokerId, v => v.Count);

                // محاسبه نرخ موفقیت و رتبه‌بندی
                var activityPerformance = totalOffersByBroker.Keys
                    .Select(id =>
                    {
                        var totalOffers = totalOffersByBroker[id];
                        tradedOffersByBroker.TryGetValue(id, out var tradedOffers); // اگر معامله‌ای نباشد، صفر است
                        var successRate = totalOffers > 0 ? (double)tradedOffers / totalOffers * 100 : 0;
                        return new { BrokerId = id, SuccessRate = successRate };
                    })
                    .OrderByDescending(b => b.SuccessRate)
                    .ToList();

                var activityRankingInfo = activityPerformance
                    .Select((b, i) => new { b.BrokerId, b.SuccessRate, Rank = i + 1 })
                    .FirstOrDefault(b => b.BrokerId == brokerId);

                // --- تجمیع نتایج ---
                return new List<RankingItem>
            {
                new RankingItem
                {
                    Title = "رتبه بر اساس ارزش",
                    Rank = valueRanking?.Rank ?? 0,
                    Subtitle = "بر اساس کل ارزش معاملات",
                    IconCssClass = "bi bi-cash-stack",
                    ThemeCssClass = "value-rank"
                },
                new RankingItem
                {
                    Title = "رتبه بر اساس حجم",
                    Rank = volumeRanking?.Rank ?? 0,
                    Subtitle = "بر اساس کل حجم معاملات",
                    IconCssClass = "bi bi-stack",
                    ThemeCssClass = "volume-rank"
                },
                new RankingItem
                {
                    Title = "رتبه بر اساس فعالیت",
                    Rank = activityRankingInfo?.Rank ?? 0,
                    Subtitle = activityRankingInfo != null ? $"نرخ موفقیت عرضه {activityRankingInfo.SuccessRate:F1}٪" : "بدون فعالیت",
                    IconCssClass = "bi bi-check-circle-fill",
                    ThemeCssClass = "activity-rank"
                }
            };
            }, expirationInMinutes: 720);
        }
        public async Task<List<CommodityGroupShareItem>> GetCommodityGroupShareAsync(int brokerId, int days = 90)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            string cacheKey = $"Broker_CommodityGroupShare_{brokerId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                var dateFrom = _dateHelper.GetPersian(DateTime.Now.AddDays(-days));

                // 1. واکشی تمام معاملات کارگزار به همراه نام گروه اصلی کالا
                var brokerTrades = await (
                    from tr in context.TradeReports
                    where tr.SellerBrokerId == brokerId && string.Compare(tr.TradeDate, dateFrom) >= 0
                    join c in context.Commodities on tr.CommodityId equals c.Id
                    join sg in context.SubGroups on c.ParentId equals sg.Id
                    join g in context.Groups on sg.ParentId equals g.Id
                    join mg in context.MainGroups on g.ParentId equals mg.Id
                    select new { tr.TradeValue, MainGroupName = mg.PersianName }
                ).ToListAsync();

                if (!brokerTrades.Any())
                {
                    return new List<CommodityGroupShareItem>();
                }

                // 2. محاسبه ارزش کل معاملات به عنوان مبنای درصدگیری
                var totalValue = brokerTrades.Sum(t => t.TradeValue);
                if (totalValue == 0) return new List<CommodityGroupShareItem>();

                // 3. گروه‌بندی معاملات بر اساس نام گروه و محاسبه مجموع ارزش هر گروه
                var groupPerformance = brokerTrades
                    .GroupBy(t => t.MainGroupName)
                    .Select(g => new
                    {
                        GroupName = g.Key,
                        Value = g.Sum(t => t.TradeValue)
                    })
                    .OrderByDescending(g => g.Value)
                    .ToList();

                var result = new List<CommodityGroupShareItem>();
                var colorPalette = new[] { "#007bff", "#28a745", "#ffc107", "#dc3545", "#6f42c1" };
                const int topN = 4; // نمایش 4 گروه برتر به صورت مجزا

                // 4. افزودن 4 گروه برتر به لیست نهایی
                var topGroups = groupPerformance.Take(topN);
                int colorIndex = 0;
                foreach (var group in topGroups)
                {
                    result.Add(new CommodityGroupShareItem
                    {
                        GroupName = group.GroupName,
                        Percentage = (double)(group.Value / totalValue) * 100,
                        Color = colorPalette[colorIndex++]
                    });
                }

                // 5. اگر تعداد گروه‌ها بیشتر از 4 بود، مابقی را در دسته "سایر" تجمیع کن
                if (groupPerformance.Count > topN)
                {
                    var otherValue = groupPerformance.Skip(topN).Sum(g => g.Value);
                    if (otherValue > 0)
                    {
                        result.Add(new CommodityGroupShareItem
                        {
                            GroupName = "سایر گروه‌ها",
                            Percentage = (double)(otherValue / totalValue) * 100,
                            Color = "#6c757d" // یک رنگ خنثی برای دسته "سایر"
                        });
                    }
                }

                return result;
            }, expirationInMinutes: 720);
        }
        public async Task<UpcomingOffersData> GetBrokerOffersAsync(int brokerId)
        {
            string cacheKey = $"Broker_BrokerOffers_{brokerId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {            // یک context واحد برای کل عملیات ایجاد می‌شود
                using var context = await _contextFactory.CreateDbContextAsync();
                var todayPersian = _dateHelper.GetPersian(DateTime.Now);

                // --- 1. واکشی عرضه‌های آینده ---
                var futureOffersQuery = context.Offers
                    .Where(o => o.BrokerId == brokerId && string.Compare(o.OfferDate, todayPersian) > 0)
                    .OrderBy(o => o.OfferDate);

                var futureItems = await ProcessOffersQuery(futureOffersQuery, OfferDateType.Future, context);

                // --- 2. واکشی عرضه‌های امروز ---
                var todayOffersQuery = context.Offers
                    .Where(o => o.BrokerId == brokerId && o.OfferDate == todayPersian);

                var todayItems = await ProcessOffersQuery(todayOffersQuery, OfferDateType.Today, context);

                // --- 3. واکشی عرضه‌های گذشته ---
                var pastOffersQuery = context.Offers
                    .Where(o => o.BrokerId == brokerId && string.Compare(o.OfferDate, todayPersian) < 0)
                    .OrderByDescending(o => o.OfferDate)
                    .Take(15); // محدود کردن به ۱۵ عرضه اخیر برای بهینه‌سازی

                var pastItems = await ProcessOffersQuery(pastOffersQuery, OfferDateType.Past, context);

                // --- 4. ترکیب نتایج ---
                // ترتیب نمایش: امروز، آینده، گذشته
                var allItems = todayItems.Concat(futureItems).Concat(pastItems).ToList();

                return new UpcomingOffersData { Items = allItems };
            }, expirationInMinutes: 720);
        }
        #region Private Helpers

        // ... other private helpers like FormatValue

        /// <summary>
        /// یک کوئری از عرضه‌ها را پردازش کرده و به لیست UpcomingOfferItem تبدیل می‌کند.
        /// این متد کمکی از CommodityService به اینجا منتقل شده است.
        /// </summary>
        private async Task<List<UpcomingOfferItem>> ProcessOffersQuery(IQueryable<IME.SpotDataApi.Models.Spot.Offer> query, OfferDateType dateType, AppDataContext context)
        {
            var pc = new PersianCalendar();

            var offerData = await query
                .Join(context.Suppliers, o => o.SupplierId, s => s.Id, (o, s) => new { Offer = o, Supplier = s })
                .Join(context.Commodities, j => j.Offer.CommodityId, c => c.Id, (j, c) => new
                {
                    j.Offer.Id,
                    j.Offer.OfferDate,
                    CommodityName = c.PersianName,
                    SupplierName = j.Supplier.PersianName,
                    UrlName = c.Symbol
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

        /// <summary>
        /// نام فارسی روز هفته را برمی‌گرداند.
        /// </summary>
        private string GetPersianDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Saturday => "شنبه",
                DayOfWeek.Sunday => "یکشنبه",
                DayOfWeek.Monday => "دوشنبه",
                DayOfWeek.Tuesday => "سه‌شنبه",
                DayOfWeek.Wednesday => "چهارشنبه",
                DayOfWeek.Thursday => "پنجشنبه",
                DayOfWeek.Friday => "جمعه",
                _ => ""
            };
        }
        #endregion
        public async Task<TopSuppliersData> GetTopSuppliersAsync(int brokerId, int days = 90)
        {
            string cacheKey = $"Broker_TopSuppliers_{brokerId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var dateFrom = _dateHelper.GetPersian(DateTime.Now.AddDays(-days));

                // واکشی و تجمیع آمار معاملات بر اساس عرضه‌کننده
                var supplierStats = await (
                    from tr in context.TradeReports
                    where tr.SellerBrokerId == brokerId && string.Compare(tr.TradeDate, dateFrom) >= 0
                    join o in context.Offers on tr.OfferId equals o.Id
                    join s in context.Suppliers on o.SupplierId equals s.Id
                    group tr by new { s.Id, s.PersianName } into g
                    select new
                    {
                        SupplierId = g.Key.Id,
                        SupplierName = g.Key.PersianName,
                        TotalValue = g.Sum(tr => tr.TradeValue),
                        TotalVolume = g.Sum(tr => tr.TradeVolume),
                        TradeCount = g.Count()
                    }
                ).ToListAsync();

                if (!supplierStats.Any())
                {
                    return new TopSuppliersData
                    {
                        ByValue = new List<SupplierPerformanceItem>(),
                        ByVolume = new List<SupplierPerformanceItem>(),
                        ByCount = new List<SupplierPerformanceItem>()
                    };
                }

                // مرتب‌سازی بر اساس ارزش و تبدیل به مدل SupplierPerformanceItem
                var topByValue = supplierStats
                    .OrderByDescending(s => s.TotalValue)
                    .Take(5)
                    .Select(s => new SupplierPerformanceItem
                    {
                        SupplierId = s.SupplierId,
                        SupplierName = s.SupplierName,
                        Value = s.TotalValue,
                        DisplayValue = FormatValue(s.TotalValue)
                    }).ToList();

                // مرتب‌سازی بر اساس حجم و تبدیل به مدل SupplierPerformanceItem
                var topByVolume = supplierStats
                    .OrderByDescending(s => s.TotalVolume)
                    .Take(5)
                    .Select(s => new SupplierPerformanceItem
                    {
                        SupplierId = s.SupplierId,
                        SupplierName = s.SupplierName,
                        Value = s.TotalVolume,
                        DisplayValue = FormatVolume(s.TotalVolume)
                    }).ToList();

                // مرتب‌سازی بر اساس تعداد و تبدیل به مدل SupplierPerformanceItem
                var topByCount = supplierStats
                    .OrderByDescending(s => s.TradeCount)
                    .Take(5)
                    .Select(s => new SupplierPerformanceItem
                    {
                        SupplierId = s.SupplierId,
                        SupplierName = s.SupplierName,
                        Value = s.TradeCount,
                        DisplayValue = $"{s.TradeCount} عرضه"
                    }).ToList();

                return new TopSuppliersData
                {
                    ByValue = topByValue,
                    ByVolume = topByVolume,
                    ByCount = topByCount
                };
            }, expirationInMinutes: 720);
        }
        public async Task<List<SupplierItem>> GetAllSuppliersAsync(int brokerId, int days = 90)
        {
            string cacheKey = $"Broker_AllSuppliers_{brokerId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var dateFrom = _dateHelper.GetPersian(DateTime.Now.AddDays(-days));

                // 1. اتصال جداول معاملات، عرضه‌ها و عرضه‌کنندگان
                var suppliersQuery = from tr in context.TradeReports
                                     where tr.SellerBrokerId == brokerId && string.Compare(tr.TradeDate, dateFrom) >= 0
                                     join o in context.Offers on tr.OfferId equals o.Id
                                     join s in context.Suppliers on o.SupplierId equals s.Id
                                     // 2. انتخاب اطلاعات مورد نیاز از عرضه‌کننده
                                     select new { s.Id, s.PersianName };

                // 3. حذف موارد تکراری، مرتب‌سازی و تبدیل به مدل خروجی
                var distinctSuppliers = await suppliersQuery
                    .Distinct()
                    .OrderBy(s => s.PersianName) // مرتب‌سازی بر اساس نام برای خوانایی
                    .Select(s => new SupplierItem
                    {
                        Id = s.Id,
                        Name = s.PersianName
                    })
                    .ToListAsync();

                return distinctSuppliers;
            }, expirationInMinutes: 720);
        }
        private static readonly Dictionary<int, string> StrategicCommodities = new()
        {
            { 1201, "شمش فولادی" },
            { 1302, "کاتد مس" },
            { 1205, "ورق گرم" },
            { 1208, "میلگرد" },
            { 1510, "پلی اتیلن" }
        };
        public async Task<List<StrategicPerformanceItem>> GetStrategicPerformanceAsync(int brokerId, int days = 90)
        {
            string cacheKey = $"Broker_StrategicPerformance_{brokerId}";
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var dateFrom = _dateHelper.GetPersian(DateTime.Now.AddDays(-days));
                var resultList = new List<StrategicPerformanceItem>();

                // کوئری پایه برای تمام معاملات در بازه زمانی مشخص
                var allTradesInPeriod = context.TradeReports.Where(t => string.Compare(t.TradeDate, dateFrom) >= 0);

                // 2. محاسبه سهم بازار کلی کارگزار (معیار پایه)
                var totalMarketValue = await allTradesInPeriod.SumAsync(t => (decimal?)t.TradeValue) ?? 0;
                var brokerOverallValue = await allTradesInPeriod.Where(t => t.SellerBrokerId == brokerId).SumAsync(t => (decimal?)t.TradeValue) ?? 0;
                var overallValueShare = totalMarketValue > 0 ? (double)(brokerOverallValue / totalMarketValue) : 0;

                var totalMarketVolume = await allTradesInPeriod.SumAsync(t => (decimal?)t.TradeVolume) ?? 0;
                var brokerOverallVolume = await allTradesInPeriod.Where(t => t.SellerBrokerId == brokerId).SumAsync(t => (decimal?)t.TradeVolume) ?? 0;
                var overallVolumeShare = totalMarketVolume > 0 ? (double)(brokerOverallVolume / totalMarketVolume) : 0;

                // 3. محاسبه سهم بازار برای هر کالای استراتژیک
                foreach (var (commodityId, commodityName) in StrategicCommodities)
                {
                    var commodityTrades = allTradesInPeriod.Where(t => t.CommodityId == commodityId);

                    // آمار کل بازار برای این کالا
                    var totalCommodityValue = await commodityTrades.SumAsync(t => (decimal?)t.TradeValue) ?? 0;
                    var totalCommodityVolume = await commodityTrades.SumAsync(t => (decimal?)t.TradeVolume) ?? 0;

                    // آمار کارگزار برای این کالا
                    var brokerCommodityValue = await commodityTrades.Where(t => t.SellerBrokerId == brokerId).SumAsync(t => (decimal?)t.TradeValue) ?? 0;
                    var brokerCommodityVolume = await commodityTrades.Where(t => t.SellerBrokerId == brokerId).SumAsync(t => (decimal?)t.TradeVolume) ?? 0;

                    // 4. مقایسه و تعیین وضعیت عملکرد
                    var valuePerformance = PerformanceStatus.NotPresent;
                    if (brokerCommodityValue > 0 && totalCommodityValue > 0)
                    {
                        var commodityValueShare = (double)(brokerCommodityValue / totalCommodityValue);
                        // اگر سهم در کالا بیش از 25% بالاتر از سهم کلی باشد، عملکرد قوی است
                        valuePerformance = commodityValueShare > (overallValueShare * 1.25) ? PerformanceStatus.Strong : PerformanceStatus.Weak;
                    }

                    var volumePerformance = PerformanceStatus.NotPresent;
                    if (brokerCommodityVolume > 0 && totalCommodityVolume > 0)
                    {
                        var commodityVolumeShare = (double)(brokerCommodityVolume / totalCommodityVolume);
                        volumePerformance = commodityVolumeShare > (overallVolumeShare * 1.25) ? PerformanceStatus.Strong : PerformanceStatus.Weak;
                    }

                    resultList.Add(new StrategicPerformanceItem
                    {
                        CommodityId = commodityId,
                        CommodityName = commodityName,
                        ValuePerformance = valuePerformance,
                        VolumePerformance = volumePerformance
                    });
                }

                return resultList;
            }, expirationInMinutes: 720);
        }

        #region Private Helpers
        private string FormatValue(decimal tradeValueInThousandRials)
        {
            var valueInRials = tradeValueInThousandRials * 1000;
            if (valueInRials >= 1_000_000_000_000) // HEMT
                return $"{valueInRials / 1_000_000_000_000m:F1} همت";
            if (valueInRials >= 1_000_000_000) // Billions
                return $"{valueInRials / 1_000_000_000m:N0} میلیارد تومان";
            if (valueInRials >= 1_000_000) // Millions
                return $"{valueInRials / 1_000_000m:N0} میلیون تومان";
            return $"{valueInRials:N0} ریال";
        }

        private string FormatVolume(decimal volumeInTons)
        {
            if (volumeInTons >= 1000)
                return $"{volumeInTons / 1000:N0} هزار تن";
            return $"{volumeInTons:N0} تن";
        }
        #endregion
    }
}