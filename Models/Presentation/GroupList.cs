using System.Collections.Generic;

namespace IME.SpotDataApi.Models.Presentation
{
    /// <summary>
    /// وضعیت فعالیت یک گروه را مشخص می‌کند
    /// </summary>
    public enum GroupActivityStatus
    {
        ActiveToday,  // امروز عرضه دارد
        ActiveFuture,// در آینده عرضه دارد
        Inactive    // عرضه‌ای ندارد
    }

    /// <summary>
    /// نمایانگر یک آیتم در لیست گروه‌های زیرمجموعه است
    /// </summary>
    public class GroupListItem
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string UrlName { get; set; } = string.Empty; // Group Id
        public GroupActivityStatus Status { get; set; }
        public int? OfferCount { get; set; }
    }

    /// <summary>
    /// نگهدارنده کل داده‌های مورد نیاز برای ویجت لیست گروه‌های زیرمجموعه
    /// </summary>
    public class GroupListData
    {
        public List<GroupListItem> Items { get; set; } = new();
    }
}
