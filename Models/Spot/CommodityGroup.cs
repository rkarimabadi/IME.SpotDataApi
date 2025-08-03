using IME.SpotDataApi.Models.Public;

namespace IME.SpotDataApi.Models.Spot
{
    /// <summary> گروه بندی کالا </summary>
    public class CommodityGroup : BaseInfo
    {
        /// <summary> شناسه والد </summary>
        public int? ParentId { get; set; }
    }
}
