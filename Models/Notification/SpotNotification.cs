using IME.SpotDataApi.Models.Public;

namespace IME.SpotDataApi.Models.Notification
{
    public class SpotNotification : RootObj<int>
    {
        public int Id { get; set; }

        public string? MainTitle { get; set; }

        public DateTime NewsDateTime { get; set; }

        public string? URL { get; set; }
    }
}
