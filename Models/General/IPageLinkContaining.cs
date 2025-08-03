namespace IME.SpotDataApi.Models.General
{
    public interface IPageLinkContaining : ILinkContaining
    {
        int PageNumber { get; set; }
        int PageCount { get; set; }
    }
}