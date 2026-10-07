namespace HackerNewsBestStories.Api.Models;

// Lo que devuelve nuestra API
public record Story(
    string Title,
    string Uri,
    string PostedBy,
    DateTimeOffset Time,
    int Score,
    int CommentCount
);
