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
        try
        {
            // 尝试从多种方式获取审批类型标识
            var approvalType = await GetApprovalTypeFromCallbackAsync(callbackEvent);
            
            if (string.IsNullOrEmpty(approvalType))
            {
                _logger.LogWarning("无法确定审批类型 - 实例: {InstanceCode}, 事件ID: {EventId}", 
                    callbackEvent.InstanceCode, callbackEvent.EventId);
                return;
            }

            _logger.LogInformation("分发审批回调 - 类型: {ApprovalType}, 实例: {InstanceCode}, 状态: {Status}", 
                approvalType, callbackEvent.InstanceCode, callbackEvent.Type);

            // 获取对应的处理器
            var handler = _handlerRegistry.GetHandler(approvalType, _serviceProvider);
            if (handler == null)
            {
                _logger.LogWarning("未找到审批类型 {ApprovalType} 的处理器 - 实例: {InstanceCode}", 
                    approvalType, callbackEvent.InstanceCode);
                return;
            }

            // 调用处理器处理回调
            await handler.HandleCallbackAsync(callbackEvent);
            
            _logger.LogInformation("审批回调处理完成 - 类型: {ApprovalType}, 实例: {InstanceCode}", 
                approvalType, callbackEvent.InstanceCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分发审批回调失败 - 实例: {InstanceCode}, 事件ID: {EventId}", 
                callbackEvent.InstanceCode, callbackEvent.EventId);
            throw;
        }
    }

    /// <summary>
    /// 从回调事件中获取审批类型标识
    /// 支持多种获取方式，提高成功率
    /// </summary>
    private async Task<string> GetApprovalTypeFromCallbackAsync(FeishuCallbackEvent callbackEvent)
    {
        // 方式1: 优先使用ApprovalCode作为类型标识
        if (!string.IsNullOrEmpty(callbackEvent.ApprovalCode))
        {
            _logger.LogDebug("从ApprovalCode获取审批类型: {ApprovalCode}", callbackEvent.ApprovalCode);
            return callbackEvent.ApprovalCode;
        }

        // 方式2: 从实例代码查询审批类型（如果实例代码包含审批类型信息）
        if (!string.IsNullOrEmpty(callbackEvent.InstanceCode))
        {
            var approvalTypeFromInstance = ExtractApprovalTypeFromInstanceCode(callbackEvent.InstanceCode);
            if (!string.IsNullOrEmpty(approvalTypeFromInstance))
            {
                _logger.LogDebug("从实例代码提取审批类型: {ApprovalType}", approvalTypeFromInstance);
                return approvalTypeFromInstance;
            }
        }

        // 方式3: 从表单数据中解析审批类型（如果表单数据包含类型信息）
        if (!string.IsNullOrEmpty(callbackEvent.Form))
        {
            var approvalTypeFromForm = ExtractApprovalTypeFromFormData(callbackEvent.Form);
            if (!string.IsNullOrEmpty(approvalTypeFromForm))
            {
                _logger.LogDebug("从表单数据提取审批类型: {ApprovalType}", approvalTypeFromForm);
                return approvalTypeFromForm;
            }
        }

        // 方式4: 从数据库查询实例对应的审批类型
        if (!string.IsNullOrEmpty(callbackEvent.InstanceCode))
        {
            var approvalTypeFromDb = await GetApprovalTypeFromDatabaseAsync(callbackEvent.InstanceCode);
            if (!string.IsNullOrEmpty(approvalTypeFromDb))
            {
                _logger.LogDebug("从数据库查询审批类型: {ApprovalType}", approvalTypeFromDb);
                return approvalTypeFromDb;
            }
        }

        _logger.LogWarning("所有方式都无法获取审批类型 - 实例: {InstanceCode}, 事件ID: {EventId}", 
            callbackEvent.InstanceCode, callbackEvent.EventId);
        return string.Empty;
    }

    /// <summary>
    /// 从实例代码中提取审批类型
    /// 如果实例代码遵循特定命名规则，可以从中提取审批类型
    /// </summary>
    private string ExtractApprovalTypeFromInstanceCode(string instanceCode)
    {
        if (string.IsNullOrEmpty(instanceCode))
            return string.Empty;

        // 示例：如果实例代码格式为 "LEAVE_APPROVAL_001_20240101_001"
        // 可以提取 "LEAVE_APPROVAL_001" 作为审批类型
        var parts = instanceCode.Split('_');
        if (parts.Length >= 2)
        {
            // 尝试组合前两部分作为审批类型
            var potentialType = $"{parts[0]}_{parts[1]}";
            
            // 检查这个类型是否已注册
            if (_handlerRegistry.IsRegistered(potentialType))
            {
                return potentialType;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// 从表单数据中提取审批类型
    /// 如果表单数据包含审批类型信息，可以从中提取
    /// </summary>
    private string ExtractApprovalTypeFromFormData(string formData)
    {
        if (string.IsNullOrEmpty(formData))
            return string.Empty;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(formData);
            
            // 尝试从表单数据中查找审批类型字段
            if (doc.RootElement.TryGetProperty("approval_type", out var approvalTypeElement))
            {
                var approvalType = approvalTypeElement.GetString();
                if (!string.IsNullOrEmpty(approvalType) && _handlerRegistry.IsRegistered(approvalType))
                {
                    return approvalType;
                }
            }

            // 尝试从其他可能的字段中查找
            if (doc.RootElement.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();
                if (!string.IsNullOrEmpty(type) && _handlerRegistry.IsRegistered(type))
                {
                    return type;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析表单数据失败: {FormData}", formData);
        }

        return string.Empty;
    }

    /// <summary>
    /// 从数据库查询实例对应的审批类型
    /// 如果系统记录了实例与审批类型的映射关系，可以从数据库查询
    /// </summary>
    private async Task<string> GetApprovalTypeFromDatabaseAsync(string instanceCode)
    {
        try
        {
            // 这里需要根据实际的数据库结构来实现
            // 例如：查询实例表，获取对应的审批类型
            // var instance = await _repository.GetInstanceByCodeAsync(instanceCode);
            // return instance?.ApprovalType;
            
            // 暂时返回空字符串，需要根据实际数据库结构实现
            await Task.CompletedTask;
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从数据库查询审批类型失败 - 实例: {InstanceCode}", instanceCode);
            return string.Empty;
        }
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