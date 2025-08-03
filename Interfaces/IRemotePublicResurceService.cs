namespace IME.SpotDataApi.Interfaces
{
    public interface IRemotePublicResurceService<T>
    {
        string EndPointPath { get; set; }

        Task<IEnumerable<T>> RetrieveResurcesDataAsync(bool force = false);
    }
}