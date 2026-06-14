using Application.Users;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Application.Caching;

public class CachePipelineBehavior<TRequest, TResponse>(
    IMemoryCache cache,
    CategoryCacheTokenStore tokenStore,
    IUserContext userContext
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (typeof(TRequest).GetCustomAttributes(typeof(CacheAttribute), false).FirstOrDefault() is CacheAttribute cacheAttr)
        {
            var key = BuildKey(cacheAttr.KeyPrefix, request, userContext.UserId);
            if (cache.TryGetValue<TResponse>(key, out var cached) && cached is not null)
                return cached;

            var result = await next();
            if (result is not null)
            {
                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheAttr.DurationMinutes));
                options.ExpirationTokens.Add(new CancellationChangeToken(tokenStore.GetToken(cacheAttr.KeyPrefix)));

                cache.Set(key, result, options);
            }
            return result;
        }

        var commandResult = await next();
        foreach (var attr in typeof(TRequest).GetCustomAttributes(typeof(InvalidateCacheAttribute), false))
        {
            tokenStore.Invalidate(((InvalidateCacheAttribute)attr).Category);
        }
        return commandResult;
    }

    private static string BuildKey(string prefix, object request, Guid userId)
    {
        var values = request.GetType().GetProperties()
            .OrderBy(p => p.Name)
            .Select(p => p.GetValue(request) switch
            {
                null => "",
                Guid[] ids => string.Join(",", ids.OrderBy(id => id.ToString())),
                Guid id => id.ToString(),
                string s => s,
                var v => v.ToString() ?? ""
            });
        return $"{prefix}:{userId}:" + string.Join(":", values);
    }
}
