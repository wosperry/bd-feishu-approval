using System;
using SqlSugar;

namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 审批注册映射（用于在管理端登记已在飞书创建的审批）。
/// </summary>
[SugarTable("FeishuApprovalRegistration")]
public class FeishuApprovalRegistration
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, InsertServerTime = false, IsNullable = false)]
    public int Id { get; set; }

    /// <summary>
    /// 审批代码（approval_code）
    /// </summary>
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 审批描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


