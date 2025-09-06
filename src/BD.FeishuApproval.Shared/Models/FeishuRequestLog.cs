using System;
using SqlSugar;

namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 飞书请求日志
/// </summary>
[SugarTable("FeishuRequestLog")]
public class FeishuRequestLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, InsertServerTime = false, IsNullable = false)]
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    [SugarColumn(ColumnDataType = "LONGTEXT")]
    public string Headers { get; set; } = string.Empty;
    [SugarColumn(ColumnDataType = "LONGTEXT")]
    public string Body { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
}

[SugarTable("FeishuUser")]
public class FeishuUser
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, InsertServerTime = false, IsNullable = false)]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Phone { get; set; }
    public string FeishuOpenId { get; set; }

}