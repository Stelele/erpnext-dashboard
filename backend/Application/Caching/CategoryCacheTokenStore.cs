using System.Collections.Concurrent;

namespace Application.Caching;

public class CategoryCacheTokenStore
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new();

    public CancellationToken GetToken(string category)
    {
        return _tokens.GetOrAdd(category, _ => new CancellationTokenSource()).Token;
    }

    public void Invalidate(string category)
    {
        if (_tokens.TryRemove(category, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
