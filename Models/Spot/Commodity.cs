using IME.SpotDataApi.Models.Public;

namespace IME.SpotDataApi.Models.Spot
{
    /// <summary> کالا </summary>
    public class Commodity : BaseInfo
    {
        public int? ParentId { get; set; }
        public string Symbol { get; set; }
    }
}
