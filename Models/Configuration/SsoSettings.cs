namespace IME.SpotDataApi.Models.Configuration
{
    public class SsoSettings
    {
        public const string SectionName = "SsoSettings";

        public string AuthorityUrl { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string GrantType { get; set; } = "password";
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
    }
}
