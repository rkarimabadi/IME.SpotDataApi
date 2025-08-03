using IME.SpotDataApi.Models.Spot;

namespace IME.SpotDataApi.Interfaces
{
    public interface ITradeReportRepository
    {
        Task<IEnumerable<TradeReport>> GetTradeReportsAsync(int pageNumber, int pageSize);
        Task<TradeReport?> GetTradeReportByIdAsync(int id);
    }
}
