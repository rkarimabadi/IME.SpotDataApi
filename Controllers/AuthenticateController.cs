using IdentityModel.Client;
using IME.SpotDataApi.Services.Authenticate;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace IME.SpotDataApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController(ITokenManager tokenManager) : ControllerBase
    {


        // GET: api/<ValuesController>
        [HttpGet]
        public async Task<TokenResponse> Get()
        {
            var token = await tokenManager.GetAccessToken();
            return token;
        }
    }
}
