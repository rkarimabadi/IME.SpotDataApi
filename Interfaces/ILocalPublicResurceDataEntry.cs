namespace IME.SpotDataApi.Interfaces
{
    public interface ILocalPublicResurceDataEntry<T>
    {
        Task SaveDataAsync(List<T> result);
    }
}
