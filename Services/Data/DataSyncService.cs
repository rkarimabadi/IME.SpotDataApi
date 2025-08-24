using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Configuration;
using IME.SpotDataApi.Models.Notification;
using IME.SpotDataApi.Models.Public;
using IME.SpotDataApi.Models.Spot;
using Microsoft.Extensions.Options;

namespace IME.SpotDataApi.Services.Data
{
    public class DataSyncService : BackgroundService
    {
        private const string version = "v2";
        private readonly ILogger<DataSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DataSyncSettings _settings;
        private readonly TimeSpan _cycleDelay;

        public DataSyncService(ILogger<DataSyncService> logger, IServiceProvider serviceProvider, IOptions<DataSyncSettings> settings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _settings = settings.Value; // نگهداری تنظیمات
            _cycleDelay = TimeSpan.FromMinutes(_settings.CycleDelayMinutes); // خواندن از تنظیمات
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("سرویس همگام‌سازی داده‌ها (نسخه موازی و قابل تنظیم) شروع به کار کرد.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("شروع چرخه جدید همگام‌سازی در: {time}", DateTimeOffset.Now);

                try
                {
                    // فراخوانی‌ها به متدهای Helper که تنظیمات را چک می‌کنند، تغییر یافت
                    await TrySyncBasicAsync<Broker>($"/{version}/spot/Brokers", stoppingToken);
                    await TrySyncBasicAsync<BuyMethod>($"/{version}/spot/BuyMethods", stoppingToken);
                    await TrySyncBasicAsync<Commodity>($"/{version}/spot/Commodities", stoppingToken);
                    await TrySyncBasicAsync<Group>($"/{version}/spot/Groups", stoppingToken);
                    await TrySyncBasicAsync<SubGroup>($"/{version}/spot/SubGroups", stoppingToken);
                    await TrySyncBasicAsync<MainGroup>($"/{version}/spot/MainGroups", stoppingToken);
                    await TrySyncBasicAsync<OfferMode>($"/{version}/spot/OfferModes", stoppingToken);
                    await TrySyncBasicAsync<PackagingType>($"/{version}/spot/PackagingTypes", stoppingToken);
                    await TrySyncBasicAsync<SettlementType>($"/{version}/spot/SettlementTypes", stoppingToken);
                    await TrySyncBasicAsync<SecurityType>($"/{version}/spot/SecurityTypes", stoppingToken);
                    await TrySyncBasicAsync<OfferType>($"/{version}/spot/OfferTypes", stoppingToken);
                    await TrySyncBasicAsync<Manufacturer>($"/{version}/spot/Manufacturers", stoppingToken);
                    await TrySyncBasicAsync<Supplier>($"/{version}/spot/Suppliers", stoppingToken);
                    await TrySyncBasicAsync<MeasurementUnit>($"/{version}/spot/MeasurementUnits", stoppingToken);
                    await TrySyncBasicAsync<CurrencyUnit>($"/{version}/spot/CurrencyUnits", stoppingToken);
                    await TrySyncBasicAsync<ContractType>($"/{version}/spot/ContractTypes", stoppingToken);
                    await TrySyncBasicAsync<DeliveryPlace>($"/{version}/spot/DeliveryPlaces", stoppingToken);
                    await TrySyncBasicAsync<TradingHall>($"/{version}/spot/TradingHalls", stoppingToken);
                    await TrySyncBasicAsync<Tender>($"/{version}/spot/Tenders", stoppingToken);

                    await TrySyncOperationalAsync<Offer>($"/{version}/spot/Offers", stoppingToken);
                    await TrySyncOperationalAsync<TradeReport>($"/{version}/spot/reports/Trades", stoppingToken);
                    await TrySyncOperationalAsync<NewsNotification>("api/Notifications/NewsNotificationsByDate", stoppingToken);
                    await TrySyncOperationalAsync<SpotNotification>("/api/Notifications/SpotNotificationsByDate", stoppingToken);

                    _logger.LogInformation("چرخه همگام‌سازی موازی با موفقیت به پایان رسید.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطا در حین اجرای چرخه همگام‌سازی موازی.");
                }

                _logger.LogInformation("در حال انتظار برای شروع چرخه بعدی همگام‌سازی...");
                await Task.Delay(_cycleDelay, stoppingToken);
            }

            _logger.LogInformation("سرویس همگام‌سازی داده‌ها متوقف شد.");
        }

