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
        [HttpGet("market-heatmap")]
        [ProducesResponseType(typeof(MarketHeatmapData), 200)]
        public async Task<IActionResult> GetMarketHeatmap()
        {
            var data = await _marketsService.GetMarketHeatmapDataAsync();
            return Ok(data);
        }
        [HttpGet("market-shortcuts")]
        [ProducesResponseType(typeof(MarketHeatmapData), 200)]
        public async Task<IActionResult> GetMarketShortcuts()
        {
            var data = new MarketShortcutsData
            {
                Items = new List<MarketShortcutItem>
                {
                    new() { Title = "اموال غیر منقول", IconCssClass = "bi bi-house-door-fill", ThemeCssClass = "real-estate" },
                    new() { Title = "بازار فرعی", IconCssClass = "bi bi-shop", ThemeCssClass = "secondary" },
                    new() { Title = "صنعتی", IconCssClass = "bi bi-building", ThemeCssClass = "industrial" },
                    new() { Title = "فرآورده های نفتی", IconCssClass = "bi bi-fuel-pump-fill", ThemeCssClass = "oil-products" },
                    new() { Title = "معدنی", IconCssClass = "bi bi-gem", ThemeCssClass = "mineral" },
                    new() { Title = "پتروشیمی", IconCssClass = "bi bi-droplet-fill", ThemeCssClass = "petro" },
                    new() { Title = "کشاورزی", IconCssClass = "bi bi-tree-fill", ThemeCssClass = "agri" }
                }
            };
            return Ok(data);
        }
    }
}
