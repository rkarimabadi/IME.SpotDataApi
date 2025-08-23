using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Models.Spot;
using IME.SpotDataApi.Services.MainGroupLevel;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IME.SpotDataApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubGroupController : ControllerBase
    {
        private readonly ISubGroupService _subGroupService;

        public SubGroupController(ISubGroupService subGroupService)
        {
            _subGroupService = subGroupService;
        }

        [HttpGet("{subGroupId}/commodities")]
        [ProducesResponseType(typeof(GroupListData), 200)]
        public async Task<IActionResult> GetActiveCommodities(int subGroupId)
        {
            var data = await _subGroupService.GetActiveCommoditiesAsync(subGroupId);
            return Ok(data);
        }

        [HttpGet("{subGroupId}/activities")]
        [ProducesResponseType(typeof(MarketConditionsData), 200)]
        public async Task<IActionResult> GetCommodityActivities(int subGroupId)
        {
            var data = await _subGroupService.GetCommodityActivitiesAsync(subGroupId);
            return Ok(data);
        }
        [HttpGet("{subGroupId}/offer-history")]
        [ProducesResponseType(typeof(UpcomingOffersData), 200)]
        public async Task<IActionResult> GetOfferHistory(int subGroupId)
        {
            var data = await _subGroupService.GetOfferHistoryAsync(subGroupId);
            return Ok(data);
        }
    }
}
