using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateways.Services;

public class HealthCheckService : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public HealthCheckService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var healthCheckResults = new ConcurrentDictionary<string, object>();
        var overallHealthy = true;
        var task = new List<Task>();

        var clusters = _configuration.GetSection("ReverseProxy:Clusters").GetChildren();

        foreach (var cluster in clusters)
        {
            var destinations = cluster.GetSection("Destinations").GetChildren();

            foreach (var destination in destinations)
            {
                var address = destination["Address"];
                if (!string.IsNullOrEmpty(address))
                {
                    task.Add(Task.Run(async () =>
                    {
                        try
                        {
                            using var client = _httpClientFactory.CreateClient();
                            client.Timeout = TimeSpan.FromSeconds(5);

                            var response = await client.GetAsync($"{address}health", cancellationToken);

                            if (response.IsSuccessStatusCode)
                            {
                                healthCheckResults[cluster.Key] = "Healthy";
                            }
                            else
                            {
                                healthCheckResults[cluster.Key] = $"Unhealthy - {response.StatusCode}";
                                overallHealthy = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            healthCheckResults[cluster.Key] = $"Unhealthy - {ex.Message}";
                            overallHealthy = false;
                        }
                    }, cancellationToken));

                }
            }
        }

        await Task.WhenAll(task);

        return overallHealthy
            ? HealthCheckResult.Healthy("All services are healthy", healthCheckResults)
            : HealthCheckResult.Unhealthy("Some services are unhealthy", data: healthCheckResults);
    }
}