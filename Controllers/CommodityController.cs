using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Services.CommodityLevel;
using Microsoft.AspNetCore.Mvc;

namespace IME.SpotDataApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommodityController : ControllerBase
    {
        private readonly ICommodityService _commodityService;

        public CommodityController(ICommodityService commodityService)
        {
            _commodityService = commodityService;
        }

        [HttpGet("{commodityId}/header")]
        [ProducesResponseType(typeof(CommodityHeaderData), 200)]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetHeader(int commodityId)
        {
            var data = await _commodityService.GetCommodityHeaderDataAsync(commodityId);
            return Ok(data);
        }

        [HttpGet("{commodityId}/hierarchy")]
        [ProducesResponseType(typeof(List<HierarchyItem>), 200)]
        public async Task<IActionResult> GetHierarchy(int commodityId)
        {
            var data = await _commodityService.GetCommodityHierarchyAsync(commodityId);
            return Ok(data);
        }

        [HttpGet("{commodityId}/price-trends")]
        [ProducesResponseType(typeof(PriceViewModel), 200)]
        public async Task<IActionResult> GetPriceTrends(int commodityId)
        {
            var data = await _commodityService.GetPriceTrendsAsync(commodityId);
            return Ok(data);
        }

        [HttpGet("{commodityId}/market-absorption")]
        [ProducesResponseType(typeof(MarketAbsorptionData), 200)]
        public async Task<IActionResult> GetMarketAbsorption(int commodityId)
        {
            var data = await _commodityService.GetMarketAbsorptionAsync(commodityId);
            return Ok(data);
        }

        [HttpGet("{commodityId}/attributes")]
        [ProducesResponseType(typeof(CommodityAttributesData), 200)]
        public async Task<IActionResult> GetAttributes(int commodityId)
        {
            var data = await _commodityService.GetCommodityAttributesAsync(commodityId);
            return Ok(data);
        }
        
        [HttpGet("{commodityId}/main-players")]
        [ProducesResponseType(typeof(IEnumerable<MainPlayer>), 200)]
        public async Task<IActionResult> GetMainPlayers(int commodityId)
        {
            var data = await _commodityService.GetMainPlayersAsync(commodityId);
            return Ok(data);
        }

        [HttpGet("{commodityId}/distributed-attributes")]
        [ProducesResponseType(typeof(DistributedAttributesData), 200)]
        public async Task<IActionResult> GetDistributedAttributes(int commodityId)
        {
            var data = await _commodityService.GetDistributedAttributesAsync(commodityId);
            return Ok(data);
        }

        [HttpGet("{commodityId}/player-distribution")]
        [ProducesResponseType(typeof(DistributedAttributesData), 200)]
        public async Task<IActionResult> GetPlayerDistribution(int commodityId)
        {
            var data = await _commodityService.GetPlayerDistributionAsync(commodityId);
            return Ok(data);
        }
        [HttpGet("{commodityId}/offer-history")]
        [ProducesResponseType(typeof(UpcomingOffersData), 200)]
        public async Task<IActionResult> GetOfferHistory(int commodityId)
        {
            var data = await _commodityService.GetOfferHistoryAsync(commodityId);
            return Ok(data);
        }
    }
}
