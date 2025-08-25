using IME.SpotDataApi.Services.Search;
using Microsoft.AspNetCore.Mvc;

namespace IME.SpotDataApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet("{term}")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Search(string term)
        {
            var data = await _searchService.GlobalSearchAsync(term);
            return Ok(data);
        }
    }
}
