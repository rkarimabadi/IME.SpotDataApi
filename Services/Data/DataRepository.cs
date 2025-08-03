using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace IME.SpotDataApi.Services.Data
{
    public class DataRepository<T> : IDataRepository<T> where T : RootObj<int>
    {
        private readonly IDbContextFactory<AppDataContext> _dbContextFactory;

        public DataRepository(IDbContextFactory<AppDataContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task UpsertAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
            {
                return;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var dbSet = context.Set<T>();

            var primaryKeyProperties = context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties;

            // حالت اول: موجودیت کلید اصلی ندارد (مانند TradeReport)
            // در این حالت، همه را به عنوان رکورد جدید اضافه می‌کنیم.
            if (primaryKeyProperties == null || !primaryKeyProperties.Any())
            {
                await dbSet.AddRangeAsync(entities);
            }
            // حالت دوم: موجودیت کلید اصلی دارد
            else
            {
                foreach (var entity in entities)
                {
                    // مقادیر کلید اصلی را از موجودیت فعلی استخراج می‌کنیم
                    var keyValues = primaryKeyProperties
                        .Select(p => p.PropertyInfo.GetValue(entity))
                        .ToArray();

                    // با استفاده از کلید، موجودیت را در دیتابیس پیدا می‌کنیم
                    var existingEntity = await dbSet.FindAsync(keyValues);

                    if (existingEntity != null)
                    {
                        // اگر موجود بود، مقادیر آن را با مقادیر جدید به‌روزرسانی می‌کنیم
                        context.Entry(existingEntity).CurrentValues.SetValues(entity);
                    }
                    else
                    {
                        // اگر موجود نبود، آن را به عنوان رکورد جدید اضافه می‌کنیم
                        dbSet.Add(entity);
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
