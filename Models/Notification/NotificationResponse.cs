namespace IME.SpotDataApi.Models.Notification
{
    public class NotificationResponse<T> where T : class
    {
        public int TotalCount { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public int PageIndex { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }

        public int TotalCountInDataStore { get; set; }

        public string TimeElapsed { get; set; }

        public bool Success { get; set; }

        public List<string> Messages { get; set; }

        public List<string> Metadata { get; set; }

        public List<T> Data { get; set; }
    }

}
