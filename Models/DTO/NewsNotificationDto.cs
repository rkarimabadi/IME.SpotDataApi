namespace IME.SpotDataApi.Models.DTO
{
    // --- DTOs for Notifications ---
    public class NewsNotificationDto
    {
        public int Id { get; set; }
        public string? MainTitle { get; set; }
        public string? ShortAbstract { get; set; }
        public DateTime NewsDateTime { get; set; }
        public string? URL { get; set; }
    }
}

