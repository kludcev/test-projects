using Microsoft.Extensions.Logging;

namespace Providers.ProviderOne;

public class ProviderOneClient : ProviderBase<ProviderOneSearchRequest, ProviderOneSearchResponse>
{
    public ProviderOneClient(ILogger logger) : base(baseUrl: "http://provider-one/api/v1", logger)
    {
    }
}