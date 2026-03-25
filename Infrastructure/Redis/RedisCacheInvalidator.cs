using System.Text.Json;
using CollabDocs.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace CollabDocs.Infrastructure.Redis;

public class RedisCacheInvalidator : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IMemoryCache _cache;

    public RedisCacheInvalidator(IConnectionMultiplexer redis, IMemoryCache cache)
    {
        _redis = redis;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();
        await subscriber.SubscribeAsync("doc_state_update", (channel, value) =>
        {
            if (value.HasValue)
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<DocumentStateUpdatePayload>(value.ToString()!);
                    if (payload != null)
                    {
                        var cacheKey = $"doc_state_{payload.DocumentId}";
                        var docState = new CachedDocumentState(payload.Content, payload.VersionNumber);
                        
                        // We set the updated cache directly into memory so other servers don't hit Postgres!
                        _cache.Set(cacheKey, docState, TimeSpan.FromHours(1));
                    }
                }
                catch (Exception)
                {
                    // Ignore malformed messages occasionally in pub/sub
                }
            }
        });
    }
}

public record DocumentStateUpdatePayload(Guid DocumentId, string Content, int VersionNumber);
