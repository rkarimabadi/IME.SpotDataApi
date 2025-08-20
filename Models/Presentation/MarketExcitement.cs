namespace IME.SpotDataApi.Models.Presentation
{
    public class MarketExcitementData
    {
        /// <summary>
        /// عنوان اصلی ویجت، مثلا: تقاضای بالا
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات تکمیلی که در کنار نمودار نمایش داده می‌شود
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// درصد معاملات از نوع اصلی (مثلا حراج)، عددی بین ۰ تا ۱۰۰
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// برچسبی که در مرکز نمودار نمایش داده می‌شود، مثلا: حراج
        /// </summary>
        public string Label { get; set; } = string.Empty;
    }
    public class ExcitementStat
    {
        public decimal InitPrice { get; set; }
        public decimal OfferVol { get; set; }
        public decimal InitVolume { get; set; }
        public decimal FinalPriceSum { get; set; }
        public decimal TradeVolumeSum { get; set; }
        public decimal DemandVolumeSum { get; set; }
    }

}
