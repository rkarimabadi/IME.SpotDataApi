using IME.SpotDataApi.Models.Public;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IME.SpotDataApi.Models.Spot
{
    public class TradeReport : RootObj<int>
    {
        /// <summary> روش خرید </summary>
        public string BuyMethod { get; set; }

        /// <summary> شناسه کالا </summary>
        public int CommodityId { get; set; }

        /// <summary> شناسه نوع ارز </summary>
        public int CurrencyId { get; set; }


        /// <summary> شناسه عرضه </summary>       
        public int OfferId { get; set; }

        /// <summary> نماد عرضه </summary>
        public string OfferSymbol { get; set; }



        /// <summary> شناسه نوع قرارداد </summary>
        public int ContractTypeId { get; set; }


        /// <summary> نوع عرضه </summary>
        public string OfferType { get; set; }

        /// <summary> نحوه عرضه </summary>
        public string OfferMode { get; set; }

        /// <summary> مقدار سفارش </summary>
        public decimal DemandVolume { get; set; }

        /// <summary> میزان عرضه </summary>
        public decimal OfferVolume { get; set; }

        /// <summary> میانگین وزنی قیمت پایانی </summary>
        public decimal FinalWeightedAveragePrice { get; set; }

        /// <summary> سر رسید </summary>
        public string DueDate { get; set; }

        /// <summary> شناسه تولید کننده </summary>
        public int ManufacturerId { get; set; }


        /// <summary> شناسه واحد اندازه گیری </summary>
        public int MeasurementUnitId { get; set; }

        /// <summary> بیشینه قیمت </summary>
        public decimal MaximumPrice { get; set; }

        /// <summary> کمینه قیمت </summary>
        public decimal MinimumPrice { get; set; }

        /// <summary> قیمت پایه عرضه </summary>
        public decimal OfferBasePrice { get; set; }

        /// <summary> شناسه کارگزاری فروشنده </summary>
        public int SellerBrokerId { get; set; }


        /// <summary> شناسه عرضه کننده </summary>
        public int SupplierId { get; set; }

        /// <summary> تاریخ معامله </summary>
        public string TradeDate { get; set; }

        /// <summary> ارزش معاملات (هزار ریال) </summary>
        public decimal TradeValue { get; set; }

        /// <summary> حجم معاملات </summary>
        public decimal TradeVolume { get; set; }
    }
}
