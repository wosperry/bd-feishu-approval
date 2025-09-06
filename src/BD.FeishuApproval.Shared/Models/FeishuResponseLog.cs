using System;
using SqlSugar;

namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 飞书响应日志
/// </summary>
[SugarTable("FeishuResponseLog")]
public class FeishuResponseLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, InsertServerTime = false, IsNullable = false)]
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int StatusCode { get; set; }
    [SugarColumn(ColumnDataType = "LONGTEXT")]
    public string Headers { get; set; } = string.Empty;
    [SugarColumn(ColumnDataType = "LONGTEXT")]
    public string Body { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public bool Success { get; set; }
    [SugarColumn(ColumnDataType = "TEXT")]
    public string ErrorMessage { get; set; } = string.Empty;
}
