using Microsoft.Extensions.Caching.Memory;
using Providers.ProviderOne;
using Providers.ProviderTwo;

namespace Providers;

public class SearchService : ISearchService
{
    private const int CacheExpTimeInMinutes = 5;
    
    private readonly IProvider<ProviderOneSearchRequest, ProviderOneSearchResponse> _providerOneClient;
    private readonly IProvider<ProviderTwoSearchRequest, ProviderTwoSearchResponse> _providerTwoClient;
    private readonly IMemoryCache _cache;

    public SearchService(IMemoryCache cache,
        IProvider<ProviderOneSearchRequest, ProviderOneSearchResponse> providerOneClient,
        IProvider<ProviderTwoSearchRequest, ProviderTwoSearchResponse> providerTwoClient)
    {
        _cache = cache;
        _providerOneClient = providerOneClient;
        _providerTwoClient = providerTwoClient;
    }

    public async Task<SearchResponse?> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _cache.TryGetValue(request.ToJson(), out SearchResponse? cachedValue);

        var isOnlyCached = request.Filters?.OnlyCached ?? false;
        if (cachedValue != null || isOnlyCached)
            return cachedValue;

        var providerOneRoutes = (await _providerOneClient.GetRoutesAsync(new ProviderOneSearchRequest
        {
            DateFrom = request.OriginDateTime,
            DateTo = request.Filters?.DestinationDateTime,
            From = request.Origin,
            To = request.Destination,
            MaxPrice = request.Filters?.MaxPrice,
        }, cancellationToken))?.Routes ?? Array.Empty<ProviderOneRoute>();

        var providerTwoRoutes = (await _providerTwoClient.GetRoutesAsync(new ProviderTwoSearchRequest
        {
            Departure = request.Origin,
            Arrival = request.Destination,
            DepartureDate = request.OriginDateTime,
            MinTimeLimit = request.Filters?.MinTimeLimit,
        }, cancellationToken))?.Routes ?? Array.Empty<ProviderTwoRoute>();

        var routes = providerOneRoutes.Select(r => new Route
            {
                Id = Guid.NewGuid(),
                Destination = r.To,
                Origin = r.From,
                OriginDateTime = r.DateFrom,
                DestinationDateTime = r.DateTo,
                Price = r.Price,
                TimeLimit = r.TimeLimit,
            }).Concat(providerTwoRoutes.Select(r => new Route
            {
                Id = Guid.NewGuid(),
                Origin = r.Departure.Point,
                Destination = r.Arrival.Point,
                OriginDateTime = r.Departure.Date,
                DestinationDateTime = r.Arrival.Date,
                Price = r.Price,
                TimeLimit = r.TimeLimit,
            })).Where(r => r.TimeLimit < DateTime.UtcNow) // assuming that we are working with utc datetimes
            .DistinctBy(r => new { r.Origin, r.OriginDateTime, r.Destination, r.DestinationDateTime, r.Price })
            .ToArray();

        var response = new SearchResponse
        {
            Routes = routes,
            MinPrice = routes.Min(r => r.Price),
            MaxPrice = routes.Max(r => r.Price),
            MinMinutesRoute = routes.Min(r => (r.DestinationDateTime - r.OriginDateTime).Minutes),
            MaxMinutesRoute = routes.Max(r => (r.DestinationDateTime - r.OriginDateTime).Minutes),
        };

        return _cache.Set(request.ToJson(), response, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(CacheExpTimeInMinutes));
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        return (
            await Task.WhenAll(_providerOneClient.IsAvailableAsync(cancellationToken),
                _providerTwoClient.IsAvailableAsync(cancellationToken))
        ).Any(a => a);
    }
}