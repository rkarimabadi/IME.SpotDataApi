using IME.SpotDataApi.Models.Spot;

namespace IME.SpotDataApi.Interfaces
{
    public interface IOfferRepository
    {
        Task<IEnumerable<Offer>> GetOffersAsync(int pageNumber, int pageSize);
        Task<Offer?> GetOfferByIdAsync(int id);
    }
}
