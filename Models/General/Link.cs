namespace IME.SpotDataApi.Models.General
{
    /// <summary>
    /// لینک
    /// </summary>
    public class Link
    {
        /// <summary>
        /// لینک
        /// </summary>
        /// <param name="href">ارجاع</param>
        /// <param name="rel">نام لینک</param>
        /// <param name="method">متد</param>
        public Link(string href, string rel, string method)
        {
            Href = href;
            Rel = rel;
            Method = method;
        }

        /// <summary>
        /// ارجاع
        /// </summary>
        public string Rel { get; }

        /// <summary>
        /// نام لینک
        /// </summary>
        public string Href { get; }

        /// <summary>
        /// متد
        /// </summary>
        public string Method { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is Link))
            {
                return false;
            }

            Link input = (Link)obj;
            return Method == input.Method && Href == input.Href && Rel == input.Rel;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Rel, Href, Method);
        }
    }
}