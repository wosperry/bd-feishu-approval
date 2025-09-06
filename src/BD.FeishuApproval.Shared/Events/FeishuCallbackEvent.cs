using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Events;

/// <summary>
/// 飞书回调事件基类
/// </summary>
public class FeishuCallbackEvent
{
    /// <summary>
    /// 事件类型（如approval_status_change）
    /// </summary>
    [JsonPropertyName("event_type")]
    public string EventType { get; set; }

    /// <summary>
    /// 事件产生时间（时间戳，秒）
    /// </summary>
    [JsonPropertyName("event_time")]
    public long EventTime { get; set; }

    /// <summary>
    /// 事件ID
    /// </summary>
    [JsonPropertyName("event_id")]
    public string EventId { get; set; }

    /// <summary>
    /// 审批实例代码
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string InstanceCode { get; set; }

    /// <summary>
    /// 审批代码
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; }

    /// <summary>
    /// 审批状态类型（如approved, rejected, cancelled）
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// 表单数据JSON字符串
    /// </summary>
    [JsonPropertyName("form")]
    public string Form { get; set; }
}
