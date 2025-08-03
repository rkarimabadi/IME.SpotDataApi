using IME.SpotDataApi.Models.Public;

namespace IME.SpotDataApi.Models.Spot
{
    /// <summary> عرضه کننده </summary>
    public class Supplier : BaseInfo
    {
        /// <summary>
        /// شناسه مشتری
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// کد ملی
        /// </summary>
        public string? NationalCode { get; set; }
    }
}
