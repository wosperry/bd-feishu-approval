using System;
using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 飞书访问令牌模型
/// </summary>
public class FeishuToken
{
    /// <summary>
    /// 租户级访问令牌（tenant_access_token）
    /// </summary>
    [JsonPropertyName("tenant_access_token")]
    public string TenantAccessToken { get; set; }

    /// <summary>
    /// 过期时间（秒，通常为7200）
    /// </summary>
    [JsonPropertyName("expire")]
    public int ExpireSeconds { get; set; }

    /// <summary>
    /// 令牌获取时间（UTC，用于本地过期判断）
    /// </summary>
    [JsonIgnore] // 不序列化到飞书
    public DateTime FetchTime { get; set; } = DateTime.UtcNow;
}
