namespace IME.SpotDataApi.Models.General
{
    public class ResponseItem<T> : ILinkContaining
    {
        private List<Link> _links;
        public T Item { get; set; }

        public List<Link> Links
        {
            get => _links ??= new List<Link>();
            set => _links = value;
        }

        public void AddLink(Link link)
        {
            Links.Add(link);
        }
    }
}