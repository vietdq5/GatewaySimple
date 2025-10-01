using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateways.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(ILogger<GatewayController> logger)
    {
        _logger = logger;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    [HttpGet("routes")]
    [Authorize]
    public IActionResult GetRoutes()
    {
        var routes = new[]
        {
            new { Service = "Notifications", Path = "/api/notifications", Port = 5001 },
            new { Service = "Users", Path = "/api/users", Port = 5002 },
            new { Service = "Storage", Path = "/api/storage", Port = 5003 },
            new { Service = "Search", Path = "/api/search", Port = 5004 },
            new { Service = "Newsfeed", Path = "/api/newsfeed", Port = 5005 },
            new { Service = "Posts", Path = "/api/posts", Port = 5006 },
            new { Service = "Graph", Path = "/api/graph", Port = 5007 }
        };

        return Ok(routes);
    }
}