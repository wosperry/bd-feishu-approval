using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Health;

/// <summary>
/// 飞书系统健康检查服务接口
/// </summary>
public interface IFeishuHealthCheckService
{
    /// <summary>
    /// 检查系统整体健康状态
    /// </summary>
    /// <returns>健康检查结果</returns>
    Task<FeishuHealthCheckResult> CheckHealthAsync();
    
    /// <summary>
    /// 检查数据库连接
    /// </summary>
    /// <returns>数据库健康状态</returns>
    Task<HealthStatus> CheckDatabaseAsync();
    
    /// <summary>
    /// 检查飞书API连通性
    /// </summary>
    /// <returns>飞书API健康状态</returns>
    Task<HealthStatus> CheckFeishuApiAsync();
    
    /// <summary>
    /// 检查配置完整性
    /// </summary>
    /// <returns>配置健康状态</returns>
    Task<HealthStatus> CheckConfigurationAsync();
}

/// <summary>
/// 健康检查结果
/// </summary>
public class FeishuHealthCheckResult
{
    /// <summary>
    /// 整体健康状态
    /// </summary>
    public HealthStatus OverallStatus { get; set; }
    
    /// <summary>
    /// 数据库状态
    /// </summary>
    public HealthStatus DatabaseStatus { get; set; }
    
    /// <summary>
    /// 飞书API状态
    /// </summary>
    public HealthStatus FeishuApiStatus { get; set; }
    
    /// <summary>
    /// 配置状态
    /// </summary>
    public HealthStatus ConfigurationStatus { get; set; }
    
    /// <summary>
    /// 检查时间
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; }
}

/// <summary>
/// 健康状态枚举
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// 健康
    /// </summary>
    Healthy,
    
    /// <summary>
    /// 不健康
    /// </summary>
    Unhealthy,
    
    /// <summary>
    /// 降级（部分功能不可用）
    /// </summary>
    Degraded
}

/// <summary>
/// 飞书事件订阅验证响应DTO
/// </summary>
public class FeishuEventVerificationResponse
{
    /// <summary>
    /// 验证挑战值，用于飞书开放平台的URL验证
    /// </summary>
    [JsonPropertyName("challenge")]
    public string Challenge { get; set; }
}