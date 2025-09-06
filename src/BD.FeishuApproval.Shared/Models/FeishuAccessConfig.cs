using System;
using SqlSugar;

namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 飞书访问配置（单行表）。
/// </summary>
[SugarTable("FeishuAccessConfig")]
public class FeishuAccessConfig
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, InsertServerTime = false, IsNullable = false)]
    public int Id { get; set; }

    /// <summary>
    /// 飞书应用ID
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 飞书应用密钥
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// 消息加密Key（可选）
    /// </summary>
    public string EncryptKey { get; set; } = string.Empty;

    /// <summary>
    /// 事件订阅验证Token（可选）
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


