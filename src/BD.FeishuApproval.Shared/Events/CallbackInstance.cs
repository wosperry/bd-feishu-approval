using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Events;

/// <summary>
/// 回调中的实例信息
/// </summary>
public class CallbackInstance
{
    /// <summary>
    /// 实例标识
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string InstanceCode { get; set; }

    /// <summary>
    /// 审批定义标识
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; }

    /// <summary>
    /// 旧状态
    /// </summary>
    [JsonPropertyName("old_status")]
    public string OldStatus { get; set; }

    /// <summary>
    /// 新状态
    /// </summary>
    [JsonPropertyName("new_status")]
    public string NewStatus { get; set; }
}
