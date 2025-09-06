using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Dtos.Definitions;

/// <summary>
/// 创建审批定义的返回结果
/// </summary>
public class CreateDefinitionResult
{
    /// <summary>
    /// 飞书审批定义唯一标识（approval_code）
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; }
}
