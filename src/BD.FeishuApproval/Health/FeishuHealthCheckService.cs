using BD.FeishuApproval.Abstractions.Configs;
using BD.FeishuApproval.Abstractions.Health;
using BD.FeishuApproval.Abstractions.Http;
using BD.FeishuApproval.Abstractions.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Health;

/// <summary>
/// 飞书系统健康检查服务实现
/// </summary>
public class FeishuHealthCheckService : IFeishuHealthCheckService
{
    private readonly IFeishuApprovalRepository _repository;
    private readonly IFeishuConfigProvider _configProvider;
    private readonly IFeishuApiClient _apiClient;
    private readonly ILogger<FeishuHealthCheckService> _logger;

    public FeishuHealthCheckService(
        IFeishuApprovalRepository repository,
        IFeishuConfigProvider configProvider,
        IFeishuApiClient apiClient,
        ILogger<FeishuHealthCheckService> logger)
    {
        _repository = repository;
        _configProvider = configProvider;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FeishuHealthCheckResult> CheckHealthAsync()
    {
        var result = new FeishuHealthCheckResult();
        
        try
        {
            // 并行检查各组件
            var databaseTask = CheckDatabaseAsync();
            var apiTask = CheckFeishuApiAsync();
            var configTask = CheckConfigurationAsync();

            await Task.WhenAll(databaseTask, apiTask, configTask);

            result.DatabaseStatus = await databaseTask;
            result.FeishuApiStatus = await apiTask;
            result.ConfigurationStatus = await configTask;

            // 计算整体状态
            result.OverallStatus = CalculateOverallStatus(
                result.DatabaseStatus,
                result.FeishuApiStatus,
                result.ConfigurationStatus);

            result.Details = GenerateHealthDetails(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            result.OverallStatus = HealthStatus.Unhealthy;
            result.DatabaseStatus = HealthStatus.Unhealthy;
            result.FeishuApiStatus = HealthStatus.Unhealthy;
            result.ConfigurationStatus = HealthStatus.Unhealthy;
            result.Details = $"Health check failed: {ex.Message}";
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<HealthStatus> CheckDatabaseAsync()
    {
        try
        {
            // 尝试获取配置以测试数据库连接
            var config = await _repository.GetAccessConfigAsync();
            _logger.LogDebug("Database health check passed");
            return HealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            return HealthStatus.Unhealthy;
        }
    }

    /// <inheritdoc />
    public async Task<HealthStatus> CheckFeishuApiAsync()
    {
        try
        {
            var config = _configProvider.GetApiOptions();
            
            if (string.IsNullOrEmpty(config.AppId) || string.IsNullOrEmpty(config.AppSecret))
            {
                _logger.LogWarning("Feishu API configuration is incomplete");
                return HealthStatus.Degraded;
            }

            // 尝试获取访问令牌来测试API连接
            using var request = new HttpRequestMessage(HttpMethod.Post, "/open-apis/auth/v3/tenant_access_token/internal");
            request.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { app_id = config.AppId, app_secret = config.AppSecret }),
                System.Text.Encoding.UTF8,
                "application/json");
                
            var response = await _apiClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Feishu API health check passed");
                return HealthStatus.Healthy;
            }
            else
            {
                _logger.LogWarning("Feishu API health check failed with status code: {StatusCode}", response.StatusCode);
                return HealthStatus.Unhealthy;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feishu API health check failed");
            return HealthStatus.Unhealthy;
        }
    }

    /// <inheritdoc />
    public Task<HealthStatus> CheckConfigurationAsync()
    {
        try
        {
            var config = _configProvider.GetApiOptions();
            
            if (string.IsNullOrEmpty(config.AppId))
            {
                return Task.FromResult(HealthStatus.Unhealthy);
            }
            
            if (string.IsNullOrEmpty(config.AppSecret))
            {
                return Task.FromResult(HealthStatus.Unhealthy);
            }
            
            _logger.LogDebug("Configuration health check passed");
            return Task.FromResult(HealthStatus.Healthy);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Configuration health check failed");
            return Task.FromResult(HealthStatus.Unhealthy);
        }
    }

    private static HealthStatus CalculateOverallStatus(params HealthStatus[] statuses)
    {
        var hasUnhealthy = false;
        var hasDegraded = false;

        foreach (var status in statuses)
        {
            switch (status)
            {
                case HealthStatus.Unhealthy:
                    hasUnhealthy = true;
                    break;
                case HealthStatus.Degraded:
                    hasDegraded = true;
                    break;
            }
        }

        if (hasUnhealthy) return HealthStatus.Unhealthy;
        if (hasDegraded) return HealthStatus.Degraded;
        return HealthStatus.Healthy;
    }

    private static string GenerateHealthDetails(FeishuHealthCheckResult result)
    {
        return $"Database: {result.DatabaseStatus}, " +
               $"Feishu API: {result.FeishuApiStatus}, " +
               $"Configuration: {result.ConfigurationStatus}";
    }
}
