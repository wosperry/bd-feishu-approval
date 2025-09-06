using BD.FeishuApproval.Handlers;
using BD.FeishuApproval.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Callbacks;

/// <summary>
/// 飞书审批回调处理服务
/// 统一处理飞书审批状态变更回调，并分发到对应的处理器
/// </summary>
public class FeishuCallbackService : IFeishuCallbackService
{
    private readonly IApprovalHandlerRegistry _handlerRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FeishuCallbackService> _logger;

    public FeishuCallbackService(
        IApprovalHandlerRegistry handlerRegistry,
        IServiceProvider serviceProvider,
        ILogger<FeishuCallbackService> logger)
    {
        _handlerRegistry = handlerRegistry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 处理飞书审批状态变更回调
    /// </summary>
    /// <param name="callbackEvent">回调事件数据</param>
    /// <returns>处理结果</returns>
    public async Task<bool> HandleApprovalCallbackAsync(FeishuCallbackEvent callbackEvent)
    {
        try
        {
            _logger.LogInformation("收到飞书审批回调: 实例ID: {InstanceCode}, 状态: {Status}", 
                callbackEvent.InstanceCode, callbackEvent.Type);

            // 验证回调数据
            if (string.IsNullOrEmpty(callbackEvent.InstanceCode))
            {
                _logger.LogWarning("回调数据缺少实例代码");
                return false;
            }

            // 根据审批类型分发到对应的处理器
            await DispatchCallbackToHandlerAsync(callbackEvent);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书审批回调失败");
            return false;
        }
    }

    /// <summary>
    /// 分发回调到对应的处理器
    /// </summary>
    private async Task DispatchCallbackToHandlerAsync(FeishuCallbackEvent callbackEvent)
    {
        // 尝试从多种方式获取审批类型标识
        var approvalType = GetApprovalTypeFromCallback(callbackEvent);
        
        if (string.IsNullOrEmpty(approvalType))
        {
            _logger.LogWarning("无法确定审批类型 - 实例: {InstanceCode}", callbackEvent.InstanceCode);
            return;
        }

        _logger.LogInformation("分发审批回调 - 类型: {ApprovalType}, 实例: {InstanceCode}", 
            approvalType, callbackEvent.InstanceCode);

        // 获取对应的处理器
        var handler = _handlerRegistry.GetHandler(approvalType, _serviceProvider);
        if (handler == null)
        {
            _logger.LogWarning("未找到审批类型 {ApprovalType} 的处理器", approvalType);
            return;
        }

        // 调用处理器处理回调
        await handler.HandleCallbackAsync(callbackEvent);
    }

    /// <summary>
    /// 从回调事件中获取审批类型标识
    /// </summary>
    private string GetApprovalTypeFromCallback(FeishuCallbackEvent callbackEvent)
    {
        // 优先使用ApprovalCode作为类型标识
        if (!string.IsNullOrEmpty(callbackEvent.ApprovalCode))
        {
            return callbackEvent.ApprovalCode;
        }

        // 如果有其他方式获取审批类型，可以在此处添加
        // 例如：从表单数据中解析、从数据库查询等

        return string.Empty;
    }
}

/// <summary>
/// 飞书回调服务接口
/// </summary>
public interface IFeishuCallbackService
{
    /// <summary>
    /// 处理飞书审批状态变更回调
    /// </summary>
    /// <param name="callbackEvent">回调事件数据</param>
    /// <returns>处理结果</returns>
    Task<bool> HandleApprovalCallbackAsync(FeishuCallbackEvent callbackEvent);
}