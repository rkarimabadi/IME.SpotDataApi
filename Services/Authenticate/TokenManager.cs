using IdentityModel.Client;
using IME.SpotDataApi.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace IME.SpotDataApi.Services.Authenticate
{
    public class TokenManager : ITokenManager
    {
        private readonly SsoSettings _ssoSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private TokenResponse? _accessToken;
        private DateTime _expirationTime;

        public TokenManager(IOptions<SsoSettings> ssoOptions, IHttpClientFactory httpClientFactory)
        {
            _ssoSettings = ssoOptions.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TokenResponse> GetAccessToken(bool force = false)
        {
            if (_accessToken != null && DateTime.Now < _expirationTime && !force)
            {
                return _accessToken;
            }

            if (_accessToken != null && DateTime.Now >= _expirationTime)
            {
                _accessToken = await RenewToken(_accessToken);
            }
            else
            {
                _accessToken = await GetNewToken();
            }

            if (_accessToken != null && !_accessToken.IsError)
            {
                _expirationTime = DateTime.Now.AddSeconds(_accessToken.ExpiresIn);
            }

            return _accessToken ?? throw new InvalidOperationException("Could not retrieve or renew access token.");
        }

        private async Task<TokenResponse> RenewToken(TokenResponse currentToken)
        {
            var client = _httpClientFactory.CreateClient();
            var discoveryDoc = await client.GetDiscoveryDocumentAsync(_ssoSettings.AuthorityUrl);
            if (discoveryDoc.IsError) throw new Exception(discoveryDoc.Error);

            var refreshTokenRequest = new RefreshTokenRequest
            {
                Address = discoveryDoc.TokenEndpoint,
                ClientId = _ssoSettings.ClientId,
                ClientSecret = _ssoSettings.ClientSecret,
                RefreshToken = currentToken.RefreshToken
            };

            return await client.RequestRefreshTokenAsync(refreshTokenRequest);
        }

        private async Task<TokenResponse> GetNewToken()
        {
            var client = _httpClientFactory.CreateClient();
            var discoveryDoc = await client.GetDiscoveryDocumentAsync(_ssoSettings.AuthorityUrl);
            if (discoveryDoc.IsError) throw new Exception(discoveryDoc.Error);

            var tokenRequest = new PasswordTokenRequest
            {
                Address = discoveryDoc.TokenEndpoint,
                ClientId = _ssoSettings.ClientId,
                ClientSecret = _ssoSettings.ClientSecret,
                GrantType = _ssoSettings.GrantType,
                UserName = _ssoSettings.UserName,
                Password = _ssoSettings.Password,
                Scope = _ssoSettings.Scope
            };

            return await client.RequestPasswordTokenAsync(tokenRequest);
        }
    }
}
