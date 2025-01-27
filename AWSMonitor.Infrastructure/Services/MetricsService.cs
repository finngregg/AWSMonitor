using Amazon.CloudWatch.Model;
using Amazon.CloudWatch;
using Amazon.Runtime;
using AWSMonitor.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon;
using Amazon.EC2.Model;
using Amazon.EC2;

public class MetricsService : IMetricsService
{
    private readonly IAmazonCloudWatch _cloudWatchClient;
    private readonly IAmazonEC2 _ec2Client;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(IConfiguration configuration, ILogger<MetricsService> logger)
    {
        _logger = logger;
        var credentials = new BasicAWSCredentials(
            configuration["AWS:AccessKey"],
            configuration["AWS:SecretKey"]);

        _cloudWatchClient = new AmazonCloudWatchClient(credentials, RegionEndpoint.USEast1);
        _ec2Client = new AmazonEC2Client(credentials, RegionEndpoint.USEast1);
    }

    public async Task<IEnumerable<AWSMonitor.Core.Models.Metric>> GetCPUMetrics(
        string instanceIp,
        DateTime startTime,
        DateTime endTime,
        int interval)
    {
        try
        {
            // Fetch instance ID - required for CloudWatch metrics
            var instanceRequest = new DescribeInstancesRequest
            {
                Filters = new List<Amazon.EC2.Model.Filter>
                {
                    new Amazon.EC2.Model.Filter
                    {
                        Name = "private-ip-address",
                        Values = new List<string> { instanceIp }
                    }
                }
            };

            var instanceResponse = await _ec2Client.DescribeInstancesAsync(instanceRequest);
            var instanceId = instanceResponse.Reservations
                .SelectMany(r => r.Instances)
                .FirstOrDefault()?.InstanceId;

            if (string.IsNullOrEmpty(instanceId))
            {
                _logger.LogError($"Could not find instance ID for IP: {instanceIp}");
                return new List<AWSMonitor.Core.Models.Metric>(); // Return empty list
            }

            _logger.LogInformation($"Found Instance ID: {instanceId} for IP: {instanceIp}");

            // Fetch metrics using instance ID
            var request = new GetMetricDataRequest
            {
                StartTimeUtc = startTime,
                EndTimeUtc = endTime,
                MetricDataQueries = new List<MetricDataQuery>
                {
                    new MetricDataQuery
                    {
                        Id = "cpu_usage",
                        MetricStat = new MetricStat
                        {
                            Metric = new Amazon.CloudWatch.Model.Metric
                            {
                                Namespace = "AWS/EC2",
                                MetricName = "CPUUtilization",
                                Dimensions = new List<Dimension>
                                {
                                    new Dimension
                                    {
                                        Name = "InstanceId",
                                        Value = instanceId
                                    }
                                }
                            },
                            Period = interval,
                            Stat = "Average"
                        }
                    }
                }
            };

            _logger.LogInformation($"Requesting metrics for Instance ID: {instanceId}");
            var response = await _cloudWatchClient.GetMetricDataAsync(request);

            var metrics = response.MetricDataResults
                .FirstOrDefault()
                ?.Timestamps
                .Zip(response.MetricDataResults[0].Values, (timestamp, value) =>
                    new AWSMonitor.Core.Models.Metric
                    {
                        Timestamp = timestamp,
                        Utilization = value,
                        InstanceId = instanceId
                    })
                .ToList() ?? new List<AWSMonitor.Core.Models.Metric>();

            _logger.LogInformation($"Retrieved {metrics.Count} metrics for Instance ID: {instanceId}");
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching metrics");
            throw;
        }
    }
}