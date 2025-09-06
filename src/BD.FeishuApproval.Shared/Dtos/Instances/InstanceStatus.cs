using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Dtos.Instances;

/// <summary>
/// 审批实例状态信息
/// </summary>
public class InstanceStatus
{
    /// <summary>
    /// 实例标识
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string InstanceCode { get; set; }

    /// <summary>
    /// 状态（PENDING/APPROVED/REJECTED/CANCELED）
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// 最后更新时间（时间戳，秒）
    /// </summary>
    [JsonPropertyName("update_time")]
    public long UpdateTime { get; set; }
}
