using System.Collections.Generic;

namespace IME.SpotDataApi.Models.Presentation
{
    public enum SearchResultType
    {
        Commodity,
        SubGroup,
        Group,
        MainGroup,
        Broker,
        Supplier,
        Manufacturer
    }


    public class SearchResultItem
    {
        /// <summary>
        /// شناسه موجودیت (مثلاً شناسه کالا یا کارگزار)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// عنوان اصلی نتیجه (مثلاً "میلگرد" یا "کارگزاری مفید")
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// زیرنویس برای توضیح نوع نتیجه (مثلاً "کالا" یا "کارگزار")
        /// </summary>
        public string Subtitle { get; set; }

        /// <summary>
        /// نوع شمارشی برای کمک به کلاینت در مپ کردن به مدل نمایشی
        /// </summary>
        public SearchResultType ResultType { get; set; }
    }

    public class SearchResultsData
    {
        public List<SearchResultItem> Items { get; set; } = new();
    }
}
