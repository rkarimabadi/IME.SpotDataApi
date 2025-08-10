using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Notification;
using IME.SpotDataApi.Models.Public;
using IME.SpotDataApi.Models.Spot;

namespace IME.SpotDataApi.Services.Data
{
    public class DataSyncService : BackgroundService
    {
        private const string version = "v2";
        private readonly ILogger<DataSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;
        // کل چرخه همگام‌سازی هر یک ساعت یکبار اجرا می‌شود تا محدودیت نرخ رعایت شود
        private readonly TimeSpan _cycleDelay = TimeSpan.FromHours(1);

        public DataSyncService(ILogger<DataSyncService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("سرویس همگام‌سازی داده‌ها (نسخه موازی) شروع به کار کرد.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("شروع چرخه جدید همگام‌سازی در: {time}", DateTimeOffset.Now);

                try
                {
                    await SyncBasicInformation<Broker>($"/{version}/spot/Brokers", stoppingToken);
                    await SyncBasicInformation<BuyMethod>($"/{version}/spot/BuyMethods", stoppingToken);
                    await SyncBasicInformation<Commodity>($"/{version}/spot/Commodities", stoppingToken);
                    await SyncBasicInformation<Group>($"/{version}/spot/Groups", stoppingToken);
                    await SyncBasicInformation<SubGroup>($"/{version}/spot/SubGroups", stoppingToken);
                    await SyncBasicInformation<MainGroup>($"/{version}/spot/MainGroups", stoppingToken);
                    await SyncBasicInformation<OfferMode>($"/{version}/spot/OfferModes", stoppingToken);
                    await SyncBasicInformation<PackagingType>($"/{version}/spot/PackagingTypes", stoppingToken);
                    await SyncBasicInformation<SettlementType>($"/{version}/spot/SettlementTypes", stoppingToken);
                    await SyncBasicInformation<SecurityType>($"/{version}/spot/SecurityTypes", stoppingToken);
                    await SyncBasicInformation<OfferType>($"/{version}/spot/OfferTypes", stoppingToken);
                    await SyncBasicInformation<Manufacturer>($"/{version}/spot/Manufacturers", stoppingToken);
                    await SyncBasicInformation<Supplier>($"/{version}/spot/Suppliers", stoppingToken);
                    await SyncBasicInformation<MeasurementUnit>($"/{version}/spot/MeasurementUnits", stoppingToken);
                    await SyncBasicInformation<CurrencyUnit>($"/{version}/spot/CurrencyUnits", stoppingToken);
                    await SyncBasicInformation<ContractType>($"/{version}/spot/ContractTypes", stoppingToken);
                    await SyncBasicInformation<DeliveryPlace>($"/{version}/spot/DeliveryPlaces", stoppingToken);
                    await SyncBasicInformation<TradingHall>($"/{version}/spot/TradingHalls", stoppingToken);
                    await SyncBasicInformation<Tender>($"/{version}/spot/Tenders", stoppingToken);


                    await SyncOperationalResource<Offer>($"/{version}/spot/Offers", DateTime.Now.AddDays(-19), DateTime.Now, stoppingToken);
                    await SyncOperationalResource<TradeReport>($"/{version}/spot/reports/Trades", DateTime.Now.AddDays(-5), DateTime.Now, stoppingToken);
                    await SyncOperationalResource<NewsNotification>("api/Notifications/NewsNotificationsByDate", DateTime.Now.AddYears(-1), DateTime.Now, stoppingToken);
                    await SyncOperationalResource<SpotNotification>("/api/Notifications/SpotNotificationsByDate", DateTime.Now.AddYears(-1), DateTime.Now, stoppingToken);


                    _logger.LogInformation("چرخه همگام‌سازی موازی با موفقیت به پایان رسید.");
                }
                catch (Exception ex)
                {
                    // Task.WhenAll اولین خطایی که رخ دهد را برمی‌گرداند
                    _logger.LogError(ex, "خطا در حین اجرای چرخه همگام‌سازی موازی.");
                }

                // انتظار برای شروع چرخه بعدی
                _logger.LogInformation("در حال انتظار برای شروع چرخه بعدی همگام‌سازی...");
                await Task.Delay(_cycleDelay, stoppingToken);
            }

            _logger.LogInformation("سرویس همگام‌سازی داده‌ها متوقف شد.");
        }

        private async Task SyncBasicInformation<T>(string endpointPath, CancellationToken stoppingToken) where T : RootObj<int>
        {
            if (stoppingToken.IsCancellationRequested) return;

            _logger.LogInformation("شروع همگام‌سازی موازی برای: {EntityName}", typeof(T).Name);
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var remoteService = scope.ServiceProvider.GetRequiredService<IRemotePublicResurceService<T>>();
                var repository = scope.ServiceProvider.GetRequiredService<IDataRepository<T>>();

                remoteService.EndPointPath = endpointPath;
                var items = await remoteService.RetrieveResurcesDataAsync();
                await repository.UpsertAsync(items);

                _logger.LogInformation("همگام‌سازی موازی برای {EntityName} با موفقیت انجام شد. تعداد رکوردها: {Count}", typeof(T).Name, items.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در هنگام همگام‌سازی {EntityName}", typeof(T).Name);
                // خطا را دوباره پرتاب می‌کنیم تا Task.WhenAll آن را دریافت کند
                throw;
            }
        }

        private async Task SyncOperationalResource<T>(string endpointPath, DateTime fromDate, DateTime toDate, CancellationToken stoppingToken) where T : RootObj<int>
        {
            if (stoppingToken.IsCancellationRequested) return;

            _logger.LogInformation("شروع همگام‌سازی موازی برای: {EntityName}", typeof(T).Name);
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var remoteService = scope.ServiceProvider.GetRequiredService<IRemoreOperationalResurceService<T>>();
                var repository = scope.ServiceProvider.GetRequiredService<IDataRepository<T>>();

                remoteService.EndPointPath = endpointPath;
                var items = await remoteService.RetrieveAsync(fromDate, toDate);
                await repository.UpsertAsync(items);

                _logger.LogInformation("همگام‌سازی موازی برای {EntityName} با موفقیت انجام شد. تعداد رکوردها: {Count}", typeof(T).Name, items.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در هنگام همگام‌سازی {EntityName}", typeof(T).Name);
                // خطا را دوباره پرتاب می‌کنیم تا Task.WhenAll آن را دریافت کند
                throw;
            }
        }
    }
}
