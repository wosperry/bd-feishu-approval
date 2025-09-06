using System;
using SqlSugar;

namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 飞书管理操作日志
/// </summary>
[SugarTable("FeishuManageLog")]
public class FeishuManageLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, InsertServerTime = false, IsNullable = false)]
    public int Id { get; set; }

    /// <summary>
    /// 操作类型：ConfigSave, ApprovalRegister, CodeGenerate, etc.
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// 操作描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 操作参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string Parameters { get; set; } = string.Empty;

    /// <summary>
    /// 操作结果：Success, Failed
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息（如果有）
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 客户端IP地址
    /// </summary>
    public string ClientIP { get; set; } = string.Empty;

    /// <summary>
    /// 用户代理
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string UserAgent { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


