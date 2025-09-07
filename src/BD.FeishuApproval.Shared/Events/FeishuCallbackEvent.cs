using System.Text.Json.Serialization;

/// <summary>
/// 飞书事件回调根对象
/// </summary>
public class FeishuCallbackEvent
{
    /// <summary>
    /// 事件唯一标识
    /// </summary>
    [JsonPropertyName("uuid")]
    public string EventId { get; set; }

    /// <summary>
    /// 事件详情
    /// </summary>
    [JsonPropertyName("event")]
    public FeishuApprovalEventData Event { get; set; }

    /// <summary>
    /// 令牌
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; }

    /// <summary>
    /// 事件时间戳
    /// </summary>
    [JsonPropertyName("ts")]
    public string Timestamp { get; set; }

    /// <summary>
    /// 事件类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("form")]
    public string Form { get; set; }
}

/// <summary>
/// 事件详情数据
/// </summary>
public class FeishuApprovalEventData
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [JsonPropertyName("app_id")]
    public string AppId { get; set; }

    /// <summary>
    /// 审批定义编码
    /// </summary>
    [JsonPropertyName("definition_code")]
    public string ApprovalCode { get; set; }

    /// <summary>
    /// 审批定义名称
    /// </summary>
    [JsonPropertyName("definition_name")]
    public string ApprovalName { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("end_time")]
    public double EndTime { get; set; }

    /// <summary>
    /// 事件动作（如：reject表示拒绝）
    /// </summary>
    [JsonPropertyName("event")]
    public string EventAction { get; set; }

    /// <summary>
    /// 实例编码
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string InstanceCode { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [JsonPropertyName("start_time")]
    public double StartTime { get; set; }

    /// <summary>
    /// 租户密钥
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string TenantKey { get; set; }

    /// <summary>
    /// 事件类型（如：approval表示审批）
    /// </summary>
    [JsonPropertyName("type")]
    public string EventType { get; set; }
}
