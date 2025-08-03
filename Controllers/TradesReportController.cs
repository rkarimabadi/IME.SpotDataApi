using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.DTO;
using Microsoft.AspNetCore.Mvc;


namespace IME.SpotDataApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradeReportsController : ControllerBase
    {
        private readonly ITradeReportRepository _repository;

        public TradeReportsController(ITradeReportRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetTradeReports([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var reports = await _repository.GetTradeReportsAsync(pageNumber, pageSize);
            var dtos = reports.Select(t => new TradeReportDto
            {
                Id = t.Id,
                OfferSymbol = t.OfferSymbol,
                TradeDate = t.TradeDate,
                TradeVolume = t.TradeVolume,
                TradeValue = t.TradeValue
            });
            return Ok(dtos);
        }
    }
}
