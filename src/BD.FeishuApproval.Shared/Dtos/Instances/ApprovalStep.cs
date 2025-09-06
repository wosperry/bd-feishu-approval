using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Dtos.Instances;

/// <summary>
/// 审批步骤记录
/// </summary>
public class ApprovalStep
{
    /// <summary>
    /// 步骤名称
    /// </summary>
    [JsonPropertyName("step_name")]
    public string StepName { get; set; }

    /// <summary>
    /// 处理人用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public string HandlerUserId { get; set; }

    /// <summary>
    /// 处理结果（APPROVED:通过, REJECTED:拒绝, PENDING:待处理）
    /// </summary>
    [JsonPropertyName("result")]
    public string Result { get; set; }

    /// <summary>
    /// 处理时间（时间戳，秒）
    /// </summary>
    [JsonPropertyName("operate_time")]
    public long? OperateTime { get; set; }
}
