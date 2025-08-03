using IME.SpotDataApi.Models.Spot;

namespace IME.SpotDataApi.Interfaces
{
    public interface IBaseInfoService
    {
        Task<BaseInfoCount> CountAsync();
    }
}
