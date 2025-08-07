using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.General;
using IME.SpotDataApi.Models.Notification;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;

namespace IME.SpotDataApi.Services.RemoteData
{
    public class NotificationService<T> : IRemoreOperationalResurceService<T> where T : class
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        public string EndPointPath { get; set; } = "api/Notifications/NewsNotificationsByDate";
        public string spotNotificationEndPointPath { get; set; } = "/api/Notifications/SpotNotificationsByDate";

        public NotificationService(IHttpClientFactory httpClientFactory, IOptions<ApiEndpoints> apiEndpoints)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(apiEndpoints.Value.NotificationApi);

            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<IEnumerable<T>> RetrieveAsync(DateTime date)
        {
            return await RetrieveAsync(date, date);
        }
        public async Task<IEnumerable<T>> RetrieveSpotNotoficationsAsync(DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
            {
                return Enumerable.Empty<T>();
            }

            var requestPayload = new NotificationRequest
            {
                FromDate = fromDate.ToString("yyyy-MM-dd", CultureInfo.GetCultureInfo("en-US")),
                ToDate = toDate.ToString("yyyy-MM-dd", CultureInfo.GetCultureInfo("en-US")),
                PageNumber = 1,
                PageSize = 1000
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(spotNotificationEndPointPath, requestPayload);
                response.EnsureSuccessStatusCode(); // Throws an exception if the response is not successful

                var result = await response.Content.ReadFromJsonAsync<NotificationResponse<T>>(_jsonOptions);
                return result?.Data ?? Enumerable.Empty<T>();
            }
            catch (HttpRequestException ex)
            {
                // Log the exception (using a proper logging framework)
                Console.WriteLine($"Error fetching spot notifications: {ex.Message}");
                return Enumerable.Empty<T>();
            }
        }

        public async Task<IEnumerable<T>> RetrieveAsync(DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
            {
                return Enumerable.Empty<T>();
            }

            var requestPayload = new NotificationRequest
            {
                FromDate = fromDate.ToString("yyyy-MM-dd", CultureInfo.GetCultureInfo("en-US")),
                ToDate = toDate.ToString("yyyy-MM-dd", CultureInfo.GetCultureInfo("en-US")),
                PageNumber = 1,
                PageSize = 1000
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(EndPointPath, requestPayload);
                response.EnsureSuccessStatusCode(); // Throws an exception if the response is not successful

                var result = await response.Content.ReadFromJsonAsync<NotificationResponse<T>>(_jsonOptions);
                return result?.Data ?? Enumerable.Empty<T>();
            }
            catch (HttpRequestException ex)
            {
                // Log the exception (using a proper logging framework)
                Console.WriteLine($"Error fetching notifications: {ex.Message}");
                return Enumerable.Empty<T>();
            }
        }
    }
}
