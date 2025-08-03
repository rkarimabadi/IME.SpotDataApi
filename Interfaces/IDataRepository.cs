using IME.SpotDataApi.Models.Public;

namespace IME.SpotDataApi.Interfaces
{
    public interface IDataRepository<T> where T : RootObj<int>
    {
        Task UpsertAsync(IEnumerable<T> entities);
    }
}
