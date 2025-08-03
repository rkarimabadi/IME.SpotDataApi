namespace IME.SpotDataApi.Models.Notification
{
    public class NotificationRequest
    {
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }
}
