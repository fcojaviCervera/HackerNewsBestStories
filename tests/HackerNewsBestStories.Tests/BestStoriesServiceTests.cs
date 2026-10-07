using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace HackerNewsBestStories.Tests;

public class BestStoriesServiceTests
{
    private readonly FakeHackerNewsClient _client = new();
    private readonly BestStoriesService _service;

    public BestStoriesServiceTests()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var cache = services.BuildServiceProvider().GetRequiredService<HybridCache>();

        _service = new BestStoriesService(_client, cache);
    }

    [Fact]
    public async Task ReturnsTheBestNStoriesOrderedByScore()
    {
        _client.AddStory(1, score: 50);
        _client.AddStory(2, score: 300);
        _client.AddStory(3, score: 10);
        _client.AddStory(4, score: 120);

        var stories = await _service.GetBestStoriesAsync(3, CancellationToken.None);

        Assert.Equal([300, 120, 50], stories.Select(s => s.Score));
    }

    [Fact]
    public async Task IgnoresDeletedDeadAndNonStoryItems()
    {
        _client.AddStory(1, score: 10);
        _client.AddItem(2, null);
        _client.AddItem(3, new HackerNewsItem { Id = 3, Type = "story", Deleted = true, Score = 500 });
        _client.AddItem(4, new HackerNewsItem { Id = 4, Type = "story", Dead = true, Score = 400 });
        _client.AddItem(5, new HackerNewsItem { Id = 5, Type = "job", Score = 300 });

        var stories = await _service.GetBestStoriesAsync(10, CancellationToken.None);

        Assert.Single(stories);
        Assert.Equal("Story 1", stories[0].Title);
    }

    [Fact]
    public async Task MapsTheFieldsCorrectly()
    {
        _client.AddItem(1, new HackerNewsItem
        {
            Id = 1,
            Type = "story",
            Title = "A uBlock Origin update was rejected from the Chrome Web Store",
            Url = "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
            By = "ismaildonmez",
            Time = 1570887781,
            Score = 1716,
            Descendants = 572
        });

        var story = (await _service.GetBestStoriesAsync(1, CancellationToken.None)).Single();

        Assert.Equal("A uBlock Origin update was rejected from the Chrome Web Store", story.Title);
        Assert.Equal("https://github.com/uBlockOrigin/uBlock-issues/issues/745", story.Uri);
        Assert.Equal("ismaildonmez", story.PostedBy);
        Assert.Equal(DateTimeOffset.Parse("2019-10-12T13:43:01+00:00"), story.Time);
        Assert.Equal(1716, story.Score);
        Assert.Equal(572, story.CommentCount);
    }

    [Fact]
    public async Task StoryWithoutUrlLinksToHackerNews()
    {
        _client.AddItem(42, new HackerNewsItem { Id = 42, Type = "story", Title = "Ask HN: something" });

        var story = (await _service.GetBestStoriesAsync(1, CancellationToken.None)).Single();

        Assert.Equal("https://news.ycombinator.com/item?id=42", story.Uri);
    }

    [Fact]
    public async Task SecondCallIsServedFromCache()
    {
        _client.AddStory(1, score: 10);
        _client.AddStory(2, score: 20);

        await _service.GetBestStoriesAsync(2, CancellationToken.None);
        await _service.GetBestStoriesAsync(2, CancellationToken.None);

        // 1 llamada para los ids + 1 por cada historia, solo la primera vez
        Assert.Equal(3, _client.Calls);
    }
}

// Cliente falso para no llamar a la API real en los tests
public class FakeHackerNewsClient : IHackerNewsClient
{
    private readonly Dictionary<int, HackerNewsItem?> _items = new();
    private int _calls;

    public int Calls => _calls;

    public void AddItem(int id, HackerNewsItem? item) => _items[id] = item;

    public void AddStory(int id, int score) => AddItem(id, new HackerNewsItem { Id = id, Type = "story", Title = $"Story {id}", Score = score });

    public Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        return Task.FromResult(_items.Keys.ToArray());
    }

    public Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        return Task.FromResult(_items[id]);
    }
}
