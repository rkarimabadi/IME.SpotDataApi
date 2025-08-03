using IME.SpotDataApi.Models.Notification;
using IME.SpotDataApi.Models.Public;
using IME.SpotDataApi.Models.Spot;
using Microsoft.EntityFrameworkCore;

namespace IME.SpotDataApi.Data
{
    public class AppDataContext : DbContext
    {
        public AppDataContext()
        {
        }
        public AppDataContext(DbContextOptions<AppDataContext> options)
        : base(options)
        {
        }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<Broker> Brokers { get; set; }
        public DbSet<BuyMethod> BuyMethods { get; set; }
        public DbSet<Commodity> Commodities { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<SubGroup> SubGroups { get; set; }
        public DbSet<MainGroup> MainGroups { get; set; }
        public DbSet<OfferMode> OfferModes { get; set; }
        public DbSet<PackagingType> PackagingTypes { get; set; }
        public DbSet<SettlementType> SettlementTypes { get; set; }
        public DbSet<SecurityType> SecurityTypes { get; set; }
        public DbSet<OfferType> OfferTypes { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<MeasurementUnit> MeasurementUnits { get; set; }
        public DbSet<CurrencyUnit> CurrencyUnits { get; set; }
        public DbSet<ContractType> ContractTypes { get; set; }
        public DbSet<DeliveryPlace> DeliveryPlaces { get; set; }
        public DbSet<TradeReport> TradeReports { get; set; }
        public DbSet<TradingHall> TradingHalls { get; set; }
        public DbSet<NewsNotification> NewsNotifications { get; set; }
        public DbSet<SpotNotification> SpotNotifications { get; set; }
        public DbSet<Tender> Tenders { get; set; }
    }
}
