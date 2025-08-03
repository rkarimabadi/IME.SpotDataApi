namespace IME.SpotDataApi.Interfaces
{
    public interface IRemoreOperationalResurceService<T>
    {
        string EndPointPath { get; set; }
        Task<IEnumerable<T>> RetrieveAsync(DateTime date);
        Task<IEnumerable<T>> RetrieveAsync(DateTime fromDate, DateTime toDate);
    }
}