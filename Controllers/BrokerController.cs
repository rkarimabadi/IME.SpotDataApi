using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Services.BrokerLevel;
using Microsoft.AspNetCore.Mvc;

namespace IME.SpotDataApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrokerController : ControllerBase
    {
        private readonly IBrokerService _brokerService;

        public BrokerController(IBrokerService brokerService)
        {
            _brokerService = brokerService;
        }

        [HttpGet("{brokerId:int}/header")]
        [ProducesResponseType(typeof(BrokerHeaderData), 200)]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetBrokerHeader(int brokerId)
        {
            var data = await _brokerService.GetBrokerHeaderAsync(brokerId);
            return Ok(data);
        }

        [HttpGet("{brokerId:int}/competition-ratio")]
        [ProducesResponseType(typeof(CompetitionData), 200)]
        public async Task<IActionResult> GetCompetitionRatio(int brokerId)
        {
            var data = await _brokerService.GetCompetitionRatioAsync(brokerId);
            return Ok(data);
        }

        [HttpGet("{brokerId:int}/success-rate")]
        [ProducesResponseType(typeof(CompetitionData), 200)]
        public async Task<IActionResult> GetSuccessRate(int brokerId)
        {
            var data = await _brokerService.GetSuccessRateAsync(brokerId);
            return Ok(data);
        }

        [HttpGet("{brokerId:int}/market-share")]
        [ProducesResponseType(typeof(List<MarketShareItem>), 200)]
        public async Task<IActionResult> GetMarketShare(int brokerId)
        {
            var data = await _brokerService.GetMarketShareAsync(brokerId);
            return Ok(data);
        }

        [HttpGet("{brokerId:int}/ranking")]
        [ProducesResponseType(typeof(List<RankingItem>), 200)]
        public async Task<IActionResult> GetRanking(int brokerId)
        {
            var data = await _brokerService.GetRankingAsync(brokerId);
            return Ok(data);
        }

        [HttpGet("{brokerId:int}/commodity-group-share")]
        [ProducesResponseType(typeof(List<CommodityGroupShareItem>), 200)]
        public async Task<IActionResult> GetCommodityGroupShare(int brokerId)
        {
            var data = await _brokerService.GetCommodityGroupShareAsync(brokerId);
            return Ok(data);
        }

        [HttpGet("{brokerId:int}/offers")]
        [ProducesResponseType(typeof(UpcomingOffersData), 200)]
        public async Task<IActionResult> GetBrokerOffers(int brokerId)
        {
            var data = await _brokerService.GetBrokerOffersAsync(brokerId);
            return Ok(data);
        }



        [HttpGet("{brokerId:int}/top-suppliers")]
        [ProducesResponseType(typeof(TopSuppliersData), 200)]
        public async Task<IActionResult> GetTopSuppliers(int brokerId)
        {
            var data = await _brokerService.GetTopSuppliersAsync(brokerId);
            return Ok(data);
        }

        [HttpGet("{brokerId:int}/all-suppliers")]
        [ProducesResponseType(typeof(List<SupplierItem>), 200)]
        public async Task<IActionResult> GetAllSuppliers(int brokerId)
        {
            var data = await _brokerService.GetAllSuppliersAsync(brokerId);
            return Ok(data);
        }

        [HttpGet("{brokerId:int}/strategic-performance")]
        [ProducesResponseType(typeof(List<StrategicPerformanceItem>), 200)]
        public async Task<IActionResult> GetStrategicPerformance(int brokerId)
        {
            var data = await _brokerService.GetStrategicPerformanceAsync(brokerId);
            return Ok(data);
        }
    }
}