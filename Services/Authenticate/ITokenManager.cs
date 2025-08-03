
using IdentityModel.Client;

namespace IME.SpotDataApi.Services.Authenticate
{
    public interface ITokenManager
    {
        Task<TokenResponse> GetAccessToken(bool force = false);
    }
}