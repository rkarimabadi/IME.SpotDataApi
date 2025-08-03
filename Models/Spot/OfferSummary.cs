using IME.SpotDataApi.Models.Public;
namespace IME.SpotDataApi.Models.Spot
{

    /// <summary> عرضه </summary>
    public class OfferSummary : RootObj<int>
    {
        public required string TradingHall { get; set; }
        public int NumberOfOffers { get; set; }
        public int NumberOfTradedOffers { get; set; }
    }
}