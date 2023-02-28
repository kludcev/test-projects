using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Providers;

// контракты поставщиков могут меняться, поэтому в общем случае не рекомендуется выносить логику в базовый класс
// вынес, потому, что требования это позволяли и являются конечными
public abstract class ProviderBase<TReq, TRes> : IProvider<TReq, TRes> where TReq : class where TRes : class
{
    private static readonly HttpClient HttpClient = new();

    private readonly string _baseUrl;
    private readonly ILogger _logger;

    protected ProviderBase(string baseUrl, ILogger logger)
    {
        if (string.IsNullOrEmpty(baseUrl))
            throw new ArgumentException(nameof(baseUrl));

        _baseUrl = baseUrl;
        _logger = logger;
    }

    public async Task<TRes?> GetRoutesAsync(TReq request, CancellationToken cancellationToken)
    {
        var result = await ExecuteProviderCallResiliently(async () =>
        {
            var response = await HttpClient.PostAsJsonAsync($"{_baseUrl}/search", request,
                cancellationToken: cancellationToken);

            return response.Validate();
        });

        return result != null ? await result.ReadContentAs<TRes>() : null;
    }

    // можно и по другому, контракт из условий непонятный 
    // HTTP 200 if provider is ready
    //HTTP 500 if provider is down
    // c остальными кодами как?
    //Сделал фейл фаст, чтобы сразу понимать есть ли проблемы
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        var result = await ExecuteProviderCallResiliently(async () =>
        {
            var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/ping"),
                cancellationToken);

            return response;
        });

        return result?.IsSuccessStatusCode ?? false;
    }

    // наивный подход, просто для того, чтобы признать существование проблемы
    private async Task<HttpResponseMessage?> ExecuteProviderCallResiliently(Func<Task<HttpResponseMessage>> call)
    {
        HttpResponseMessage? result = null;
        try
        {
            result = await call();
        }
        catch (Exception e) when (e is HttpRequestException or HttpException)
        {
            _logger.Log(LogLevel.Error, e.Message);
        }

        return result;
    }
}