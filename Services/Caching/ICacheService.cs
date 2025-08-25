namespace IME.SpotDataApi.Services.Caching
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int expirationInMinutes = 5);
    }
}
