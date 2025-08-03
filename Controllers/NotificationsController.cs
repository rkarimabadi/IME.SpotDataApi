using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.DTO;
using Microsoft.AspNetCore.Mvc;


namespace IME.SpotDataApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationRepository _repository;

        public NotificationsController(INotificationRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("news")]
        public async Task<IActionResult> GetNews([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var news = await _repository.GetNewsNotificationsAsync(pageNumber, pageSize);
            var dtos = news.Select(n => new NewsNotificationDto
            {
                Id = n.Id,
                MainTitle = n.MainTitle,
                ShortAbstract = n.ShortAbstract,
                NewsDateTime = n.NewsDateTime,
                URL = n.URL
            });
            return Ok(dtos);
        }

        [HttpGet("spot")]
        public async Task<IActionResult> GetSpot([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var spot = await _repository.GetSpotNotificationsAsync(pageNumber, pageSize);
            var dtos = spot.Select(s => new SpotNotificationDto
            {
                Id = s.Id,
                MainTitle = s.MainTitle,
                NewsDateTime = s.NewsDateTime,
                URL = s.URL
            });
            return Ok(dtos);
        }
    }
}
