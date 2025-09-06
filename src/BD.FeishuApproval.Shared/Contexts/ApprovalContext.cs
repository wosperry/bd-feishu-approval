using BD.FeishuApproval.Shared.Dtos.Instances;
using System.Collections.Generic;

namespace BD.FeishuApproval.Shared.Contexts;

/// <summary>
/// 审批策略执行上下文
/// </summary>
public class ApprovalContext
{
    /// <summary>
    /// 审批类型（如"leave"、"purchase"）
    /// </summary>
    public string ApprovalType { get; set; }

    /// <summary>
    /// 审批实例创建请求
    /// </summary>
    public CreateInstanceRequest InstanceRequest { get; set; } 

    /// <summary>
    /// 飞书返回的实例信息（创建后填充）
    /// </summary>
    public CreateInstanceResult InstanceResult { get; set; }

    /// <summary>
    /// 自定义扩展数据
    /// </summary>
    public Dictionary<string, object> Extensions { get; set; } = new();
}
