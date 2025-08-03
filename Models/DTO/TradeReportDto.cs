namespace IME.SpotDataApi.Models.DTO
{
    // --- DTOs for TradeReports ---
    public class TradeReportDto
    {
        public int Id { get; set; }
        public string OfferSymbol { get; set; }
        public string TradeDate { get; set; }
        public decimal TradeVolume { get; set; }
        public decimal TradeValue { get; set; }
    }
}

