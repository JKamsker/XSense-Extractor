using Amazon.Runtime;

namespace XSenseExtractor.Utils;

public class HttpClientFactoryWithSslDisabled : HttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        return new HttpClient(handler);
    }

    public override HttpClient CreateHttpClient(IClientConfig clientConfig)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        return new HttpClient(handler);
    }
}