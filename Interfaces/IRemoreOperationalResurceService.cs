namespace IME.SpotDataApi.Interfaces
{
    public interface IRemoreOperationalResurceService<T>
    {
        string EndPointPath { get; set; }
        Task<IEnumerable<T>> RetrieveAsync(DateTime date);
        Task<IEnumerable<T>> RetrieveAsync(DateTime fromDate, DateTime toDate);
        Task<IEnumerable<T>> RetrieveSpotNotoficationsAsync(DateTime fromDate, DateTime toDate);
    }
}