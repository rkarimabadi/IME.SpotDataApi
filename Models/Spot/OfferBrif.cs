using IME.SpotDataApi.Models.Public;
namespace IME.SpotDataApi.Models.Spot
{

    /// <summary> عرضه </summary>
    public class OfferBrif : RootObj<int>
    {
        public required string Commodity { get; set; }
        public required string Supplier { get; set; }
        public string Date { get; set; }
        public bool TradeStatus { get; set; }
    }
}