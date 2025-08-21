using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.General;
using IME.SpotDataApi.Services.Authenticate;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace IME.SpotDataApi.Services.RemoteData
{
    public class OperationalResurceService<T> : IRemoreOperationalResurceService<T> where T : class
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenManager _tokenManager;
        private readonly IDateHelper _dateHelper;
        private readonly JsonSerializerOptions _jsonOptions;
        public string EndPointPath { get; set; }

        public OperationalResurceService(
            ITokenManager tokenManager,
            IDateHelper dateHelper,
            IHttpClientFactory httpClientFactory,
            IOptions<ApiEndpoints> apiEndpoints)
        {
            _tokenManager = tokenManager;
            _dateHelper = dateHelper;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(apiEndpoints.Value.OperationalApi);
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<IEnumerable<T>> RetrieveAsync(DateTime date)
        {
            return await RetrieveDataFromRemoteApiAsync(date);
        }
        public async Task<IEnumerable<T>> RetrieveAsync(DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate) return Enumerable.Empty<T>();

            var allResults = new List<T>();
            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                try
                {
                    var dailyResult = await RetrieveDataFromRemoteApiAsync(date);
                    allResults.AddRange(dailyResult);
                }
                catch (Exception ex)
                {
                    // Log the exception for the specific day and continue
                    Console.WriteLine($"Failed to retrieve data for {date:yyyy-MM-dd}. Error: {ex.Message}");
                }
            }
            return allResults;
        }

        private static readonly Random _random = new Random();
        private async Task<IEnumerable<T>> RetrieveDataFromRemoteApiAsync(DateTime date)
        {
            var stoppingToken  = new CancellationToken();
            int delayInSeconds = _random.Next(3, 5);
            await Task.Delay(TimeSpan.FromMinutes(delayInSeconds), stoppingToken);
            var persianDate = _dateHelper.GetPersianYYYYMMDD(date);
            var requestUrl = $"{EndPointPath}?persianDate={persianDate}&pageNumber=1&pageSize=1000&Language=fa";
            var items = new List<T>();

            var token = await _tokenManager.GetAccessToken();
            if (token.IsError)
            {
                throw new InvalidOperationException("Failed to get access token.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            do
            {
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<PagedDataInquiryResponse<T>>(_jsonOptions);
                if (result?.Items == null) break;

                items.AddRange(result.Items.Select(s => s.Item));

                var nextPageLink = result.Links?.FirstOrDefault(l => l.Rel.Contains("nextPage"));
                requestUrl = nextPageLink?.Href;

            } while (!string.IsNullOrEmpty(requestUrl));

            return items;
        }
    }
}
