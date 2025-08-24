// File: IME.SpotDataApi/Services/RemoteData/OperationalResurceService.cs

using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Configuration;
using IME.SpotDataApi.Models.General;
using IME.SpotDataApi.Models.Spot; // <-- افزوده شد
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

        // --- بخش مدیریت Rate Limiting ---
        // برای Offer: حداکثر 20 درخواست در دقیقه
        private static readonly object _offerRateLock = new object();
        private static int _offerRequestCount = 0;
        private static DateTime _offerWindowStart = DateTime.UtcNow;

        // برای TradeReport: حداکثر 20 درخواست در ساعت
        private static readonly object _tradeReportRateLock = new object();
        private static int _tradeReportRequestCount = 0;
        private static DateTime _tradeReportWindowStart = DateTime.UtcNow;
        // --- پایان بخش Rate Limiting ---

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

        private async Task<IEnumerable<T>> RetrieveDataFromRemoteApiAsync(DateTime date)
        {
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
                // اعمال قوانین محدودیت درخواست قبل از هر فراخوانی
                await ApplyRateLimitingAsync();

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

        private async Task ApplyRateLimitingAsync()
        {
            TimeSpan delay = TimeSpan.Zero;

            // قانون برای Offer: 20 درخواست در دقیقه
            if (typeof(T) == typeof(Offer))
            {
                lock (_offerRateLock)
                {
                    var now = DateTime.UtcNow;
                    if ((now - _offerWindowStart).TotalMinutes >= 1)
                    {
                        _offerWindowStart = now;
                        _offerRequestCount = 0;
                    }

                    if (_offerRequestCount >= 20)
                    {
                        delay = _offerWindowStart.AddMinutes(1) - now;
                        _offerWindowStart = _offerWindowStart.AddMinutes(1); // تنظیم پنجره بعدی
                        _offerRequestCount = 0;
                    }
                    _offerRequestCount++;
                }
            }
            // قانون برای TradeReport: 20 درخواست در ساعت
            else if (typeof(T) == typeof(TradeReport))
            {
                lock (_tradeReportRateLock)
                {
                    var now = DateTime.UtcNow;
                    if ((now - _tradeReportWindowStart).TotalHours >= 1)
                    {
                        _tradeReportWindowStart = now;
                        _tradeReportRequestCount = 0;
                    }

                    if (_tradeReportRequestCount >= 20)
                    {
                        delay = _tradeReportWindowStart.AddHours(1) - now;
                        _tradeReportWindowStart = _tradeReportWindowStart.AddHours(1); // تنظیم پنجره بعدی
                        _tradeReportRequestCount = 0;
                    }
                    _tradeReportRequestCount++;
                }
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }
        }
    }
}