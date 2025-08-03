using IME.SpotDataApi.Models.Public;

namespace IME.SpotDataApi.Models.Spot
{
    /// <summary> مناقصه </summary>
    public class Tender : RootObj<int>
    {
        /// <summary> قیمت پایه </summary>
        public decimal InitPrice { get; set; }

        /// <summary> شناسه نوع تسویه </summary>
        public int SettlementTypeId { get; set; }

        /// <summary> شناسه نوع قرارداد </summary>
        public int ContractTypeId { get; set; }

        /// <summary> تاریخ تحویل </summary>
        public string DeliveryDate { get; set; }

        /// <summary> شناسه محل تحویل </summary>
        public int DeliveryPlaceId { get; set; }

        /// <summary> شناسه کالا </summary>
        public int CommodityId { get; set; }

        /// <summary> بیشینه افزایش تقاضا </summary>
        public int MaxIncreaseDemand { get; set; }

        /// <summary> ضریب بار </summary>
        public decimal LoadFactor { get; set; }

        /// <summary> تعداد محموله درخواستی </summary>
        public int LotCount { get; set; }

        /// <summary> اندازه محموله </summary>
        public int LotSize { get; set; } //LotSize

        /// <summary> بیشینه قیمت خرید </summary>
        public decimal MaxBuyPrice { get; set; }

        /// <summary> بیشینه قیمت فروش </summary>
        public decimal MaxSellPrice { get; set; }

        /// <summary> کمینه قیمت خرید </summary>
        public decimal MinBuyPrice { get; set; }

        /// <summary> کمینه قیمت فروش </summary>
        public decimal MinSellPrice { get; set; }

        /// <summary> نوع بسته بندی </summary>
        public int PackagingTypeId { get; set; }

        /// <summary> نماد مناقصه </summary>
        public string Symbol { get; set; }

        /// <summary> شناسه مناقصه گزار </summary>
        public int TenderApplicantId { get; set; }

        /// <summary> شناسه کارگزاری </summary>
        public int BrokerId { get; set; }

        /// <summary> تاریخ مناقصه </summary>
        public string TenderDate { get; set; }

        /// <summary> شرح مناقصه </summary>
        public string Description { get; set; }

        /// <summary> نوع تضمین </summary>
        public int SecurityTypeId { get; set; }
    }
}
