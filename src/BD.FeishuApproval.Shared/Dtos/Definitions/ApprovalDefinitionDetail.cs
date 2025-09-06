using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Dtos.Definitions;

/// <summary>
/// 审批定义详情
/// </summary>
public class ApprovalDefinitionDetail
{
    /// <summary>
    /// 审批代码
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; }

    /// <summary>
    /// 审批名称
    /// </summary>
    [JsonPropertyName("approval_name")]
    public string ApprovalName { get; set; }

    /// <summary>
    /// 表单结构（JSON字符串）
    /// </summary>
    [JsonPropertyName("form")]
    public string Form { get; set; }

    /// <summary>
    /// 表单组件关系（JSON字符串）
    /// </summary>
    [JsonPropertyName("form_widget_relation")]
    public string FormWidgetRelation { get; set; }

    /// <summary>
    /// 节点列表
    /// </summary>
    [JsonPropertyName("node_list")]
    public List<ApprovalNode> NodeList { get; set; } = new List<ApprovalNode>();

    /// <summary>
    /// 状态（ACTIVE:启用，INACTIVE:停用）
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 查看者列表
    /// </summary>
    [JsonPropertyName("viewers")]
    public List<ApprovalViewer> Viewers { get; set; } = new List<ApprovalViewer>();
}

/// <summary>
/// 发起审批用的Body
/// </summary>
public class FeishuCreateApprovalBody
{

    /// <summary>
    /// 审批代码
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; }

    /// <summary>
    /// 表单结构（JSON字符串）
    /// </summary>
    [JsonPropertyName("form")]
    public string Form { get; set; }

    /// <summary>
    /// 飞书用户ID
    /// </summary>
    [JsonPropertyName("open_id")]
    public string OpenId { get; set; }
}

/// <summary>
/// 审批节点信息
/// </summary>
public class ApprovalNode
{
    /// <summary>
    /// 是否允许多选审批人
    /// </summary>
    [JsonPropertyName("approver_chosen_multi")]
    public bool ApproverChosenMulti { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// 是否需要审批人
    /// </summary>
    [JsonPropertyName("need_approver")]
    public bool NeedApprover { get; set; }

    /// <summary>
    /// 节点ID
    /// </summary>
    [JsonPropertyName("node_id")]
    public string NodeId { get; set; }

    /// <summary>
    /// 节点类型
    /// </summary>
    [JsonPropertyName("node_type")]
    public string NodeType { get; set; }

    /// <summary>
    /// 是否需要签名
    /// </summary>
    [JsonPropertyName("require_signature")]
    public bool RequireSignature { get; set; }
}

/// <summary>
/// 审批查看者信息
/// </summary>
public class ApprovalViewer
{
    /// <summary>
    /// ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
}
