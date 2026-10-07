namespace HackerNewsBestStories.Api.Models;

// Item tal y como lo devuelve https://hacker-news.firebaseio.com/v0/item/{id}.json
public class HackerNewsItem
{
    public int Id { get; set; }
    public string? Type { get; set; }
    public string? By { get; set; }
    public long Time { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public int Score { get; set; }
    public int Descendants { get; set; }
    public bool Deleted { get; set; }
    public bool Dead { get; set; }
}
