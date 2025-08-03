using IME.SpotDataApi.Models.Public;

namespace IME.SpotDataApi.Models.Notification
{
    public class NewsNotification : RootObj<int>
    {
        public string? PriorTitle { get; set; }

        public string? ShortAbstract { get; set; }

        public string? SubTitle { get; set; }

        public string? Body { get; set; }

        public string? FirstPicture { get; set; }

        public int Id { get; set; }

        public string? MainTitle { get; set; }

        public DateTime NewsDateTime { get; set; }

        public string? URL { get; set; }
    }
}
