using AWSMonitor.Core.Interfaces;
using AWSMonitor.Core.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(IMetricsService metricsService, ILogger<MetricsController> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Metric>>> GetMetrics(
        [FromQuery] string? instanceIp,
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] int interval = 60)
    {
        try
        {
            var start = startTime ?? DateTime.UtcNow.AddHours(-1); // Default start time
            var end = endTime ?? DateTime.UtcNow; // Default end time
            var ip = instanceIp ?? "172.31.88.161"; // Default instance IP

            var metrics = await _metricsService.GetCPUMetrics(ip, start, end, interval);

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics");
            return StatusCode(500, "An error occurred while retrieving metrics");
        }
    }
}