using System.Collections.Concurrent;
using HackerNewsBestStories.Api.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace HackerNewsBestStories.Api.Services;

public class BestStoriesService(IHackerNewsClient client, HybridCache cache)
{
    // beststories.json devuelve como máximo 200 ids
    public const int MaxStories = 200;

    private static readonly HybridCacheEntryOptions IdsCacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    private static readonly HybridCacheEntryOptions StoryCacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    public async Task<List<Story>> GetBestStoriesAsync(int n, CancellationToken cancellationToken)
    {
        // HybridCache hace que si llegan muchas peticiones a la vez con la caché vacía,
        // solo una llame a Hacker News y el resto espere a ese resultado.
        var ids = await cache.GetOrCreateAsync(
            "beststories",
            async token => await client.GetBestStoryIdsAsync(token),
            IdsCacheOptions,
            cancellationToken: cancellationToken);

        // El orden de beststories no es exactamente por score, así que pido todas
        // las historias y las ordeno yo. Como mucho 50 llamadas a la vez a HN.
        var items = new ConcurrentBag<HackerNewsItem>();
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 50, CancellationToken = cancellationToken };

        await Parallel.ForEachAsync(ids, parallelOptions, async (id, token) =>
        {
            var item = await cache.GetOrCreateAsync(
                $"item-{id}",
                async t => await client.GetItemAsync(id, t),
                StoryCacheOptions,
                cancellationToken: token);

            if (item is { Type: "story", Deleted: false, Dead: false })
            {
                items.Add(item);
            }
        });

        return items
            .OrderByDescending(i => i.Score)
            .ThenBy(i => i.Id)
            .Take(n)
            .Select(ToStory)
            .ToList();
    }

    private static Story ToStory(HackerNewsItem item)
    {
        return new Story(
            Title: item.Title ?? "",
            // Los "Ask HN" no tienen url, en ese caso devuelvo el enlace a la discusión
            Uri: item.Url ?? $"https://news.ycombinator.com/item?id={item.Id}",
            PostedBy: item.By ?? "",
            Time: DateTimeOffset.FromUnixTimeSeconds(item.Time),
            Score: item.Score,
            CommentCount: item.Descendants);
    }
}
