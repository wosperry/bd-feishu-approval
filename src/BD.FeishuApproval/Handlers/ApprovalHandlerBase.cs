using BD.FeishuApproval.Abstractions.Handlers;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Shared.Abstractions;
using BD.FeishuApproval.Shared.Dtos.Instances;
using BD.FeishuApproval.Shared.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Handlers;

/// <summary>
/// 审批处理器基类
/// 基于策略模式，统一审批创建流程，并强制要求子类实现关键方法
/// </summary>
public abstract class ApprovalHandlerBase<TApprovalDto> : IApprovalHandler<TApprovalDto>, IApprovalHandler
    where TApprovalDto : class, IFeishuApprovalRequest, new()
{
    protected readonly IFeishuApprovalInstanceService _instanceService;
    protected readonly ILogger _logger;

    protected ApprovalHandlerBase(
        IFeishuApprovalInstanceService instanceService,
        ILogger logger)
    {
        _instanceService = instanceService;
        _logger = logger;
    }

    public virtual string ApprovalType => new TApprovalDto().GetApprovalType();

    /// <summary>
    /// 验证审批请求（供外部调用，如Controller或Service）
    /// 让第三方可以在Handler中定制校验逻辑，但不强制Handler负责Create
    /// </summary>
    public async Task ValidateAsync(TApprovalDto request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        await ValidateApprovalRequestAsync(request);
    }

    /// <summary>
    /// 预处理审批请求（供外部调用）
    /// 让第三方可以在Handler中定制预处理逻辑
    /// </summary>
    public async Task PreProcessAsync(TApprovalDto request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        await PreProcessApprovalAsync(request);
    }

    /// <summary>
    /// 后处理审批结果（供外部调用）
    /// 让第三方可以在Handler中定制后处理逻辑
    /// </summary>
    public async Task PostProcessAsync(TApprovalDto request, CreateInstanceResult result)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        await PostProcessApprovalAsync(request, result);
    }

    /// <summary>
    /// 处理创建失败（供外部调用）
    /// </summary>
    public async Task HandleCreateFailureAsync(TApprovalDto request, Exception exception)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        await HandleCreateFailureInternalAsync(request, exception);
    }

    /// <summary>
    /// 非泛型回调分发
    /// </summary>
    public async Task HandleCallbackAsync(FeishuCallbackEvent callbackEvent)
    {
        try
        {
            var approvalData = ParseApprovalDataFromCallback(callbackEvent);
            var context = new ApprovalContext<TApprovalDto>
            {
                Data = approvalData,
                Callback = callbackEvent
            };

            await HandleApprovalStatusChangedAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理审批回调失败 - 实例: {InstanceCode}", callbackEvent.InstanceCode);
            throw;
        }
    }

    private async Task HandleApprovalStatusChangedAsync(ApprovalContext<TApprovalDto> context)
    {
        try
        {
            switch (context.Callback.Type?.ToLower())
            {
                case "approved":
                    await HandleApprovalApprovedAsync(context);
                    break;
                case "rejected":
                    await HandleApprovalRejectedAsync(context);
                    break;
                case "cancelled":
                    await HandleApprovalCancelledAsync(context);
                    break;
                default:
                    await HandleUnknownStatusAsync(context);
                    break;
            }
        }
        catch (Exception ex)
        {
            await HandleBusinessExceptionAsync(context, ex);
        }
    }

    #region ===== 必须由子类实现的方法 =====
    public abstract Task HandleApprovalApprovedAsync(ApprovalContext<TApprovalDto> context);
    public abstract Task HandleApprovalRejectedAsync(ApprovalContext<TApprovalDto> context);
    public abstract Task HandleApprovalCancelledAsync(ApprovalContext<TApprovalDto> context);
    public abstract Task HandleUnknownStatusAsync(ApprovalContext<TApprovalDto> context);
    public abstract Task HandleBusinessExceptionAsync(ApprovalContext<TApprovalDto> context, Exception exception);
    #endregion

    #region ===== 可选重写的钩子方法 =====
    protected virtual async Task ValidateApprovalRequestAsync(TApprovalDto request) => await Task.CompletedTask;
    protected virtual async Task PreProcessApprovalAsync(TApprovalDto request) => await Task.CompletedTask;
    protected virtual async Task PostProcessApprovalAsync(TApprovalDto request, CreateInstanceResult result) => await Task.CompletedTask;
    protected virtual async Task HandleCreateFailureInternalAsync(TApprovalDto request, Exception exception) => await Task.CompletedTask;
    #endregion

    #region ===== 私有辅助方法 =====
    private TApprovalDto ParseApprovalDataFromCallback(FeishuCallbackEvent callbackEvent)
    {
        if (string.IsNullOrEmpty(callbackEvent.Form)) return new TApprovalDto();
        try
        {
            return JsonSerializer.Deserialize<TApprovalDto>(callbackEvent.Form) ?? new TApprovalDto();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析审批数据失败，使用默认实例");
            return new TApprovalDto();
        }
    }
    #endregion
}
