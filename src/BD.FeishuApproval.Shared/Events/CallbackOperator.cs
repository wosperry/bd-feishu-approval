using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Events;

/// <summary>
/// 回调中的操作人信息
/// </summary>
public class CallbackOperator
{
    /// <summary>
    /// 操作人用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    /// <summary>
    /// 操作时间（时间戳，秒）
    /// </summary>
    [JsonPropertyName("operate_time")]
    public long OperateTime { get; set; }

    /// <summary>
    /// 操作意见（可选）
    /// </summary>
    [JsonPropertyName("comment")]
    public string Comment { get; set; }
}
