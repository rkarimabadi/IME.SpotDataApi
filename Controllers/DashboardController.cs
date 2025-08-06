using IME.SpotDataApi.Models.Presentation;
using IME.SpotDataApi.Services.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IME.SpotDataApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("market-pulse")]
        [ProducesResponseType(typeof(MarketPulseData), 200)]
        public async Task<IActionResult> GetMarketPulse()
        {
            //var data = new MarketPulseData
            //{
            //    Items = new List<PulseCardItem>
            //    {
            //        new() { Title = "ارزش معاملات", Value = "۱۲.۳ همت", Change = "+۱.۵٪", ChangeState = ValueState.Positive },
            //        new() { Title = "شاخص رقابت", Value = "۸.۲٪", Change = "-۰.۳٪", ChangeState = ValueState.Negative }
            //    }
            //};
            var data = await _dashboardService.GetMarketPulseAsync();
            return Ok(data);
        }

        [HttpGet("market-sentiment")]
        [ProducesResponseType(typeof(MarketSentimentData), 200)]
        public async Task<IActionResult> GetMarketSentiment()
        {
            //var data = new MarketSentimentData
            //{
            //    Items = new List<SentimentItem>
            //    {
            //        new() { Name = "نقدی", Percentage = 65, ColorCssVariable = "var(--primary-color)" },
            //        new() { Name = "سلف", Percentage = 25, ColorCssVariable = "var(--success-color)" },
            //        new() { Name = "نسیه", Percentage = 10, ColorCssVariable = "var(--warning-color)" }
            //    }
            //};
            var data = await _dashboardService.GetMarketSentimentAsync();
            return Ok(data);
        }

        [HttpGet("market-excitement")]
        [ProducesResponseType(typeof(MarketExcitementData), 200)]
        public async Task<IActionResult> GetMarketExcitement()
        {
            //var data =  new MarketExcitementData
            //{
            //    Title = "تقاضای بالا",
            //    Description = "بخش عمده معاملات امروز به روش حراج باز انجام شده که نشان‌دهنده رقابت شدید و هیجان در بازار است.",
            //    Percentage = 70,
            //    Label = "حراج"
            //};
            var data = await _dashboardService.GetMarketExcitementAsync();
            return Ok(data);
        }

        [HttpGet("supply-risk")]
        [ProducesResponseType(typeof(SupplyRiskData), 200)]
        public async Task<IActionResult> GetSupplyRisk()
        {
            //var data = new SupplyRiskData
            //{
            //    Items = new List<SupplyRiskItem>
            //    {
            //        new()
            //        {
            //            Title = "مس کاتد",
            //            Subtitle = "ریسک بالا",
            //            RiskLevel = RiskLevel.High,
            //            Value = "۱ عرضه‌کننده"
            //        },
            //        new()
            //        {
            //            Title = "میلگرد",
            //            Subtitle = "ریسک پایین",
            //            RiskLevel = RiskLevel.Low,
            //            Value = "۱۲ عرضه‌کننده"
            //        }
            //    }
            //};
            var data = await _dashboardService.GetSupplyRiskAsync();
            return Ok(data);
        }

        [HttpGet("market-movers")]
        [ProducesResponseType(typeof(MarketMoversData), 200)]
        public async Task<IActionResult> GetMarketMovers()
        {
            //var data = new MarketMoversData
            //{
            //    CompetitionItems = new List<MarketMoverItem>
            //    {
            //        new() { Rank = 1, Title = "ورق گالوانیزه", Subtitle = "فولاد مبارکه", Value = "+۱۲.۵٪", ValueState = ValueState.Positive },
            //        new() { Rank = 2, Title = "مس کاتد", Subtitle = "شرکت ملی مس", Value = "+۹.۸٪", ValueState = ValueState.Positive }
            //    },
            //    DemandItems = new List<MarketMoverItem>
            //    {
            //        new() { Rank = 1, Title = "سیمان تیپ ۲", Subtitle = "سیمان تهران", Value = "۳.۵x", ValueState = ValueState.Neutral },
            //        new() { Rank = 2, Title = "پلی‌پروپیلن", Subtitle = "پتروشیمی مارون", Value = "۲.۸x", ValueState = ValueState.Neutral }
            //    }
            //};
            var data = await _dashboardService.GetMarketMoversAsync();
            return Ok(data);
        }
        
        [HttpGet("main-players")]
        [ProducesResponseType(typeof(IEnumerable<MainPlayer>), 200)]
        public IActionResult GetMainPlayers()
        {
            var data = new List<MainPlayer>
            {
                new()
                {
                    Type = "برترین کارگزار",
                    Name = "کارگزاری مفید",
                    IconCssClass = "bi bi-person-workspace",
                    MarketShare = 18.4m
                },
                new()
                {
                    Type = "برترین عرضه‌کننده",
                    Name = "فولاد مبارکه",
                    IconCssClass = "bi bi-buildings-fill",
                    MarketShare = 25.1m
                }
            };
            return Ok(data);
        }

        [HttpGet("trading-halls")]
        [ProducesResponseType(typeof(TradingHallsData), 200)]
        public IActionResult GetTradingHalls()
        {
            var data = new TradingHallsData
            {
                Items = new List<TradingHallItem>
                {
                    new() { Title = "صنعتی", IconCssClass = "bi bi-building", IconBgCssClass = "industrial", Value = "۵.۸ همت", Change = "+۳.۱٪", ChangeState = ValueState.Positive },
                    new() { Title = "پتروشیمی", IconCssClass = "bi bi-droplet-fill", IconBgCssClass = "petro", Value = "۴.۲ همت", Change = "-۱.۲٪", ChangeState = ValueState.Negative },
                    new() { Title = "کشاورزی", IconCssClass = "bi bi-tree-fill", IconBgCssClass = "agri", Value = "۰.۹ همت", Change = "+۵.۴٪", ChangeState = ValueState.Positive }
                }
            };
            return Ok(data);
        }

        [HttpGet("news")]
        [ProducesResponseType(typeof(NewsData), 200)]
        public IActionResult GetNews()
        {
            var data = new NewsData
            {
                Items = new List<NewsItem>
                {
                    new()
                    {
                        Category = NewsCategory.HotGroup,
                        MarketGroup = MarketGroup.Steel,
                        Source = "گروه داغ • فولاد",
                        Title = "افزایش تقاضا برای ورق سرد، قیمت‌ها را در گروه فولاد بالا برد"
                    },
                    new()
                    {
                        Category = NewsCategory.SupplyAnnouncement,
                        MarketGroup = MarketGroup.Cement,
                        Source = "اطلاعیه عرضه",
                        Title = "عرضه جدید سیمان تیپ ۲ تهران برای هفته آینده تایید شد"
                    },
                    new()
                    {
                        Category = NewsCategory.HotGroup,
                        MarketGroup = MarketGroup.Petrochemical,
                        Source = "گروه داغ • پتروشیمی",
                        Title = "رقابت سنگین بر سر پلیمرهای مهندسی در تالار پتروشیمی"
                    }
                }
            };     
            return Ok(data);
        }
    }
}
