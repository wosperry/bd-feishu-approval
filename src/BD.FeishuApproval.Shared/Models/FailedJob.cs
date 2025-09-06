using System;
using SqlSugar;

namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 失败任务记录
/// </summary>
[SugarTable("FailedJob")]
public class FailedJob
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, InsertServerTime = false, IsNullable = false)]
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string JobType { get; set; } = string.Empty; // e.g. ApiCall, CallbackHandle
    [SugarColumn(ColumnDataType = "LONGTEXT")]
    public string Payload { get; set; } = string.Empty; // JSON
    public int RetryCount { get; set; }
    public DateTime LastTriedAt { get; set; }
    [SugarColumn(ColumnDataType = "LONGTEXT")]
    public string LastError { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
}
