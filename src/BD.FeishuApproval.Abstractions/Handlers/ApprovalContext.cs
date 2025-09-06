using System;
using BD.FeishuApproval.Shared.Abstractions;
using BD.FeishuApproval.Shared.Events;

namespace BD.FeishuApproval.Abstractions.Handlers;

/// <summary>
/// 审批处理器上下文
/// 用于集中传递审批请求数据、回调事件及元信息
/// </summary>
public class ApprovalContext<TApprovalDto>
    where TApprovalDto : class, IFeishuApprovalRequest, new()
{
    public TApprovalDto Data { get; set; } = new();
    public FeishuCallbackEvent Callback { get; set; } = new();
    public Guid TraceId { get; set; } = Guid.NewGuid();
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}