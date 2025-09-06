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

/// <summary>
/// OpenId 缓存表
/// </summary>
[SugarTable("FeishuOpenIdCache")]
public class FeishuOpenIdCache
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, InsertServerTime = false, IsNullable = false)]
    public int Id { get; set; }
    
    /// <summary>
    /// 用户标识类型（mobile, email, union_id, user_id）
    /// </summary>
    public string UserIdType { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户标识值
    /// </summary>
    public string UserIdValue { get; set; } = string.Empty;
    
    /// <summary>
    /// 缓存的OpenId
    /// </summary>
    public string OpenId { get; set; } = string.Empty;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}