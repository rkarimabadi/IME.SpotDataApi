using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Services.GroupLevel;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IME.SpotDataApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpGet("{groupId}/sub-groups")]
        [ProducesResponseType(typeof(GroupListData), 200)]
        public async Task<IActionResult> GetActiveSubGroups(int groupId)
        {
            var data = await _groupService.GetActiveSubGroupsAsync(groupId);
            return Ok(data);
        }

        [HttpGet("{groupId}/activities")]
        [ProducesResponseType(typeof(MarketConditionsData), 200)]
        public async Task<IActionResult> GetGroupActivities(int groupId)
        {
            var data = await _groupService.GetSubGroupActivitiesAsync(groupId);
            return Ok(data);
        }
        [HttpGet("{groupId}/upcoming-offers")]
        [ProducesResponseType(typeof(UpcomingOffersData), 200)]
        public async Task<IActionResult> GetUpcomingOffers(int groupId)
        {
            var data = await _groupService.GetUpcomingOffersAsync(groupId);
            return Ok(data);
        }
        [HttpGet("{groupId}/today-offers")]
        [ProducesResponseType(typeof(UpcomingOffersData), 200)]
        public async Task<IActionResult> GetTodayOffers(int groupId)
        {
            var data = await _groupService.GetTodayOffersAsync(groupId);
            return Ok(data);
        }
        [HttpGet("{groupId}/header")]
        [ProducesResponseType(typeof(GroupHeaderData), 200)]
        public async Task<IActionResult> GetHeader(int groupId)
        {
            var data = await _groupService.GetGroupHeaderDataAsync(groupId);
            return Ok(data);
        }

        [HttpGet("{groupId}/hierarchy")]
        [ProducesResponseType(typeof(List<HierarchyItem>), 200)]
        public async Task<IActionResult> GetHierarchy(int groupId)
        {
            var data = await _groupService.GetGroupHierarchyAsync(groupId);
            return Ok(data);
        }
    }
}
