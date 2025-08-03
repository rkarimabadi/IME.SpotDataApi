using System.Security.Cryptography.X509Certificates;

namespace IME.SpotDataApi.Helpers
{
    public class CustomHttpMessageHandler : HttpClientHandler
    {
        public CustomHttpMessageHandler()
        {
            // Set the ServerCertificateCustomValidationCallback property to a lambda expression that always returns true
            ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) => true;
        }
    }
}
