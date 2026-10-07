using HackerNewsBestStories.Api.Models;

namespace HackerNewsBestStories.Api.Services;

public interface IHackerNewsClient
{
    Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken);
    Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken);
}

public class HackerNewsClient(HttpClient httpClient) : IHackerNewsClient
{
    public async Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<int[]>("beststories.json", cancellationToken) ?? [];
    }

    public async Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        // Si el item no existe, HN devuelve "null"
        return await httpClient.GetFromJsonAsync<HackerNewsItem>($"item/{id}.json", cancellationToken);
    }
}
