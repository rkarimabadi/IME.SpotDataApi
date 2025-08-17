using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Presentation;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace IME.SpotDataApi.Services.OfferDetails
{
    public interface IOfferDetailsService
    {
        Task<OfferViewModel> GetOfferByIdAsync(int id);
    }

    /// <summary>
    /// نسخه ریفکتور شده با هدف حل مشکل N+1 Query.
    /// تمام اطلاعات مورد نیاز برای نمایش جزئیات عرضه، در یک کوئری واحد واکشی می‌شود.
    /// </summary>
    public class OfferDetailsService : IOfferDetailsService
    {
        private readonly IDbContextFactory<AppDataContext> _contextFactory;

        public OfferDetailsService(IDbContextFactory<AppDataContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<OfferViewModel> GetOfferByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // بهینه‌سازی کامل: با استفاده از Join های متعدد (که به LEFT JOIN ترجمه می‌شوند)
            // تمام اطلاعات مورد نیاز را در یک درخواست واحد از دیتابیس واکشی می‌کنیم.
            // برای پایداری بیشتر، ابتدا کوئری مربوط به سلسله مراتب را تعریف می‌کنیم.
            var hierarchyQuery = from commodity in context.Commodities
                                 join subGroup in context.SubGroups on commodity.ParentId equals subGroup.Id into sgj from subGroup in sgj.DefaultIfEmpty()
                                 join groupEntity in context.Groups on subGroup.ParentId equals groupEntity.Id into gj from groupEntity in gj.DefaultIfEmpty()
                                 join mainGroup in context.MainGroups on groupEntity.ParentId equals mainGroup.Id into mgj from mainGroup in mgj.DefaultIfEmpty()
                                 select new
                                 {
                                     Commodity = commodity,
                                     SubGroup = subGroup,
                                     Group = groupEntity,
                                     MainGroup = mainGroup
                                 };

            var query = from offer in context.Offers.Where(x => x.Id == id)
                        // Join all lookup tables
                        join broker in context.Brokers on offer.BrokerId equals broker.Id into bj from broker in bj.DefaultIfEmpty()
                        join contractType in context.ContractTypes on offer.ContractTypeId equals contractType.Id into ctj from contractType in ctj.DefaultIfEmpty()
                        join currencyUnit in context.CurrencyUnits on offer.CurrencyId equals currencyUnit.Id into cuj from currencyUnit in cuj.DefaultIfEmpty()
                        join manufacturer in context.Manufacturers on offer.ManufacturerId equals manufacturer.Id into mfj from manufacturer in mfj.DefaultIfEmpty()
                        join measurementUnit in context.MeasurementUnits on offer.MeasureUnitId equals measurementUnit.Id into muj from measurementUnit in muj.DefaultIfEmpty()
                        join supplier in context.Suppliers on offer.SupplierId equals supplier.Id into spj from supplier in spj.DefaultIfEmpty()
                        join offerMode in context.OfferModes on offer.OfferModeId equals offerMode.Id into omj from offerMode in omj.DefaultIfEmpty()
                        join deliveryPlace in context.DeliveryPlaces on offer.DeliveryPlaceId equals deliveryPlace.Id into dpj from deliveryPlace in dpj.DefaultIfEmpty()
                        join buyMethod in context.BuyMethods on offer.BuyMethodId equals buyMethod.Id into bmj from buyMethod in bmj.DefaultIfEmpty()
                        join offerType in context.OfferTypes on offer.OfferTypeId equals offerType.Id into otj from offerType in otj.DefaultIfEmpty()
                        join settlementType in context.SettlementTypes on offer.SettlementTypeId equals settlementType.Id into stj from settlementType in stj.DefaultIfEmpty()
                        join securityType in context.SecurityTypes on offer.SecurityTypeId equals securityType.Id into secj from securityType in secj.DefaultIfEmpty()
                        join packagingType in context.PackagingTypes on offer.PackagingTypeId equals packagingType.Id into ptj from packagingType in ptj.DefaultIfEmpty()
                        // Join hierarchy
                        join h in hierarchyQuery on offer.CommodityId equals h.Commodity.Id into hj from h in hj.DefaultIfEmpty()
                        select new OfferViewModel
                        {
                            Id = offer.Id,
                            Symbol = offer.OfferSymbol,
                            Description = offer.Description,
                            OfferDate = offer.OfferDate,
                            DeliveryDate = offer.DeliveryDate,
                            InitialPrice = offer.InitPrice,
                            OfferVolume = offer.OfferVol,
                            TickSize = offer.TickSize,
                            MaxOrderVolume = offer.MaxOrderVol,
                            MinOrderVolume = offer.MinOrderVol,
                            OfferRing = offer.OfferRing,
                            PrepaymentPercent = offer.PrepaymentPercent,
                            LotSize = offer.LotSize,
                            WeightFactor = offer.WeightFactor,
                            Broker = broker.PersianName,
                            CommodityName = h.Commodity.PersianName,
                            ContractType = contractType.PersianName,
                            CurrencyUnit = currencyUnit.PersianName,
                            Manufacturer = manufacturer.PersianName,
                            MeasurementUnit = measurementUnit.PersianName,
                            Supplier = supplier.PersianName,
                            OfferMode = offerMode.PersianName,
                            DeliveryPlace = deliveryPlace.PersianName,
                            BuyMethod = buyMethod.PersianName,
                            OfferType = offerType.PersianName,
                            SettlementType = settlementType.PersianName,
                            SecurityType = securityType.PersianName,
                            PackagingType = packagingType.PersianName,
                            HierarchyItems = new List<HierarchyItem>
                            {
                                new HierarchyItem { Id = offer.TradingHallId, Name = offer.OfferRing, IsActive = true },
                                new HierarchyItem { Id = h.MainGroup.Id, Name = h.MainGroup.PersianName, IsActive = true },
                                new HierarchyItem { Id = h.Group.Id, Name = h.Group.PersianName, IsActive = true },
                                new HierarchyItem { Id = h.SubGroup.Id, Name = h.SubGroup.PersianName, IsActive = true },
                                new HierarchyItem { Id = offer.CommodityId, Name = h.Commodity.Symbol, IsActive = false }
                            }
                        };

            var result = await query.FirstOrDefaultAsync();

            return result ?? new OfferViewModel();
        }
    }
}
