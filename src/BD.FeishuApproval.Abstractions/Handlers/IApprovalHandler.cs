using BD.FeishuApproval.Shared.Abstractions;
using BD.FeishuApproval.Shared.Dtos.Instances;
using BD.FeishuApproval.Shared.Events;
using System;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Handlers;

/// <summary>
/// 泛型审批处理器接口
/// 负责回调处理和提供校验能力，但不负责创建审批
/// </summary>
public interface IApprovalHandler<TApprovalDto> where TApprovalDto : class, IFeishuApprovalRequest, new()
{
    string ApprovalCode { get; }

    /// <summary>验证审批请求（供外部调用）</summary>
    Task ValidateAsync(TApprovalDto request);

    /// <summary>预处理审批请求（供外部调用）</summary>
    Task PreProcessAsync(TApprovalDto request);

    /// <summary>后处理审批结果（供外部调用）</summary>
    Task PostProcessAsync(TApprovalDto request, CreateInstanceResult result);

    /// <summary>处理创建失败（供外部调用）</summary>
    Task HandleCreateFailureAsync(TApprovalDto request, Exception exception);

    /// <summary>审批通过</summary>
    Task HandleApprovalApprovedAsync(ApprovalContext<TApprovalDto> context);

    /// <summary>审批拒绝</summary>
    Task HandleApprovalRejectedAsync(ApprovalContext<TApprovalDto> context);

    /// <summary>审批撤回</summary>
    Task HandleApprovalCancelledAsync(ApprovalContext<TApprovalDto> context);

    /// <summary>未知状态</summary>
    Task HandleUnknownStatusAsync(ApprovalContext<TApprovalDto> context);

    /// <summary>业务异常处理</summary>
    Task HandleBusinessExceptionAsync(ApprovalContext<TApprovalDto> context, Exception exception);
}

/// <summary>
/// 非泛型审批处理器接口，用于运行时分发
/// </summary>
public interface IApprovalHandler
{
    string ApprovalCode { get; }
    Task HandleCallbackAsync(FeishuCallbackEvent callbackEvent);
}

 