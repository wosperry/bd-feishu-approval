using BD.FeishuApproval.Handlers;
using BD.FeishuApproval.Shared.Events;
using BD.FeishuApproval.Shared.Models;
using BD.FeishuApproval.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
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
    private readonly IFeishuApprovalRepository _repository;
    private readonly ILogger<FeishuCallbackService> _logger;

    public FeishuCallbackService(
        IApprovalHandlerRegistry handlerRegistry,
        IServiceProvider serviceProvider,
        IFeishuApprovalRepository repository,
        ILogger<FeishuCallbackService> logger)
    {
        _handlerRegistry = handlerRegistry;
        _serviceProvider = serviceProvider;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 处理飞书审批状态变更回调
    /// </summary>
    /// <param name="callbackEvent">回调事件数据</param>
    /// <returns>处理结果</returns>
    public async Task<bool> HandleApprovalCallbackAsync(FeishuCallbackEvent callbackEvent)
    {
        int? callbackRecordId = null;
        try
        {
            _logger.LogDebug("收到飞书审批回调: 实例ID: {InstanceCode}, 审批：{} {ApproveCode} 状态: {Status}", 
                callbackEvent.EventId,callbackEvent.Event.ApprovalName,callbackEvent.Event.ApprovalCode, callbackEvent.Event.EventAction);

            // 验证回调数据
            if (string.IsNullOrEmpty(callbackEvent.Event.ApprovalCode))
            {
                _logger.LogWarning("回调数据缺少实例代码");
                return false;
            }

            // 检查是否为重复事件
            if (!string.IsNullOrEmpty(callbackEvent.EventId))
            {
                var existingRecord = await _repository.GetCallbackRecordByEventIdAsync(callbackEvent.EventId);
                if (existingRecord != null)
                {
                    _logger.LogWarning("收到重复的回调事件: {EventId}, 原记录ID: {RecordId}", 
                        callbackEvent.EventId, existingRecord.Id);
                    return true; // 重复事件视为成功
                }
            }

            // 保存回调记录到数据库
            callbackRecordId = await SaveCallbackRecordAsync(callbackEvent);
            
            if (callbackRecordId.HasValue)
            {
                // 更新状态为处理中
                await _repository.UpdateCallbackRecordStatusAsync(callbackRecordId.Value, 
                    CallbackProcessingStatus.Processing, "开始处理回调事件");
            }

            // 根据审批类型分发到对应的处理器
            await DispatchCallbackToHandlerAsync(callbackEvent);

            // 更新状态为完成
            if (callbackRecordId.HasValue)
            {
                await _repository.UpdateCallbackRecordStatusAsync(callbackRecordId.Value, 
                    CallbackProcessingStatus.Completed, "回调事件处理成功");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书审批回调失败");
            
            // 更新状态为失败
            if (callbackRecordId.HasValue)
            {
                await _repository.UpdateCallbackRecordStatusAsync(callbackRecordId.Value, 
                    CallbackProcessingStatus.Failed, $"处理失败: {ex.Message}");
            }
            
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
                    callbackEvent.Event.ApprovalCode, callbackEvent.EventId);
                return;
            }

            _logger.LogDebug("分发审批回调 - 类型: {ApprovalType}, 实例: {InstanceCode}, 状态: {Status}", 
                approvalType, callbackEvent.Event.ApprovalCode, callbackEvent.Type);

            // 获取对应的处理器
            var handler = _handlerRegistry.GetHandler(approvalType, _serviceProvider);
            if (handler == null)
            {
                _logger.LogWarning("未找到审批类型 {ApprovalType} 的处理器 - 实例: {InstanceCode}", 
                    approvalType, callbackEvent.Event.ApprovalCode);
                return;
            }

            // 调用处理器处理回调
            await handler.HandleCallbackAsync(callbackEvent);
            
            _logger.LogDebug("审批回调处理完成 - 类型: {ApprovalType}, 实例: {InstanceCode}", 
                approvalType, callbackEvent.Event.ApprovalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分发审批回调失败 - 实例: {InstanceCode}, 事件ID: {EventId}", 
                callbackEvent.Event.ApprovalCode, callbackEvent.EventId);
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
        if (!string.IsNullOrEmpty(callbackEvent.Event.ApprovalCode))
        {
            _logger.LogDebug("从ApprovalCode获取审批类型: {ApprovalCode}", callbackEvent.Event.ApprovalCode);
            return callbackEvent.Event.ApprovalCode;
        }

        // 方式2: 从实例代码查询审批类型（如果实例代码包含审批类型信息）
        if (!string.IsNullOrEmpty(callbackEvent.Event.ApprovalCode))
        {
            var approvalTypeFromInstance = ExtractApprovalTypeFromInstanceCode(callbackEvent.Event.ApprovalCode);
            if (!string.IsNullOrEmpty(approvalTypeFromInstance))
            {
                _logger.LogDebug("从实例代码提取审批类型: {ApprovalType}", approvalTypeFromInstance);
                return approvalTypeFromInstance;
            }
        }

        // 方式3: 从表单数据中解析审批类型（如果表单数据包含类型信息）
        if (!string.IsNullOrEmpty(callbackEvent.Event.ApprovalCode))
        {
            var approvalTypeFromForm =  callbackEvent.Event.ApprovalCode;
            if (!string.IsNullOrEmpty(approvalTypeFromForm))
            {
                _logger.LogDebug("从表单数据提取审批类型: {ApprovalType}", approvalTypeFromForm);
                return approvalTypeFromForm;
            }
        }

        // 方式4: 从数据库查询实例对应的审批类型
        if (!string.IsNullOrEmpty(callbackEvent.Event.ApprovalCode))
        {
            var approvalTypeFromDb = await GetApprovalTypeFromDatabaseAsync(callbackEvent.Event.ApprovalCode);
            if (!string.IsNullOrEmpty(approvalTypeFromDb))
            {
                _logger.LogDebug("从数据库查询审批类型: {ApprovalType}", approvalTypeFromDb);
                return approvalTypeFromDb;
            }
        }

        _logger.LogWarning("所有方式都无法获取审批类型 - 实例: {InstanceCode}, 事件ID: {EventId}", 
            callbackEvent.Event.ApprovalCode, callbackEvent.EventId);
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
    /// 从数据库查询实例对应的审批类型
    /// 如果系统记录了实例与审批类型的映射关系，可以从数据库查询
    /// </summary>
    private async Task<string> GetApprovalTypeFromDatabaseAsync(string instanceCode)
    {
        try
        {
            var approvalType = await _repository.GetApprovalTypeByInstanceCodeAsync(instanceCode);
            return approvalType;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从数据库查询审批类型失败 - 实例: {InstanceCode}", instanceCode);
            return string.Empty;
        }
    }

    /// <summary>
    /// 保存回调记录到数据库
    /// </summary>
    private async Task<int> SaveCallbackRecordAsync(FeishuCallbackEvent callbackEvent)
    {
        try
        {
            var record = new FeishuCallbackRecord
            {
                EventId = callbackEvent.EventId ?? string.Empty,
                EventType = callbackEvent.Event.EventType ?? string.Empty,
                InstanceCode = callbackEvent.Event.ApprovalCode ?? string.Empty,
                ApprovalCode = callbackEvent.Event.ApprovalCode ?? string.Empty,
                Status = callbackEvent.Type ?? string.Empty,
                EventTime = callbackEvent.Timestamp?? DateTime.UtcNow.ToFileTimeUtc().ToString(),
                FormData = callbackEvent.Event.ApprovalCode ?? string.Empty,
                RawCallbackData = JsonSerializer.Serialize(callbackEvent),
                ProcessingStatus = CallbackProcessingStatus.Pending
                
            };

            var recordId = await _repository.SaveCallbackRecordAsync(record);
            _logger.LogDebug("回调记录已保存到数据库 - 记录ID: {RecordId}, 事件ID: {EventId}", recordId, callbackEvent.EventId);
            return recordId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存回调记录到数据库失败 - 事件ID: {EventId}", callbackEvent.EventId);
            throw;
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