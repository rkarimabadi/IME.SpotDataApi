// File: IME.SpotDataApi/Services/Caching/MemoryCacheService.cs
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace IME.SpotDataApi.Services.Caching
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int expirationInMinutes = 5)
        {
            // تلاش برای دریافت داده از کش
            if (_memoryCache.TryGetValue(key, out T value))
            {
                return value;
            }

            // اگر داده در کش نبود، آن را از منبع اصلی (factory) ایجاد می‌کنیم
            var result = await factory();

            // تنظیمات انقضای کش
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationInMinutes)
            };

            // ذخیره نتیجه در کش
            _memoryCache.Set(key, result, cacheEntryOptions);

            return result;
        }
    }
}
