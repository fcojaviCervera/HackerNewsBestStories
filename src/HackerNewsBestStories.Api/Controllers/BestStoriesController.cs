using System.ComponentModel.DataAnnotations;
using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HackerNewsBestStories.Api.Controllers;

[ApiController]
[Route("api/beststories")]
[EnableRateLimiting("fixed")] 
public class BestStoriesController(BestStoriesService service, ILogger<BestStoriesController> logger) : ControllerBase
{
    // GET api/beststories?n=10
    // Si n no viene llega como 0, así que el Range también cubre ese caso (400)
    [HttpGet]
    public async Task<ActionResult<List<Story>>> Get([FromQuery, Range(1, BestStoriesService.MaxStories)] int n, CancellationToken cancellationToken)
    {
        try
        {
            return await service.GetBestStoriesAsync(n, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting stories from Hacker News");
            return Problem("Could not get the stories from Hacker News, try again later.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
