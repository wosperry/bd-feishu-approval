using System;
using System.Text.Json.Serialization;
using SqlSugar;

namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 飞书审批回调记录
/// 存储接收到的回调事件数据
/// </summary>
[SugarTable("FeishuCallbackRecord")]
public class FeishuCallbackRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 事件ID（飞书提供）
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// 事件类型（如approval_status_change）
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 审批实例代码
    /// </summary>
    public string InstanceCode { get; set; } = string.Empty;

    /// <summary>
    /// 审批代码
    /// </summary>
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// 审批状态类型（如approved, rejected, cancelled）
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 事件产生时间（飞书时间戳，秒）
    /// </summary>
    public string EventTime { get; set; } = string.Empty;

    /// <summary>
    /// 表单数据JSON字符串
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string FormData { get; set; } = string.Empty;

    /// <summary>
    /// 原始回调数据JSON字符串
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string RawCallbackData { get; set; } = string.Empty;

    /// <summary>
    /// 处理状态：Pending, Processing, Completed, Failed
    /// </summary>
    public string ProcessingStatus { get; set; } = "Pending";

    /// <summary>
    /// 处理结果消息
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string ProcessingMessage { get; set; } = string.Empty;

    /// <summary>
    /// 处理开始时间
    /// </summary>
    public DateTime? ProcessingStartedAt { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 处理完成时间
    /// </summary>
    public DateTime? ProcessingCompletedAt { get; set; } = DateTime.MaxValue;

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// 最后重试时间
    /// </summary>
    public DateTime? LastRetryAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 记录创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 记录更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 回调处理状态枚举
/// </summary>
public static class CallbackProcessingStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";  
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}