        // متد Helper جدید برای اطلاعات پایه
        private async Task TrySyncBasicAsync<T>(string endpoint, CancellationToken token) where T : RootObj<int>
        {
            var entityName = typeof(T).Name;
            if (_settings.BasicInformation.TryGetValue(entityName, out bool isEnabled) && isEnabled)
            {
                await SyncBasicInformation<T>(endpoint, token);
            }
            else
            {
                _logger.LogDebug("همگام‌سازی برای {EntityName} در تنظیمات غیرفعال است.", entityName);
            }
        }

        // متد Helper جدید برای منابع عملیاتی
        private async Task TrySyncOperationalAsync<T>(string endpoint, CancellationToken token) where T : RootObj<int>
        {
            var entityName = typeof(T).Name;
            if (_settings.OperationalResources.TryGetValue(entityName, out var resourceSettings) && resourceSettings.IsEnabled)
            {
                var fromDate = DateTime.Now.AddDays(resourceSettings.FromDateDaysOffset);
                var toDate = DateTime.Now.AddDays(resourceSettings.ToDateDaysOffset);
                await SyncOperationalResource<T>(endpoint, fromDate, toDate, token);
            }
            else
            {
                _logger.LogDebug("همگام‌سازی برای {EntityName} در تنظیمات غیرفعال است.", entityName);
            }
        }

        private async Task SyncBasicInformation<T>(string endpointPath, CancellationToken stoppingToken) where T : RootObj<int>
        {
            // بدون تغییر
            if (stoppingToken.IsCancellationRequested) return;
            _logger.LogInformation("شروع همگام‌سازی برای: {EntityName}", typeof(T).Name);
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var remoteService = scope.ServiceProvider.GetRequiredService<IRemotePublicResurceService<T>>();
                var repository = scope.ServiceProvider.GetRequiredService<IDataRepository<T>>();
                remoteService.EndPointPath = endpointPath;
                var items = await remoteService.RetrieveResurcesDataAsync();
                await repository.UpsertAsync(items);
                _logger.LogInformation("همگام‌سازی برای {EntityName} با موفقیت انجام شد. تعداد رکوردها: {Count}", typeof(T).Name, items.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در هنگام همگام‌سازی {EntityName}", typeof(T).Name);
                throw;
            }
        }

        private async Task SyncOperationalResource<T>(string endpointPath, DateTime fromDate, DateTime toDate, CancellationToken stoppingToken) where T : RootObj<int>
        {
            // بدون تغییر
            if (stoppingToken.IsCancellationRequested) return;
            _logger.LogInformation("شروع همگام‌سازی برای: {EntityName} از {FromDate} تا {ToDate}", typeof(T).Name, fromDate, toDate);
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var remoteService = scope.ServiceProvider.GetRequiredService<IRemoreOperationalResurceService<T>>();
                var repository = scope.ServiceProvider.GetRequiredService<IDataRepository<T>>();
                remoteService.EndPointPath = endpointPath;
                var items = await remoteService.RetrieveAsync(fromDate, toDate);
                await repository.UpsertAsync(items);
                _logger.LogInformation("همگام‌سازی برای {EntityName} با موفقیت انجام شد. تعداد رکوردها: {Count}", typeof(T).Name, items.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در هنگام همگام‌سازی {EntityName}", typeof(T).Name);
                throw;
            }
        }
    }
}