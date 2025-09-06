using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Dtos.Instances;

/// <summary>
/// 审批实例详情
/// </summary>
public class InstanceDetail
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
    /// 审批标题
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// 申请人用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public string ApplicantUserId { get; set; }

    /// <summary>
    /// 申请人姓名
    /// </summary>
    [JsonPropertyName("user_name")]
    public string ApplicantName { get; set; }

    /// <summary>
    /// 实例状态（PENDING:审批中, APPROVED:已通过, REJECTED:已拒绝, CANCELED:已取消）
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// 表单数据（JSON）
    /// </summary>
    [JsonPropertyName("form_data")]
    public string FormData { get; set; }

    /// <summary>
    /// 创建时间（时间戳，秒）
    /// </summary>
    [JsonPropertyName("create_time")]
    public long CreateTime { get; set; }

    /// <summary>
    /// 结束时间（时间戳，秒）
    /// </summary>
    [JsonPropertyName("end_time")]
    public long? EndTime { get; set; }

    /// <summary>
    /// 审批记录列表
    /// </summary>
    [JsonPropertyName("approval_steps")]
    public List<ApprovalStep> ApprovalSteps { get; set; } = new();
}
