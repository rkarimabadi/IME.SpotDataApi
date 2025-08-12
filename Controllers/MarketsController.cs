using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Services.Markets;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IME.SpotDataApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketsController : ControllerBase
    {
        private readonly IMarketsService _marketsService;

        public MarketsController(IMarketsService marketsService)
        {
            _marketsService = marketsService;
        }

        [HttpGet("main-groups")]
        [ProducesResponseType(typeof(List<MarketInfo>), 200)]
        public async Task<IActionResult> GetMainGroups()
        {
            var data = await _marketsService.GetMainGroupsDataAsync();
            return Ok(data);
        }
        [HttpGet("index-groups")]
        [ProducesResponseType(typeof(CommodityStatusData), 200)]
        public async Task<IActionResult> GetIndexGroups()
        {
            var data = await _marketsService.GetIndexGroupsAsync();
            return Ok(data);
        }
        [HttpGet("market-activities")]
        [ProducesResponseType(typeof(List<MarketActivity>), 200)]
        public async Task<IActionResult> GetMarketActivities()
        {
            var data = await _marketsService.GetMarketActivitiesAsync();
            return Ok(data);
        }
    }
}
