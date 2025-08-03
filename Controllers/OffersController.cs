using IME.MobileApp.Api.Models;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Spot;
using IME.SpotDataApi.Services.RemoteData;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace IME.SpotDataApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OffersController : ControllerBase
    {
        private readonly IOfferRepository _repository;
        private readonly ILogger<OffersController> _logger;

        public OffersController(IOfferRepository repository, ILogger<OffersController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetOffers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var offers = await _repository.GetOffersAsync(pageNumber, pageSize);
            var dtos = offers.Select(o => new OfferBriefDto
            {
                Id = o.Id,
                OfferSymbol = o.OfferSymbol,
                OfferDate = o.OfferDate,
                TradeStatus = o.TradeStatus
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOfferById(int id)
        {
            var offer = await _repository.GetOfferByIdAsync(id);
            if (offer == null) return NotFound();

            var dto = new OfferDetailDto
            {
                Id = offer.Id,
                OfferSymbol = offer.OfferSymbol,
                Description = offer.Description,
                OfferDate = offer.OfferDate,
                DeliveryDate = offer.DeliveryDate,
                InitPrice = offer.InitPrice,
                OfferVol = offer.OfferVol,
                TradeStatus = offer.TradeStatus,
            };
            return Ok(dto);
        }
    }
}
