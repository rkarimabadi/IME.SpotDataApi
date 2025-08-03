namespace IME.SpotDataApi.Models.General
{
    public interface ILinkContaining
    {
        List<Link> Links { get; set; }

        void AddLink(Link link);
    }
}