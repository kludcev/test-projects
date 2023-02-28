using Microsoft.Extensions.Logging;

namespace Providers.ProviderTwo;

public class ProviderTwoClient : ProviderBase<ProviderTwoSearchRequest, ProviderTwoSearchResponse>
{
    public ProviderTwoClient(ILogger logger) : base(baseUrl: "http://provider-two/api/v1", logger)
    {
    }
}