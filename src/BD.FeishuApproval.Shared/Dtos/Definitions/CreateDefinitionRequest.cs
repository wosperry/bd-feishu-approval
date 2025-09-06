using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Dtos.Definitions;

/// <summary>
/// 创建飞书审批定义请求
/// </summary>
public class CreateDefinitionRequest
{
    /// <summary>
    /// 审批名称（最多50字）
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// 审批描述（最多200字，可选）
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// 表单结构（飞书表单JSON schema，必填）
    /// </summary>
    [JsonPropertyName("form_schema")]
    public string FormSchema { get; set; }

    /// <summary>
    /// 审批流程配置（飞书流程JSON，必填）
    /// </summary>
    [JsonPropertyName("flow_schema")]
    public string FlowSchema { get; set; }

    /// <summary>
    /// 是否启用（默认true）
    /// </summary>
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 审批图标（可选，飞书图标库ID）
    /// </summary>
    [JsonPropertyName("icon_key")]
    public string IconKey { get; set; }
}
