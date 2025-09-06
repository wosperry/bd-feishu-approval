using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Events;

/// <summary>
/// 审批状态变更事件
/// </summary>
public class ApprovalStatusChangedEvent : FeishuCallbackEvent
{
    /// <summary>
    /// 审批实例信息
    /// </summary>
    [JsonPropertyName("instance")]
    public CallbackInstance Instance { get; set; }

    /// <summary>
    /// 操作人信息
    /// </summary>
    [JsonPropertyName("operator")]
    public CallbackOperator Operator { get; set; }
}
