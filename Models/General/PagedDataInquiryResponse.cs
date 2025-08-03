using IME.SpotDataApi.Models.Core;

namespace IME.SpotDataApi.Models.General
{
    public class PagedDataInquiryResponse<T> : IPageLinkContaining
    {
        private List<ResponseItem<T>> _items;
        private List<Link> _links;

        public List<ResponseItem<T>> Items
        {
            get => _items ??= new List<ResponseItem<T>>();
            set => _items = value;
        }

        /// <summary>
        /// تعداد صفحات
        /// </summary>
        public int PageSize { get; set; } = Constants.Paging.DefaultPageSize;

        /// <summary>
        /// جمع تعداد آیتم ها
        /// </summary>
        public int TotalItemCount { get; set; }

        /// <summary>
        /// لینک ها
        /// </summary>
        public List<Link> Links
        {
            get => _links ??= new List<Link>();
            set => _links = value;
        }

        public void AddLink(Link link)
        {
            Links.Add(link);
        }

        /// <summary>
        /// شماره صفحه
        /// </summary>
        public int PageNumber { get; set; } = Constants.Paging.MinPageNumber;

        /// <summary>
        /// تعداد صفحه
        /// </summary>
        public int PageCount { get; set; }
    }
}