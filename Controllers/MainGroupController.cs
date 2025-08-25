using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Services.MainGroupLevel;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IME.SpotDataApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MainGroupController : ControllerBase
    {
        private readonly IMainGroupService _mainGroupService;

        public MainGroupController(IMainGroupService mainGroupService)
        {
            _mainGroupService = mainGroupService;
        }

        [HttpGet("{mainGroupId}/groups")]
        [ProducesResponseType(typeof(GroupListData), 200)]
        public async Task<IActionResult> GetActiveGroups(int mainGroupId)
        {
            var data = await _mainGroupService.GetActiveGroupsAsync(mainGroupId);
            return Ok(data);
        }

        [HttpGet("{mainGroupId}/activities")]
        [ProducesResponseType(typeof(MarketConditionsData), 200)]
        public async Task<IActionResult> GetGroupActivities(int mainGroupId)
        {
            var data = await _mainGroupService.GetGroupActivitiesAsync(mainGroupId);
            return Ok(data);
        }
        [HttpGet("{mainGroupId}/upcoming-offers")]
        [ProducesResponseType(typeof(UpcomingOffersData), 200)]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetUpcomingOffers(int mainGroupId)
        {
            var data = await _mainGroupService.GetUpcomingOffersAsync(mainGroupId);
            return Ok(data);
        }
        [HttpGet("{mainGroupId}/market-share")]
        [ProducesResponseType(typeof(MarketShortcutsData), 200)]
        public async Task<IActionResult> GetMarketShare(int mainGroupId)
        {
            var data = await _mainGroupService.GetMarketShareAsync(mainGroupId);
            return Ok(data);
        }
        [HttpGet("{mainGroupId}/trade-share")]
        [ProducesResponseType(typeof(MarketShortcutsData), 200)]
        public async Task<IActionResult> GetTradeShare(int mainGroupId)
        {
            var data = await _mainGroupService.GetTradeShareAsync(mainGroupId);
            return Ok(data);
        }
    }
}
