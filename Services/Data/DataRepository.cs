using IME.SpotDataApi.Data;
using IME.SpotDataApi.Interfaces;
using IME.SpotDataApi.Models.Public;
using Microsoft.EntityFrameworkCore;

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

            using var context = _dbContextFactory.CreateDbContext();
            var dbSet = context.Set<T>();

            foreach (var entity in entities)
            {
                var existingEntity = await dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entity.Id);

                if (entity != null && !context.Set<T>().Any(a => a == entity))
                {
                    context.Set<T>().Add(entity);
                    await context.SaveChangesAsync();
                }
            }
            try
            {
            } catch(Exception e)
            {
               Console.WriteLine(e.Message);
            }
        }
    }
}
