using IME.SpotDataApi.Models.DTO;
using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Services.OfferDetails;
using Microsoft.AspNetCore.Mvc;

namespace IME.SpotDataApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OfferDetailsController : ControllerBase
    {
        private readonly IOfferDetailsService _offerDetailsService;

        public OfferDetailsController(IOfferDetailsService offerDetailsService)
        {
            _offerDetailsService = offerDetailsService;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OfferViewModel), 200)]
        public async Task<IActionResult> GetOfferById(int id)
        {
            var data = await _offerDetailsService.GetOfferByIdAsync(id);
            return Ok(data);
        }
    }
}
