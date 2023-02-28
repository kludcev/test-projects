namespace Providers;

public interface IProvider<TReq, TRes> where TReq : class
    where TRes : class
{
    Task<TRes?> GetRoutesAsync(TReq request, CancellationToken cancellationToken);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}