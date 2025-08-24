using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Configuration;
using IME.SpotDataApi.Models.General;
using IME.SpotDataApi.Services.Authenticate;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace IME.SpotDataApi.Services.RemoteData
{
    public class PublicResurceService<T> : IRemotePublicResurceService<T> where T : class
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenManager _tokenManager;
        private readonly JsonSerializerOptions _jsonOptions;
        public string EndPointPath { get; set; } = "/v2/spot/Commodities";

        public PublicResurceService(
            ITokenManager tokenManager,
            IHttpClientFactory httpClientFactory,
            IOptions<ApiEndpoints> apiEndpoints)
        {
            _tokenManager = tokenManager;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(apiEndpoints.Value.OperationalApi);
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<IEnumerable<T>> RetrieveResurcesDataAsync(bool force = false)
        {
            var items = new List<T>();
            var requestUrl = $"{EndPointPath}?pageNumber=1&pageSize=1000&Language=fa";
            
            var token = await _tokenManager.GetAccessToken(force);
            if (token.IsError)
            {
                throw new InvalidOperationException("Failed to get access token.");
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            do
            {
                try
                {
                    var response = await _httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    var result = await response.Content.ReadFromJsonAsync<PagedDataInquiryResponse<T>>(_jsonOptions);
                    if (result?.Items == null) break;

                    items.AddRange(result.Items.Select(s => s.Item));

                    var nextPageLink = result.Links?.FirstOrDefault(l => l.Rel.Contains("nextPage"));
                    requestUrl = nextPageLink?.Href;

                    if (!string.IsNullOrEmpty(requestUrl))
                    {
                        // Use Task.Delay for non-blocking waits in async methods
                        await Task.Delay(3000);
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error fetching public resource: {ex.Message}");
                    // Decide if you want to break the loop or retry
                    break; 
                }

            } while (!string.IsNullOrEmpty(requestUrl));

            return items;
        }
    }     
}
