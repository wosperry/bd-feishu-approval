using System;
using SqlSugar;

namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 仪表盘访问口令（哈希存储，使用PBKDF2算法）。
/// </summary>
[SugarTable("FeishuAdminAccessKey")]
public class FeishuAdminAccessKey
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, InsertServerTime = false, IsNullable = false)]
    public int Id { get; set; }

    /// <summary>
    /// 哈希后的密码（格式：salt:hash，兼容旧的明文格式）
    /// </summary>
    public string PlainPassword { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


