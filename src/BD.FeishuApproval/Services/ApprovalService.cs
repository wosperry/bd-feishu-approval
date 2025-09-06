using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Abstractions.Services;
using BD.FeishuApproval.Extensions;
using BD.FeishuApproval.Handlers;
using BD.FeishuApproval.Shared.Abstractions;
using BD.FeishuApproval.Shared.Dtos.Instances;
using BD.FeishuApproval.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Services;

/// <summary>
/// 统一审批服务实现
/// 正确的职责划分：ApprovalService负责创建审批，Handler负责回调处理和提供校验能力
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly IApprovalHandlerRegistry _handlerRegistry;
    private readonly IFeishuApprovalInstanceService _instanceService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        IApprovalHandlerRegistry handlerRegistry,
        IFeishuApprovalInstanceService instanceService,
        IServiceProvider serviceProvider,
        ILogger<ApprovalService> logger)
    {
        _handlerRegistry = handlerRegistry;
        _instanceService = instanceService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 创建审批实例
    /// 正确的职责划分：ApprovalService负责创建，Handler提供校验和生命周期钩子
    /// </summary>
    public async Task<CreateInstanceResult> CreateApprovalAsync<T>(T request) 
        where T : class, IFeishuApprovalRequest, new()
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var approvalType = request.GetApprovalType();
        _logger.LogInformation("开始创建审批 - 类型: {ApprovalType}", approvalType);

        // 通过Registry获取对应的Handler（用于校验和生命周期钩子）
        var handler = _handlerRegistry.GetHandler<T>(_serviceProvider);
        if (handler == null)
        {
            var errorMsg = $"未找到审批类型 '{approvalType}' 对应的处理器，请确保已注册相应的Handler";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        try
        {
            // 1. 委托给Handler进行验证
            await handler.ValidateAsync(request);
            
            // 2. 委托给Handler进行预处理
            await handler.PreProcessAsync(request);

            // 3. ApprovalService负责实际的API调用
            _logger.LogDebug("调用飞书API创建审批实例 - 类型: {ApprovalType}", approvalType);
            var result = await _instanceService.CreateTypedInstanceAsync(request);

            if (!result.IsSuccess || result.Data == null)
            {
                throw new InvalidOperationException($"创建审批实例失败: {result.Message}");
            }

            // 4. 委托给Handler进行后处理
            await handler.PostProcessAsync(request, result.Data);

            _logger.LogInformation("审批创建成功 - 类型: {ApprovalType}, 实例ID: {InstanceCode}", 
                approvalType, result.Data.InstanceCode);
            
            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建审批失败 - 类型: {ApprovalType}", approvalType);
            
            // 委托给Handler处理创建失败
            try
            {
                await handler.HandleCreateFailureAsync(request, ex);
            }
            catch (Exception handlerEx)
            {
                _logger.LogWarning(handlerEx, "Handler处理创建失败时发生异常");
            }
            
            throw;
        }
    }

    /// <summary>
    /// 处理审批回调事件（自动解析审批类型）
    /// </summary>
    public async Task HandleApprovalCallbackAsync(FeishuCallbackEvent callbackEvent)
    {
        if (callbackEvent == null)
            throw new ArgumentNullException(nameof(callbackEvent));

        // 这里需要根据callbackEvent解析出审批类型
        // 可能需要从InstanceCode、ApprovalCode或其他字段推断
        var approvalType = ExtractApprovalTypeFromCallback(callbackEvent);
        
        if (string.IsNullOrEmpty(approvalType))
        {
            _logger.LogWarning("无法从回调事件中解析审批类型 - 实例: {InstanceCode}", callbackEvent.InstanceCode);
            throw new InvalidOperationException("无法确定审批类型，回调事件缺少必要信息");
        }

        await HandleApprovalCallbackAsync(approvalType, callbackEvent);
    }

    /// <summary>
    /// 根据审批类型处理回调事件
    /// </summary>
    public async Task HandleApprovalCallbackAsync(string approvalType, FeishuCallbackEvent callbackEvent)
    {
        if (string.IsNullOrEmpty(approvalType))
            throw new ArgumentException("审批类型不能为空", nameof(approvalType));
        
        if (callbackEvent == null)
            throw new ArgumentNullException(nameof(callbackEvent));

        _logger.LogInformation("开始处理审批回调 - 类型: {ApprovalType}, 实例: {InstanceCode}, 状态: {Status}", 
            approvalType, callbackEvent.InstanceCode, callbackEvent.Type);

        // 通过Registry获取对应的Handler
        var handler = _handlerRegistry.GetHandler(approvalType, _serviceProvider);
        if (handler == null)
        {
            var errorMsg = $"未找到审批类型 '{approvalType}' 对应的处理器";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        try
        {
            // 委托给Handler处理回调
            await handler.HandleCallbackAsync(callbackEvent);
            
            _logger.LogInformation("审批回调处理完成 - 类型: {ApprovalType}, 实例: {InstanceCode}", 
                approvalType, callbackEvent.InstanceCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理审批回调失败 - 类型: {ApprovalType}, 实例: {InstanceCode}", 
                approvalType, callbackEvent.InstanceCode);
            throw;
        }
    }

    /// <summary>
    /// 检查指定审批类型是否已注册Handler
    /// </summary>
    public bool IsApprovalTypeSupported(string approvalType)
    {
        if (string.IsNullOrEmpty(approvalType))
            return false;
        
        return _handlerRegistry.IsRegistered(approvalType);
    }

    /// <summary>
    /// 检查指定审批请求类型是否已注册Handler
    /// </summary>
    public bool IsApprovalTypeSupported<T>() where T : class, IFeishuApprovalRequest, new()
    {
        var approvalType = new T().GetApprovalType();
        return IsApprovalTypeSupported(approvalType);
    }

    /// <summary>
    /// 获取所有支持的审批类型
    /// </summary>
    public string[] GetSupportedApprovalTypes()
    {
        return _handlerRegistry.GetRegisteredApprovalTypes().ToArray();
    }

    /// <summary>
    /// 从回调事件中提取审批类型
    /// 这是一个策略方法，可能需要根据实际情况调整实现逻辑
    /// </summary>
    private string ExtractApprovalTypeFromCallback(FeishuCallbackEvent callbackEvent)
    {
        // 策略1: 尝试从ApprovalCode直接获取（如果回调事件包含这个字段）
        if (!string.IsNullOrEmpty(callbackEvent.ApprovalCode))
        {
            return callbackEvent.ApprovalCode;
        }

        // 策略2: 从Form数据中解析（需要根据实际JSON结构调整）
        if (!string.IsNullOrEmpty(callbackEvent.Form))
        {
            try
            {
                // 这里可能需要解析JSON来获取审批类型信息
                // 具体实现依赖于飞书回调的数据结构
                // 例如：从Form中的某个字段推断，或者维护InstanceCode到ApprovalType的映射表
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析回调事件Form数据失败 - 实例: {InstanceCode}", callbackEvent.InstanceCode);
            }
        }

        // 策略3: 通过数据库查询InstanceCode对应的审批类型
        // 这里需要注入相应的Repository来查询
        // 例如：await _repository.GetApprovalTypeByInstanceCodeAsync(callbackEvent.InstanceCode);

        return null; // 无法确定审批类型
    }
}