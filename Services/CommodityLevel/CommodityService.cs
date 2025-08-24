using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Models.Public;
using IME.SpotDataApi.Models.Spot;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;


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
                                         Commodity = new { commodity.Id, commodity.Symbol },
                                         SubGroup = new { subGroup.Id, subGroup.PersianName },
                                         Group = new { grp.Id, grp.PersianName },
                                         MainGroup = new { mainGroup.Id, mainGroup.PersianName }
                                     }).FirstOrDefaultAsync();

            if (queryResult != null)
            {
                hierarchy.Add(new HierarchyItem { Id = queryResult.MainGroup.Id, Type = "MainGroup", Name = queryResult.MainGroup.PersianName, IsActive = true });
                hierarchy.Add(new HierarchyItem { Id = queryResult.Group.Id, Type = "Group", Name = queryResult.Group.PersianName, IsActive = true });
                hierarchy.Add(new HierarchyItem { Id = queryResult.SubGroup.Id, Type = "SubGroup", Name = queryResult.SubGroup.PersianName, IsActive = true });
                hierarchy.Add(new HierarchyItem { Id = queryResult.Commodity.Id, Type = "Commodity", Name = queryResult.Commodity.Symbol, IsActive = false });
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
                                      .Take(10)
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

            var priceHistoryPoints = recentTrades
                .OrderBy(t => t.TradeDate)
                .Select(t => new PriceHistoryPoint
                {
                    DateLabel = t.TradeDate.Substring(5),
                    Price = t.FinalWeightedAveragePrice
                })
                .ToList();

            // Pad the list with empty items if there are fewer than 5 trades
            int requiredItems = 10;
            while (priceHistoryPoints.Count < requiredItems)
            {
                priceHistoryPoints.Insert(0, new PriceHistoryPoint { DateLabel = "", Price = 0 });
            }

            return new PriceViewModel
            {
                CurrentPrice = latestTrade.FinalWeightedAveragePrice,
                ChangeAmount = changeAmount,
                ChangePercentage = changePercentage,
                ChangeContext = previousTrade != null ? $"نسبت به عرضه {previousTrade.TradeDate}" : "اولین معامله",
                DateLabel = $"آخرین عرضه ({daysSinceLastTrade} روز پیش)",
                IsOutdated = daysSinceLastTrade > 7,
                PriceHistory = priceHistoryPoints,
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
            string description = percentage >= 95 ? "تقریباً تمام حجم عرضه‌شده به فروش رسیده و رقابت شدیدی در معاملات شکل گرفته است که نشان‌دهنده تقاضای بسیار قوی در بازار است."
                               : percentage > 75 ? "بخش قابل توجهی از حجم عرضه‌شده معامله شده است، اما رقابت در سطح متوسطی قرار داشته و بخشی از عرضه بدون مشتری باقی مانده است."
                               : "کمتر از نیمی از حجم عرضه‌شده به فروش رسیده که نشان‌دهنده تقاضای ضعیف و رکود در معاملات این کالا است.";

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
            var commonCommodityBuyMethod = await GetMostCommonValueAsync(commodityOffers, o => o.BuyMethodId, context.BuyMethods);
            var commonCommoditySettlement = await GetMostCommonValueAsync(commodityOffers, o => o.SettlementTypeId, context.SettlementTypes);
            var commonCommoditySecurityType = await GetMostCommonValueAsync(commodityOffers, o => o.SecurityTypeId, context.SecurityTypes);
            var commonCommodityContractType = await GetMostCommonValueAsync(commodityOffers, o => o.ContractTypeId, context.ContractTypes);
            var commonCommodityPackagingType = await GetMostCommonValueAsync(commodityOffers, o => o.PackagingTypeId, context.PackagingTypes);
            var commonCommodityDeliveryPlace = await GetMostCommonValueAsync(commodityOffers, o => o.DeliveryPlaceId, context.DeliveryPlaces);
            var commonCommodityMeasurementUnit = await GetMostCommonValueAsync(commodityOffers, o => o.MeasureUnitId, context.MeasurementUnits);
            var commonCommodityOfferMode = await GetMostCommonValueAsync(commodityOffers, o => o.OfferModeId, context.OfferModes);
            var commonCommodityOfferType = await GetMostCommonValueAsync(commodityOffers, o => o.OfferTypeId, context.OfferTypes);
            var commonCommodityCurrency = await GetMostCommonValueAsync(commodityOffers, o => o.CurrencyId, context.CurrencyUnits);
            var commonCommodityPrepayment = await GetMostCommonPrimitiveValueAsync(commodityOffers, o => o.PrepaymentPercent);
            var commonCommodityTickSize = await GetMostCommonPrimitiveValueAsync(commodityOffers, o => o.TickSize);
            var commonCommodityWeightFactor = await GetMostCommonPrimitiveValueAsync(commodityOffers, o => o.WeightFactor);


            // Fetch most common values for the entire market
            var commonMarketBuyMethod = await GetMostCommonValueAsync(allOffers, o => o.BuyMethodId, context.BuyMethods);
            var commonMarketSettlement = await GetMostCommonValueAsync(allOffers, o => o.SettlementTypeId, context.SettlementTypes);
            var commonMarketSecurityType = await GetMostCommonValueAsync(allOffers, o => o.SecurityTypeId, context.SecurityTypes);
            var commonMarketContractType = await GetMostCommonValueAsync(allOffers, o => o.ContractTypeId, context.ContractTypes);
            var commonMarketDeliveryPlace = await GetMostCommonValueAsync(allOffers, o => o.DeliveryPlaceId, context.DeliveryPlaces);
            var commonMarketPackagingType = await GetMostCommonValueAsync(allOffers, o => o.PackagingTypeId, context.PackagingTypes);
            var commonMarketMeasurementUnit = await GetMostCommonValueAsync(allOffers, o => o.MeasureUnitId, context.MeasurementUnits);
            var commonMarketOfferMode = await GetMostCommonValueAsync(allOffers, o => o.OfferModeId, context.OfferModes);
            var commonMarketOfferType = await GetMostCommonValueAsync(allOffers, o => o.OfferTypeId, context.OfferTypes);
            var commonMarketCurrency = await GetMostCommonValueAsync(allOffers, o => o.CurrencyId, context.CurrencyUnits);
            var commonMarketPrepayment = await GetMostCommonPrimitiveValueAsync(allOffers, o => o.PrepaymentPercent);
            var commonMarketTickSize = await GetMostCommonPrimitiveValueAsync(allOffers, o => o.TickSize);
            var commonMarketWeightFactor = await GetMostCommonPrimitiveValueAsync(allOffers, o => o.WeightFactor);

            // Get current values from the latest offer
            var currentBuyMethod = (await context.BuyMethods.FindAsync(latestOffer.BuyMethodId))?.PersianName ?? "نامشخص";
            var currentSettlementType = (await context.SettlementTypes.FindAsync(latestOffer.SettlementTypeId))?.PersianName ?? "نامشخص";
            var currentSecurityType = (await context.SecurityTypes.FindAsync(latestOffer.SecurityTypeId))?.PersianName ?? "نامشخص";
            var currentContractType = (await context.ContractTypes.FindAsync(latestOffer.ContractTypeId))?.PersianName ?? "نامشخص";
            var currentDeliveryPlace = (await context.DeliveryPlaces.FindAsync(latestOffer.DeliveryPlaceId))?.PersianName ?? "نامشخص";
            var currentPackagingType = (await context.PackagingTypes.FindAsync(latestOffer.PackagingTypeId))?.PersianName ?? "نامشخص";
            var currentMeasurementUnit = (await context.MeasurementUnits.FindAsync(latestOffer.MeasureUnitId))?.PersianName ?? "نامشخص";
            var currentOfferMode = (await context.OfferModes.FindAsync(latestOffer.OfferModeId))?.PersianName ?? "نامشخص";
            var currentOfferType = (await context.OfferTypes.FindAsync(latestOffer.OfferTypeId))?.PersianName ?? "نامشخص";
            var currentCurrency = (await context.CurrencyUnits.FindAsync(latestOffer.CurrencyId))?.PersianName ?? "نامشخص";
            
            var items = new List<CommodityAttributeItem>();
            
            // 4. Prepayment Percent
            items.Add(new CommodityAttributeItem {
                Title = "درصد پیش‌پرداخت", CurrentValue = $"{latestOffer.PrepaymentPercent}%", IconCssClass = "bi bi-percent", IconBgCssClass = "icon-bg-green",
                Interpretation = latestOffer.PrepaymentPercent switch
            {
                1 => " پایین‌ترین حد ممکن؛ نشان‌دهنده اعتماد کامل فروشنده به نقدشوندگی کالا و تلاش برای جذب حداکثری مشتریان با کمترین موانع است.",
                3 => "بسیار پایین؛ بیانگر ریسک معاملاتی ناچیز از دید فروشنده و تمایل او برای تسهیل شرایط خرید جهت افزایش حجم تقاضا است.",
                5 => "کمتر از عرف بازار؛ معمولاً برای کالاهای بسیار پرتقاضا و کم‌ریسک به کار می‌رود تا فرآیند خرید را برای فعالان بازار آسان‌تر کند.",
                6 => "کمی پایین‌تر از استاندارد؛ سیگنالی از اطمینان نسبی فروشنده به وضعیت بازار و تلاش برای ایجاد مزیت رقابتی کوچک برای خریداران است.",
                10 => "استاندارد و رایج؛ این درصد نیاز به نقدینگی عادی برای خریداران را نشان می‌دهد و متداول‌ترین رویه در بازار فیزیکی است.",
                20 => "بالاتر از عرف؛ نشان‌دهنده احتیاط فروشنده و نیاز به اطمینان بیشتر از توان مالی و تعهد خریدار برای تکمیل معامله است.",
                25 => "قابل توجه؛ بیانگر آن است که فروشنده به دنبال جذب خریداران با توان مالی بالا و جلوگیری از ورود متقاضیان ضعیف‌تر به رقابت است.",
                30 => "بالا؛ معمولاً در مورد کالاهایی با نوسانات قیمتی زیاد یا ریسک‌های خاص اعمال می‌شود تا تعهد خریداران جدی سنجیده شود.",
                40 => "بسیار بالا؛ این درصد به منزله یک فیلتر جدی برای ورود به معامله است و اغلب برای معاملات خاص یا کالاهای استراتژیک به کار می‌رود.",
                50 => "نیمی از ارزش معامله؛ نشان‌دهنده ریسک بالای معامله از دید فروشنده یا تمایل به تسویه سریع بخش قابل توجهی از مبلغ قرارداد است.",
                60 => "بسیار سنگین؛ بیانگر شرایط خاص بازار یا ریسک بالای نکول است و تنها خریداران با توان نقدینگی بسیار بالا قادر به شرکت در آن هستند.",
                100 => "پرداخت کامل (معامله نقدی)؛ نشان‌دهنده حذف کامل ریسک اعتباری برای فروشنده است و بیان می‌کند که تسویه باید به صورت آنی و کامل انجام شود.",
                _ => "این درصد، بیانگر سطح نقدینگی و تضمین مالی مورد نیاز از سوی فروشنده برای ورود به معامله است و خریداران باید توانایی تأمین آن را به عنوان شرط اولیه شرکت در رقابت داشته باشند."
            },
                IsAlert = latestOffer.PrepaymentPercent != commonCommodityPrepayment,
                CommodityMostCommonValue = $"{commonCommodityPrepayment}%", MarketMostCommonValue = $"{commonMarketPrepayment}%"
            });
            
            // 5. Measurement Unit
            items.Add(new CommodityAttributeItem {
                Title = "واحد اندازه‌گیری", CurrentValue = currentMeasurementUnit, IconCssClass = "bi bi-rulers", IconBgCssClass = "icon-bg-blue",
                Interpretation = currentMeasurementUnit switch
            {
                "تن" => "متداول‌ترین واحد وزنی برای کالاهای سنگین و فله‌ای مانند محصولات فولادی، معدنی و پتروشیمی.",
                "کیلوگرم" => "واحد وزنی استاندارد و رایج برای طیف وسیعی از کالاها در مقیاس‌های متوسط.",
                "گرم" => "واحد وزنی دقیق برای کالاهای سبک یا محصولاتی که با دقت بالا معامله می‌شوند.",
                "اونس" => "واحد وزنی دقیق برای فلزات گران‌بها مانند طلا؛ بیانگر معامله در مقیاس جهانی و با دقت بالا است.",
                "پاوند" => "واحد وزنی رایج در برخی استانداردهای بین‌المللی، معادل تقریباً ۴۵۳ گرم.",
                "بشکه" or "بشگه" => "واحد حجم استاندارد جهانی برای مایعات، به ویژه نفت خام و فرآورده‌های نفتی.",
                "لیتر" => "واحد حجم استاندارد برای مایعات که معمولاً در مقیاس‌های کوچک‌تر از بشکه استفاده می‌شود.",
                "متر مکعب" => "واحد حجم برای گازها، مایعات یا کالاهای فله‌ای که حجم آن‌ها اهمیت دارد.",
                "مترمربع" => "واحد مساحت برای کالاهایی مانند ورق فلزی، سنگ یا زمین.",
                "متر" => "واحد طول برای کالاهایی مانند پارچه، کابل یا لوله.",
                "عدد" => "واحد شمارش عمومی برای کالاهایی که به صورت تکی معامله می‌شوند، مانند خودرو یا تجهیزات.",
                "قطعه" => "واحد شمارش برای اجزا و قطعات صنعتی یا ساختمانی.",
                "حلقه" => "واحد شمارش برای کالاهایی مانند لاستیک یا کلاف؛ نشان‌دهنده معامله بر اساس تعداد حلقه‌ها است.",
                "اصله" => "واحد شمارش برای برخی کالاها مانند درختان؛ نشان‌دهنده معامله بر اساس تعداد است.",
                "باب" => "واحد شمارش برای املاک و مستغلات یا واحدهای مشخص؛ نشان‌دهنده معامله یک واحد کامل است.",
                "بخش" => "واحد شمارش برای دارایی‌های قابل تقسیم مانند سهام یا زمین؛ نشان‌دهنده معامله جزئی از یک کل است.",
                "دوز" => "واحد شمارش خاص برای کالاهای دارویی یا شیمیایی که به مقدار معین مصرف می‌شوند.",
                _ => $"واحد اندازه‌گیری این کالا '{currentMeasurementUnit}' است که مبنای محاسبات حجم و قیمت معامله قرار می‌گیرد."
            },
                IsAlert = currentMeasurementUnit != commonCommodityMeasurementUnit.Name,
                CommodityMostCommonValue = commonCommodityMeasurementUnit.Name, MarketMostCommonValue = commonMarketMeasurementUnit.Name
            });

            // 6. Tick Size
            items.Add(new CommodityAttributeItem {
                Title = "حداقل تغییر قیمت", CurrentValue = $"{latestOffer.TickSize.ToString("N0")} {currentCurrency}", IconCssClass = "bi bi-graph-up", IconBgCssClass = "icon-bg-purple",
                Interpretation = "هر سفارش خرید باید مضربی از این عدد بالاتر از قیمت پایه باشد. این مقدار بر پویایی رقابت در حراج تاثیرگذار است.",
                IsAlert = latestOffer.TickSize != commonCommodityTickSize,
                CommodityMostCommonValue = $"{commonCommodityTickSize.ToString("N0")} {commonCommodityCurrency}", MarketMostCommonValue = $"{commonMarketTickSize.ToString("N0")} {commonMarketCurrency}" 
            });

            // 7. Offer Mode
            items.Add(new CommodityAttributeItem
            {
                Title = "نحوه عرضه",
                CurrentValue = currentOfferMode,
                IconCssClass = "bi bi-megaphone-fill",
                IconBgCssClass = "icon-bg-orange",
                Interpretation = currentOfferMode switch
                {
                    "عمده" => "این کالا به صورت عمده و در حجم‌های بالا عرضه می‌شود و برای خریدارانی مناسب است که به دنبال تأمین در مقیاس بزرگ هستند.",
                    "خرد" => "عرضه به صورت خرد انجام می‌شود که به خریداران با نیازهای کمتر اجازه می‌دهد تا در حجم‌های کوچک‌تر اقدام به خرید کنند.",
                    "پریمیوم" => "این عرضه به روش پریمیوم انجام می‌شود، به این معنی که فروشنده انتظار دارد کالا را با قیمتی بالاتر از نرخ‌های پایه به فروش برساند.",
                    _ => "نحوه عرضه این کالا تابع شرایط عمومی بازار و جزئیات ذکر شده در اطلاعیه رسمی بورس کالا است."
                },
                IsAlert = currentOfferMode != commonCommodityOfferMode.Name,
                CommodityMostCommonValue = commonCommodityOfferMode.Name,
                MarketMostCommonValue = commonMarketOfferMode.Name
            });

            // 3. Delivery Place
            bool isDeliveryPlaceAlert = currentDeliveryPlace != commonCommodityDeliveryPlace.Name;
            items.Add(new CommodityAttributeItem
            {
                Title = "محل تحویل",
                CurrentValue = currentDeliveryPlace,
                IconCssClass = "bi bi-geo-alt-fill",
                IconBgCssClass = "icon-bg-red",
                Interpretation = currentDeliveryPlace switch
            {
                "انبار کارخانه" => "کالا در محل انبار کارخانه تحویل داده می‌شود و کلیه هزینه‌های حمل و لجستیک از درب کارخانه بر عهده خریدار است.",
                _ => $"محل تحویل کالا '{currentDeliveryPlace}' تعیین شده و خریداران باید هزینه‌ها و هماهنگی‌های لازم برای حمل از این مکان را در نظر بگیرند."
            },
                IsAlert = isDeliveryPlaceAlert,
                CommodityMostCommonValue = commonCommodityDeliveryPlace.Name,
                MarketMostCommonValue = commonMarketDeliveryPlace.Name
            });

            // 7. Currency Unit
            items.Add(new CommodityAttributeItem {
                Title = "ارز معامله", CurrentValue = currentCurrency, IconCssClass = "bi bi-currency-dollar", IconBgCssClass = "icon-bg-green",
                Interpretation = currentCurrency switch
                {
                    "ریال" => "مبنای قیمت‌گذاری و تسویه این معامله، ریال ایران است و برای بازار داخلی در نظر گرفته شده است.",
                    "دلار" => "قیمت پایه و تسویه نهایی این کالا بر اساس دلار آمریکا محاسبه می‌شود که معمولاً برای عرضه‌های صادراتی یا کالاهای مرتبط با بازارهای جهانی است.",
                    "درهم" => "مبنای مالی این معامله درهم امارات است و اغلب برای تسهیل تجارت با کشورهای حاشیه خلیج فارس استفاده می‌شود.",
                    "یورو" => "قیمت‌گذاری و تسویه این عرضه بر پایه یورو انجام می‌شود که نشان‌دهنده ارتباط آن با بازارهای اروپایی است.",
                    "یکصد ین" => "مبنای این معامله یکصد ین ژاپن است که نشان‌دهنده یک عرضه خاص برای بازارهای مرتبط با شرق آسیا است.",
                    "دلار آزاد" => "تسویه این معامله بر اساس نرخ دلار در بازار آزاد انجام خواهد شد که ریسک نوسانات ارزی را برای خریدار به همراه دارد.",
                    "یورو آزاد" => "تسویه این معامله بر اساس نرخ یورو در بازار آزاد محاسبه می‌شود و خریدار باید ریسک تغییرات نرخ ارز را بپذیرد.",
                    "ریال صادراتی" => "این عرضه بر پایه ریال اما مختص معاملات صادراتی است و تابع قوانین و مقررات ارزی مربوط به صادرات خواهد بود.",
                    _ => "ارز معامله مطابق با شرایط ذکر شده در اطلاعیه عرضه است و تسویه بر اساس آن صورت خواهد گرفت."
                },
                IsAlert = currentCurrency != commonCommodityCurrency.Name,
                CommodityMostCommonValue = commonCommodityCurrency.Name, MarketMostCommonValue = commonMarketCurrency.Name
            });

            // 1. Buy Method
            bool isBuyMethodAlert = currentBuyMethod != commonCommodityBuyMethod.Name;
            items.Add(new CommodityAttributeItem
            {
                Title = "روش خرید",
                CurrentValue = currentBuyMethod,
                IconCssClass = "bi bi-cart",
                IconBgCssClass = "icon-bg-orange",
                Interpretation = currentBuyMethod switch
                {
                    "عادی" => "روش عرضه عادی، نشان‌دهنده تعادل نسبی میان عرضه و تقاضا در بازار است و فعالان بازار انتظار دارند که معامله با رقابتی محدود و در محدوده‌ی قیمت پایه به سرانجام برسد.",
                    "خاص" => "عرضه به روش خاص مشخص می‌کند که کالا برای گروه محدودی از خریداران با صلاحیت‌های از پیش تعیین‌شده در نظر گرفته شده و هدف فروشنده، فراتر از یک فروش عمومی، تحقق اهداف استراتژیک یا قراردادی است.",
                    "بلند مدت" => "انتخاب این روش، از نگاه استراتژیک طرفین به معامله حکایت دارد و هدف آن، ایجاد یک رابطه تجاری پایدار به منظور تضمین تأمین و تقاضای کالا در بازه‌های زمانی آینده و کاهش نوسانات است.",
                    "پریمیوم" => "این روش نشان می‌دهد که فروشنده به پشتوانه کیفیت برتر محصول، شرایط مطلوب بازار یا ارائه خدمات ویژه، اطمینان دارد که می‌تواند کالا را با یک اضافه بهای مشخص (پریمیوم) بالاتر از نرخ‌های رایج به فروش برساند.",
                    "معامله نهایی پریمیوم" => "این عنوان بیانگر آن است که مذاکرات اصلی بین خریدار و فروشنده قبلاً صورت گرفته و طرفین بر سر یک قیمت نهایی به همراه اضافه بها به توافق رسیده‌اند؛ این عرضه صرفاً جهت شفاف‌سازی و ثبت رسمی آن توافق در ساختار بورس است.",
                    _ => "عرضه این کالا تابع قوانین کلی بورس و بر اساس جزئیات و شرایط خاصی است که در متن کامل اطلاعیه ذکر شده است."
                },
                CommodityMostCommonValue = commonCommodityBuyMethod.Name,
                IsAlert = isBuyMethodAlert,
                MarketMostCommonValue = commonMarketBuyMethod.Name
            });

            // 8. Weight Factor
            items.Add(new CommodityAttributeItem {
                Title = "ضریب وزنی",
                CurrentValue = $"{latestOffer.WeightFactor:N0} {currentMeasurementUnit}", IconCssClass = "bi bi-truck", IconBgCssClass = "icon-bg-blue",
                Interpretation = $"ضریب وزنی استاندارد برای این کالا {latestOffer.WeightFactor:N0} {currentMeasurementUnit} است و محاسبات بر اساس وزن خالص انجام می‌شود.",
                IsAlert = latestOffer.WeightFactor != commonCommodityWeightFactor,
                CommodityMostCommonValue = $"{commonCommodityWeightFactor:N0} {commonCommodityMeasurementUnit}", MarketMostCommonValue = $"{commonMarketWeightFactor:N0} {commonMarketMeasurementUnit}"
            });

            // 9. Offer Type
            items.Add(new CommodityAttributeItem
            {
                Title = "نوع عرضه",
                CurrentValue = currentOfferType,
                IconCssClass = "bi bi-file-earmark-text-fill",
                IconBgCssClass = "icon-bg-purple",
                Interpretation = currentOfferType switch
                {
                    "عرضه داخلی" => "این عرضه مخصوص بازار داخل کشور است و تنها خریداران داخلی مجاز به شرکت در آن هستند.",
                    "عرضه صادراتی" => "این کالا منحصراً جهت صادرات عرضه شده و خریداران باید دارای شرایط و مجوزهای لازم برای صادرات باشند.",
                    "مچینگ داخلی" => "معامله به صورت مچینگ داخلی انجام می‌شود؛ یعنی خریدار و فروشنده پس از توافق اولیه، معامله خود را جهت ثبت نهایی در بورس کالا عرضه می‌کنند.",
                    "مچینگ صادراتی" => "این عرضه، ثبت رسمی یک توافق صادراتی از پیش تعیین شده بین خریدار و فروشنده در سامانه بورس کالا است.",
                    _ => "این کالا برای تمام فعالان واجد شرایط بازار و بر اساس قوانین عمومی بورس کالا عرضه می‌شود."
                },
                CommodityMostCommonValue = commonCommodityOfferType.Name,
                IsAlert = currentOfferType != commonCommodityOfferType.Name,
                MarketMostCommonValue = commonMarketOfferType.Name
            });

            // 2. Settlement Type
            bool isSettlementAlert = currentSettlementType != commonCommoditySettlement.Name;
            items.Add(new CommodityAttributeItem
            {
                Title = "نوع تسویه",
                CurrentValue = currentSettlementType,
                IconCssClass = "bi bi-credit-card-fill",
                IconBgCssClass = "icon-bg-blue",
                Interpretation = currentSettlementType switch
                {
                    "نقدی" => "تسویه نقدی نشان‌دهنده نقدشوندگی بالا و سلامت مالی خریداران در این بازار است و ریسک اعتباری را برای فروشنده به صفر می‌رساند.",
                    "اعتباری" => "تسویه اعتباری بیانگر انعطاف‌پذیری فروشنده و تلاش او برای جذب مشتریان بیشتر از طریق فراهم کردن فرصت پرداخت در آینده است.",
                    "نقدی / اعتباری" => "تسویه نقدی / اعتباری یک راهکار ترکیبی است که به خریدار امکان می‌دهد بخشی از مبلغ را فوراً پرداخت کرده و برای بخش دیگر از اعتبار استفاده نماید، که این امر قدرت خرید را افزایش می‌دهد.",
                    _ => "روش تسویه این معامله تابع شرایط اعلام‌شده از سوی فروشنده است و خریداران موظفند بر اساس جزئیات مندرج در اطلاعیه عرضه، برای تکمیل فرآیند پرداخت اقدام کنند."
                },
                IsAlert = isSettlementAlert,
                CommodityMostCommonValue = commonCommoditySettlement.Name,
                MarketMostCommonValue = commonMarketSettlement.Name
            });

            // 10. Settlement Type
            items.Add(new CommodityAttributeItem
            {
                Title = "نوع تضمین",
                CurrentValue = currentSecurityType,
                IconCssClass = "bi bi-shield-lock-fill",
                IconBgCssClass = "icon-bg-red",
                Interpretation = currentSecurityType switch
                {
                    "نگهداری وجه" => "در این معامله، وجه تضمین به صورت کامل نزد کارگزار یا اتاق پایاپای نگهداری می‌شود تا از اجرای تعهدات طرفین اطمینان حاصل شود.",
                    "با تعهد کامل کارگزار" => "کارگزار خریدار، مسئولیت کامل پرداخت و تسویه نهایی معامله را بر عهده گرفته و به عنوان ضامن اصلی عمل می‌کند.",
                    _ => "مشارکت در این معامله مستلزم ارائه تضمین‌های لازم جهت حصول اطمینان از اجرای تعهدات است که جزئیات دقیق و شرایط آن در اسناد عرضه قید شده است."
                },
                IsAlert = currentSecurityType != commonCommoditySecurityType.Name,
                CommodityMostCommonValue = commonCommoditySecurityType.Name,
                MarketMostCommonValue = commonMarketSecurityType.Name
            });

            // 11. Contract Type
            items.Add(new CommodityAttributeItem
            {
                Title = "نوع قرارداد",
                CurrentValue = currentContractType,
                IconCssClass = "bi bi-file-text-fill",
                IconBgCssClass = "icon-bg-blue",
                Interpretation = currentContractType switch
                {
                    "نقدی" => "قرارداد نقدی: تسویه کامل معامله یعنی پرداخت وجه و تحویل کالا، باید در مدت زمان کوتاهی پس از انجام معامله صورت پذیرد.",
                    "سلف" => "قرارداد سلف: خریدار وجه را به صورت نقدی پرداخت می‌کند اما کالا را در تاریخ مشخصی در آینده تحویل می‌گیرد.",
                    "نسیه" => "قرارداد نسیه: خریدار کالا را تحویل گرفته و وجه آن را در تاریخ مشخصی در آینده پرداخت می‌کند.",
                    "نقدی (مچینگ)" => "ثبت یک معامله نقدی که خریدار و فروشنده از قبل بر سر آن توافق کرده‌اند.",
                    "سلف (مچینگ)" => "ثبت یک معامله سلف که طرفین از قبل بر سر شرایط آن به توافق رسیده‌اند.",
                    "نسیه (مچینگ)" => "ثبت یک معامله نسیه که جزئیات آن قبلاً بین خریدار و فروشنده نهایی شده است.",
                    "نقدی ($)" or "نقدی (€)" or "نقدی (درهم)" or "نقدی (¥100)" => $"قرارداد نقدی ارزی: تسویه به صورت نقدی و بر اساس ارز ({currentContractType.Split(' ')[1]}) انجام می‌شود.",
                    "نقدی ($) آزاد" => "قرارداد نقدی بر پایه دلار که تسویه آن بر اساس نرخ بازار آزاد محاسبه خواهد شد.",
                    "سلف استاندارد" => "قرارداد سلف با مشخصات و قوانین استاندارد شده بورس کالا که قابلیت معامله ثانویه را نیز دارد.",
                    "پریمیوم" => "قرارداد پریمیوم: معامله‌ای که در آن یک اضافه بها نسبت به قیمت پایه یا قیمت مرجع پرداخت می‌شود.",
                    "-----" => "نوع قرارداد این عرضه مشخص نشده و تابع شرایط عمومی اعلامی است.",
                    _ => $"این معامله در قالب قرارداد '{currentContractType}' انجام می‌شود و خریداران باید با شرایط و قوانین این نوع قرارداد آشنا باشند."
                },
                IsAlert = currentContractType != commonCommodityContractType.Name,
                CommodityMostCommonValue = commonCommodityContractType.Name,
                MarketMostCommonValue = commonMarketContractType.Name
            });

            // 12. Settlement Type
            items.Add(new CommodityAttributeItem
            {
                Title = "نوع بسته‌بندی",
                CurrentValue = currentPackagingType,
                IconCssClass = "bi bi-box-seam",
                IconBgCssClass = "icon-bg-purple",
                Interpretation = currentPackagingType switch
                {
                    "فله" => "کالا بدون هیچ‌گونه بسته‌بندی و به صورت فله عرضه و تحویل داده می‌شود.",
                    "جامبوبگ" => "بسته‌بندی در کیسه‌های بزرگ (جامبوبگ) که برای حمل و نگهداری مواد فله‌ای و پودری مناسب است.",
                    "کیسه" => "کالا در کیسه‌هایی با اندازه‌های استاندارد بسته‌بندی شده است.",
                    "بشکه" => "بسته‌بندی استاندارد برای مایعات، مواد شیمیایی یا فرآورده‌های نفتی.",
                    "پالت چوبی" or "پالت فلزی" or "پالت پلاستیکی" => "کالا بر روی پالت قرار گرفته تا حمل و جابجایی آن با لیفتراک تسهیل شود.",
                    "کلاف" => "محصول به صورت کلاف پیچیده شده که برای کالاهایی مانند مفتول، میلگرد یا ورق‌های نازک رایج است.",
                    "شمش" => "کالا به صورت شمش‌های استاندارد جهت تسهیل در شمارش، حمل و ذوب مجدد عرضه می‌شود.",
                    "رول" => "بسته‌بندی به صورت رول که برای کالاهایی مانند ورق فلزی، کاغذ یا پارچه کاربرد دارد.",
                    "شاخه" => "کالا به صورت شاخه‌هایی با طول مشخص عرضه می‌شود، مانند میلگرد یا پروفیل‌های ساختمانی.",
                    "عدل" => "بسته‌بندی به صورت عدل (فشرده‌سازی و بستن) که برای کالاهایی مانند پنبه یا الیاف استفاده می‌شود.",
                    "کارتن" => "کالا در بسته‌بندی کارتنی استاندارد عرضه می‌شود.",
                    "کپسول 60" or "کپسول 90" or "کپسول 120" => "کالا در کپسول‌های تحت فشار با حجم مشخص عرضه می‌شود که مخصوص گازها است.",
                    "بیتوپلاست" or "بیتوپک" => "بسته‌بندی پلیمری مخصوص قیر که حمل و نگهداری آن را آسان‌تر می‌کند.",
                    "سایر" => "نوع بسته‌بندی این کالا خاص بوده و جزئیات آن در اسناد عرضه ذکر شده است.",
                    _ => $"کالا با بسته‌بندی از نوع '{currentPackagingType}' عرضه می‌شود که شرایط آن باید توسط خریدار بررسی شود."
                },
                IsAlert = currentPackagingType != commonCommodityPackagingType.Name,
                CommodityMostCommonValue = commonCommodityPackagingType.Name,
                MarketMostCommonValue = commonMarketPackagingType.Name
            });


            return new CommodityAttributesData { Items = items };
        }

            // Helper method to find the most common value in a given query
        private async Task<(int Id, string Name)> GetMostCommonValueAsync<TEntity>(IQueryable<Offer> query, System.Linq.Expressions.Expression<Func<Offer, int>> keySelector, DbSet<TEntity> dbSet) where TEntity : BaseInfo
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
        private async Task<T> GetMostCommonPrimitiveValueAsync<T>(IQueryable<Offer> query, System.Linq.Expressions.Expression<Func<Offer, T>> keySelector) where T : struct
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

            // کوئری پایه برای واکشی تمام عرضه‌های کالا در ۳۰ روز گذشته
            var offersDataQuery = from o in context.Offers
                                  where o.CommodityId == commodityId && string.Compare(o.OfferDate, thirtyDaysAgo) >= 0
                                  join s in context.Suppliers on o.SupplierId equals s.Id
                                  join b in context.Brokers on o.BrokerId equals b.Id
                                  select new
                                  {
                                      o.OfferVol,
                                      SupplierName = s.PersianName,
                                      BrokerName = b.PersianName,
                                      o.SupplierId,
                                      o.BrokerId
                                  };

            var offersData = await offersDataQuery.ToListAsync();

            if (!offersData.Any()) return Enumerable.Empty<MainPlayer>();

            // 1. شناسایی برترین عرضه‌کننده بر اساس مجموع حجم عرضه
            var totalVolume = offersData.Sum(o => o.OfferVol);
            var topSupplier = offersData
                .GroupBy(o => (o.SupplierName, o.SupplierId))
                .Select(g => new {Id = g.Key.SupplierId , Name = g.Key.SupplierName, Volume = g.Sum(o => o.OfferVol) })
                .OrderByDescending(x => x.Volume)
                .FirstOrDefault();

            // 2. شناسایی برترین کارگزار بر اساس تعداد عرضه
            var totalOfferCount = offersData.Count;
            var topBroker = offersData
                .GroupBy(o => (o.BrokerName, o.BrokerId))
                .Select(g => new {Id = g.Key.BrokerId, Name = g.Key.BrokerName, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            var players = new List<MainPlayer>();
            if (topSupplier != null && totalVolume > 0)
            {
                players.Add(new MainPlayer
                {
                    Type = MainPlayerType.Supplier,
                    Id = topSupplier.Id,
                    Name = topSupplier.Name,
                    IconCssClass = "bi bi-buildings-fill",
                    MarketShare = (decimal)(topSupplier.Volume / totalVolume) * 100
                });
            }
            if (topBroker != null && totalOfferCount > 0)
            {
                players.Add(new MainPlayer
                {
                    Type = MainPlayerType.Broker,
                    Id = topBroker.Id,
                    Name = topBroker.Name,
                    IconCssClass = "bi bi-person-workspace",
                    MarketShare = ((decimal)topBroker.Count / totalOfferCount) * 100
                });
            }

            return players;
        }
        private readonly List<string> _colorPalette = new List<string>
        {
            "color-blue", "color-green", "color-orange", "color-purple", "color-red", "color-teal"
        };
        public async Task<DistributedAttributesData> GetDistributedAttributesAsync(int commodityId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var thirtyDaysAgo = _dateHelper.GetPersian(DateTime.Now.AddDays(-30));
            var items = new List<DistributedAttributeItem>();

            // --- 1. نوع قرارداد ---
            var contractTrades = await (from t in context.TradeReports.Where(t => t.CommodityId == commodityId && string.Compare(t.TradeDate, thirtyDaysAgo) >= 0)
                                        join ct in context.ContractTypes on t.ContractTypeId equals ct.Id
                                        select new { t.TradeVolume, ContractTypeName = ct.PersianName })
                                        .ToListAsync();
            if (contractTrades.Any())
            {
                var totalVolume = contractTrades.Sum(t => t.TradeVolume);
                var distribution = contractTrades
                    .GroupBy(t => t.ContractTypeName)
                    .Select(g => new { Name = g.Key, Volume = g.Sum(t => t.TradeVolume) })
                    .OrderByDescending(x => x.Volume)
                    .ToList() // ابتدا لیست را ایجاد می‌کنیم تا بتوانیم از ایندکس استفاده کنیم
                    .Select((g, index) => new AttributeValueShare
                    {
                        Name = g.Name,
                        Percentage = totalVolume > 0 ? (double)(g.Volume / totalVolume) * 100 : 0,
                        // اختصاص رنگ به صورت چرخشی از پالت
                        ColorCssClass = _colorPalette[index % _colorPalette.Count]
                    })
                    .ToList();

                var dominantValue = distribution.FirstOrDefault()?.Name ?? "نامشخص";
                var marketMostCommon = await GetMostCommonValueAsync(context.Offers.Where(o => o.CommodityId == commodityId), o => o.ContractTypeId, context.ContractTypes);
                if(distribution.Count > 1)
                    items.Add(new DistributedAttributeItem
                    {
                        Title = "نوع قرارداد",
                        ValueDistribution = distribution,
                        IconCssClass = "bi bi-file-text-fill",
                        IconBgCssClass = "icon-bg-blue",
                        Interpretation = $"تسلط {dominantValue} نشان‌دهنده گرایش اصلی بازار این کالا است.",
                        IsAlert = dominantValue != marketMostCommon.Name,
                        DominantValue = dominantValue,
                        MarketMostCommonValue = marketMostCommon.Name
                    });
            }

            // --- 2. نوع تسویه ---
            var settlementOffers = await (from o in context.Offers.Where(o => o.CommodityId == commodityId && string.Compare(o.OfferDate, thirtyDaysAgo) >= 0)
                                          join st in context.SettlementTypes on o.SettlementTypeId equals st.Id
                                          select new { SettlementTypeName = st.PersianName })
                                          .ToListAsync();
            if (settlementOffers.Any())
            {
                var totalOffers = settlementOffers.Count;
                var distribution = settlementOffers
                    .GroupBy(o => o.SettlementTypeName)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList()
                    .Select((g, index) => new AttributeValueShare
                    {
                        Name = g.Name,
                        Percentage = ((double)g.Count / totalOffers) * 100,
                        ColorCssClass = _colorPalette[index % _colorPalette.Count]
                    })
                    .ToList();

                var dominantValue = distribution.FirstOrDefault()?.Name ?? "نامشخص";
                var marketMostCommon = await GetMostCommonValueAsync(context.Offers.Where(o => o.CommodityId == commodityId), o => o.SettlementTypeId, context.SettlementTypes);

                if (distribution.Count > 1)
                    items.Add(new DistributedAttributeItem
                    {
                        Title = "نوع تسویه",
                        ValueDistribution = distribution,
                        IconCssClass = "bi bi-credit-card-fill",
                        IconBgCssClass = "icon-bg-orange",
                        Interpretation = $"غالب بودن تسویه {dominantValue} می‌تواند بر نیاز به نقدینگی خریداران تاثیرگذار باشد.",
                        IsAlert = dominantValue != marketMostCommon.Name,
                        DominantValue = dominantValue,
                        MarketMostCommonValue = marketMostCommon.Name
                    });
            }

            // --- 3. نوع بسته‌بندی ---
            var packagingOffers = await (from o in context.Offers.Where(o => o.CommodityId == commodityId && string.Compare(o.OfferDate, thirtyDaysAgo) >= 0)
                                         join pt in context.PackagingTypes on o.PackagingTypeId equals pt.Id
                                         where pt.Id != 0 // با فرض اینکه 0 به معنی نامشخص است
                                         select new { PackagingTypeName = pt.PersianName })
                                         .ToListAsync();
            if (packagingOffers.Any())
            {
                var totalOffers = packagingOffers.Count;
                var distribution = packagingOffers
                    .GroupBy(o => o.PackagingTypeName)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList()
                    .Select((g, index) => new AttributeValueShare
                    {
                        Name = g.Name,
                        Percentage = ((double)g.Count / totalOffers) * 100,
                        ColorCssClass = _colorPalette[index % _colorPalette.Count]
                    })
                    .ToList();

                var dominantValue = distribution.FirstOrDefault()?.Name ?? "نامشخص";
                var marketMostCommon = await GetMostCommonValueAsync(context.Offers.Where(o => o.CommodityId == commodityId && o.PackagingTypeId != 0), o => o.PackagingTypeId, context.PackagingTypes);

                if (distribution.Count > 1)
                    items.Add(new DistributedAttributeItem
                    {
                        Title = "نوع بسته‌بندی",
                        ValueDistribution = distribution,
                        IconCssClass = "bi bi-box-seam",
                        IconBgCssClass = "icon-bg-purple",
                        Interpretation = $"غالب بودن بسته‌بندی {dominantValue} نشان می‌دهد که خریداران اصلی چه گروهی هستند (صنعتی یا خرد).",
                        IsAlert = dominantValue != marketMostCommon.Name,
                        DominantValue = dominantValue,
                        MarketMostCommonValue = marketMostCommon.Name
                    });
            }

            return new DistributedAttributesData { Items = items };
        }

        /// <summary>
        /// تاریخچه عرضه‌های یک کالا را برای گذشته، امروز و آینده واکشی می‌کند.
        /// </summary>
        /// <param name="commodityId">شناسه کالا</param>
        /// <returns>لیستی از اطلاعیه‌های عرضه در سه دسته زمانی</returns>
        public async Task<UpcomingOffersData> GetOfferHistoryAsync(int commodityId)
        {
            // یک context واحد برای کل عملیات ایجاد می‌شود
            using var context = await _contextFactory.CreateDbContextAsync();
            var todayPersian = _dateHelper.GetPersian(DateTime.Now);

            // --- 1. واکشی عرضه‌های آینده ---
            var futureOffersQuery = context.Offers
                .Where(o => o.CommodityId == commodityId && string.Compare(o.OfferDate, todayPersian) > 0)
                .OrderBy(o => o.OfferDate);

            var futureItems = await ProcessOffersQuery(futureOffersQuery, OfferDateType.Future, context);

            // --- 2. واکشی عرضه‌های امروز ---
            var todayOffersQuery = context.Offers
                .Where(o => o.CommodityId == commodityId && o.OfferDate == todayPersian);

            var todayItems = await ProcessOffersQuery(todayOffersQuery, OfferDateType.Today, context);

            // --- 3. واکشی عرضه‌های گذشته ---
            var pastOffersQuery = context.Offers
                .Where(o => o.CommodityId == commodityId && string.Compare(o.OfferDate, todayPersian) < 0)
                .OrderByDescending(o => o.OfferDate)
                .Take(15);

            var pastItems = await ProcessOffersQuery(pastOffersQuery, OfferDateType.Past, context);

            // --- 4. ترکیب نتایج ---
            var allItems = todayItems.Concat(futureItems).Concat(pastItems).ToList();

            return new UpcomingOffersData { Items = allItems };
        }

        /// <summary>
        /// یک کوئری از عرضه‌ها را پردازش کرده و به لیست UpcomingOfferItem تبدیل می‌کند.
        /// </summary>
        private async Task<List<UpcomingOfferItem>> ProcessOffersQuery(IQueryable<Models.Spot.Offer> query, OfferDateType dateType, AppDataContext context)
        {
            // از context پاس داده شده استفاده می‌کند و context جدیدی نمی‌سازد
            var pc = new PersianCalendar();

            var offerData = await query
                .Join(context.Suppliers, o => o.SupplierId, s => s.Id, (o, s) => new { Offer = o, Supplier = s })
                .Join(context.Commodities, j => j.Offer.CommodityId, c => c.Id, (j, c) => new
                {
                    j.Offer.Id,
                    j.Offer.OfferDate,
                    CommodityName = c.PersianName,
                    SupplierName = j.Supplier.PersianName,
                    UrlName = c.Symbol // Or another suitable field for URL
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

        /// <summary>
        /// توزیع سهم بازار بین عرضه‌کنندگان و کارگزاران یک کالا را تحلیل می‌کند.
        /// </summary>
        /// <param name="commodityId">شناسه کالا</param>
        /// <returns>داده‌های توزیع بازیگران اصلی بازار</returns>
        public async Task<DistributedAttributesData> GetPlayerDistributionAsync(int commodityId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var thirtyDaysAgo = _dateHelper.GetPersian(DateTime.Now.AddDays(-30));
            var items = new List<DistributedAttributeItem>();

            // --- واکشی داده‌های پایه ---
            var offersInPeriod = await context.Offers
                .Where(o => o.CommodityId == commodityId && string.Compare(o.OfferDate, thirtyDaysAgo) >= 0)
                .Join(context.Suppliers, o => o.SupplierId, s => s.Id, (o, s) => new { Offer = o, SupplierName = s.PersianName })
                .Join(context.Brokers, j => j.Offer.BrokerId, b => b.Id, (j, b) => new
                {
                    j.Offer.OfferDate,
                    j.Offer.OfferVol,
                    j.SupplierName,
                    BrokerName = b.PersianName
                })
                .ToListAsync();

            if (!offersInPeriod.Any())
            {
                return new DistributedAttributesData();
            }

            // یافتن بازیگران آخرین عرضه
            var latestOffer = offersInPeriod.OrderByDescending(o => o.OfferDate).First();
            var latestSupplier = latestOffer.SupplierName;
            var latestBroker = latestOffer.BrokerName;

            // --- 1. تحلیل عرضه‌کنندگان (بر اساس حجم) ---
            var totalVolume = offersInPeriod.Sum(o => o.OfferVol);
            if (totalVolume > 0)
            {
                var supplierDistribution = offersInPeriod
                    .GroupBy(o => o.SupplierName)
                    .Select(g => new { Name = g.Key, Volume = g.Sum(o => o.OfferVol) })
                    .OrderByDescending(x => x.Volume)
                    .ToList()
                    .Select((g, index) => new AttributeValueShare
                    {
                        Name = g.Name,
                        Percentage = (double)(g.Volume / totalVolume) * 100,
                        ColorCssClass = _colorPalette[index % _colorPalette.Count]
                    })
                    .ToList();

                var dominantSupplier = supplierDistribution.FirstOrDefault()?.Name ?? "نامشخص";
                items.Add(new DistributedAttributeItem
                {
                    Title = "عرضه‌کنندگان",
                    ValueDistribution = supplierDistribution,
                    IconCssClass = "bi bi-buildings-fill",
                    IconBgCssClass = "icon-bg-green",
                    Interpretation = $"تسلط «{dominantSupplier}» نشان‌دهنده قدرت اصلی سمت عرضه در این بازار است.",
                    IsAlert = false,
                    DominantValue = dominantSupplier,
                    MarketMostCommonValue = latestSupplier // In this context, it's the latest value
                });
            }

            // --- 2. تحلیل کارگزاران (بر اساس تعداد) ---
            var totalOfferCount = offersInPeriod.Count;
            if (totalOfferCount > 0)
            {
                var brokerDistribution = offersInPeriod
                    .GroupBy(o => o.BrokerName)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList()
                    .Select((g, index) => new AttributeValueShare
                    {
                        Name = g.Name,
                        Percentage = ((double)g.Count / totalOfferCount) * 100,
                        ColorCssClass = _colorPalette[index % _colorPalette.Count]
                    })
                    .ToList();

                var dominantBroker = brokerDistribution.FirstOrDefault()?.Name ?? "نامشخص";
                items.Add(new DistributedAttributeItem
                {
                    Title = "کارگزاران عرضه",
                    ValueDistribution = brokerDistribution,
                    IconCssClass = "bi bi-person-workspace",
                    IconBgCssClass = "icon-bg-teal",
                    Interpretation = $"فعالیت بالای «{dominantBroker}» نشان‌دهنده تمرکز عرضه‌ها نزد این کارگزار است.",
                    IsAlert = false,
                    DominantValue = dominantBroker,
                    MarketMostCommonValue = latestBroker // In this context, it's the latest value
                });
            }

            return new DistributedAttributesData { Items = items };
        }
    }
}
