using IME.MobileApp.Api.Models;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.General;
using IME.SpotDataApi.Models.Spot;
using IME.SpotDataApi.Services.Authenticate;
using IME.SpotDataApi.Services.RemoteData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

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
