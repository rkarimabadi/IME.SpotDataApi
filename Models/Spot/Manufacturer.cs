using IME.SpotDataApi.Models.Public;

namespace IME.SpotDataApi.Models.Spot
{
    /// <summary> تولید کننده </summary>
    public class Manufacturer : BaseInfo
    {
        /// <summary>
        /// نماد تولید کننده
        /// </summary>
        public string Symbol { get; set; }
    }
}
