using IME.SpotDataApi.Models.Public;
using System.ComponentModel.DataAnnotations.Schema;

namespace IME.SpotDataApi.Models.Spot
{
    //TODO: در نگارش بعدی بایستی عنوان تغییر پیدا کند 
    //TODO: مفهوم حراج و مناقصه دیده نشده است
    /// <summary> عرضه </summary>
    public class Offer : RootObj<int>
    {
        /// <summary> شناسه کارگزاری </summary>
        public int BrokerId { get; set; }
        /// <summary> کارگزاری </summary>

        /// <summary> شناسه روش خرید </summary>
        public int BuyMethodId { get; set; }

        /// <summary> شناسه کالا </summary>
        public int CommodityId { get; set; }


        /// <summary> شناسه نوع قرارداد </summary>
        public int ContractTypeId { get; set; }

        /// <summary> شناسه ارز </summary>
        public int CurrencyId { get; set; }


        /// <summary> تاریخ تحویل </summary>
        public string DeliveryDate { get; set; }

        /// <summary> شناسه محل تحویل </summary>
        public int DeliveryPlaceId { get; set; }

        /// <summary> شرح عرضه </summary>
        public string Description { get; set; }

        /// <summary> قیمت پایه </summary>
        public decimal InitPrice { get; set; }

        /// <summary> مقدار عرضه اولیه </summary>
        public decimal InitVolume { get; set; }

        /// <summary> اندازه محموله </summary>
        public decimal LotSize { get; set; }

        /// <summary> شناسه تولید کننده </summary>
        public int ManufacturerId { get; set; }

        /// <summary> بیشینه قیمت پایه </summary>
        public decimal MaxInitPrice { get; set; }

        /// <summary> بیشینه افزایش مقدار عرضه </summary>
        public decimal MaxIncOfferVol { get; set; }

        /// <summary> بیشینه مقدار سفارش </summary>
        public decimal MaxOrderVol { get; set; }

        /// <summary> بیشینه قیمت عرضه </summary>
        public decimal MaxOfferPrice { get; set; }

        /// <summary> کد واحد اندازه گیری </summary>
        public int MeasureUnitId { get; set; }

        /// <summary> کمینه مقدار تخصیص </summary>
        public decimal MinAllocationVol { get; set; }

        /// <summary> کمینه مقدار عرضه </summary>
        public decimal MinOfferVol { get; set; }

        /// <summary> کمینه قیمت پایه </summary>
        public decimal MinInitPrice { get; set; }

        /// <summary> کمینه مقدار سفارش </summary>
        public decimal MinOrderVol { get; set; }

        /// <summary> کمینه قیمت عرضه </summary>
        public decimal MinOfferPrice { get; set; }

        /// <summary> تاریخ عرضه </summary>
        public string OfferDate { get; set; }

        /// <summary> شناسه نحوه عرضه </summary>
        public int OfferModeId { get; set; }

        /// <summary> نام تالار عرضه </summary>
        public string OfferRing { get; set; }

        /// <summary> نماد عرضه </summary>
        public string OfferSymbol { get; set; }

        /// <summary> نوع عرضه </summary>
        public int OfferTypeId { get; set; }

        /// <summary> میزان عرضه </summary>
        public decimal OfferVol { get; set; }

        /// <summary> نوع بسته بندی </summary>
        public int PackagingTypeId { get; set; }

        /// <summary> خطای مجاز تحویل </summary>
        public decimal PermissibleError { get; set; }

        /// <summary> کمینه مقدار کشف نرخ </summary>
        public decimal PriceDiscoveryMinOrderVol { get; set; }

        /// <summary> درصد پیش پرداخت </summary>
        public decimal PrepaymentPercent { get; set; }

        /// <summary> نوع تضمین </summary>
        public int SecurityTypeId { get; set; }

        /// <summary> توضیحات تضمین </summary>
        public string SecurityTypeNote { get; set; }

        /// <summary> کد نوع تسویه </summary>
        public int SettlementTypeId { get; set; }

        /// <summary> شناسه عرضه کننده </summary>
        public int SupplierId { get; set; }

        /// <summary> کمینه حرکت قیمت </summary>
        public decimal TickSize { get; set; }

        /// <summary> کد تالار معاملات </summary>
        public int TradingHallId { get; set; }

        /// <summary> وضعیت معامله </summary>
        public string TradeStatus { get; set; }

        /// <summary> ضریب وزنی </summary>
        public decimal WeightFactor { get; set; }
    }
}