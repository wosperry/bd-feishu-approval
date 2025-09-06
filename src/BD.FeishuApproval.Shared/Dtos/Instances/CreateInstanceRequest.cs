using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Dtos.Instances;

/// <summary>
/// 创建审批实例请求
/// </summary>
public class CreateInstanceRequest
{
    /// <summary>
    /// 审批定义标识（approval_code）
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; }

    /// <summary>
    /// 申请人用户ID（open_id/union_id/user_id）
    /// </summary>
    [JsonPropertyName("user_id")]
    public string ApplicantUserId { get; set; }

    /// <summary>
    /// 表单数据（JSON，与审批定义的form_schema对应）
    /// </summary>
    [JsonPropertyName("form_data")]
    public string FormData { get; set; }

    /// <summary>
    /// 表单数据（兼容属性，与FormData相同）
    /// </summary>
    [JsonIgnore]
    public string Form
    {
        get => FormData;
        set => FormData = value;
    }

    /// <summary>
    /// 审批实例标题（可选，默认使用审批名称）
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// 抄送人用户ID列表（可选）
    /// </summary>
    [JsonPropertyName("cc_user_ids")]
    public List<string> CcUserIds { get; set; } = new();
}
