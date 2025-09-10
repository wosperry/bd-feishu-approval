using BD.FeishuApproval.Abstractions.Definitions;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Abstractions.Services;
using BD.FeishuApproval.Extensions;
using BD.FeishuApproval.Handlers;
using BD.FeishuApproval.Shared.Abstractions;
using BD.FeishuApproval.Shared.Dtos.Instances;
using BD.FeishuApproval.Shared.Events;
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
public class ApprovalService(
    IApprovalHandlerRegistry handlerRegistry,
    IFeishuApprovalInstanceService instanceService,
    IServiceProvider serviceProvider,
    ILogger<ApprovalService> logger,
    IFeishuUserService userService,
    IFeishuApprovalDefinitionService definitionService)
    : IApprovalService
{
    /// <summary>
    /// 创建审批实例
    /// 正确的职责划分：ApprovalService负责创建，Handler提供校验和生命周期钩子
    /// </summary>
    public async Task<CreateInstanceResult> CreateApprovalAsync<T, TId>(T request, TId userId) 
        where T : class, IFeishuApprovalRequest, new()
        where TId : struct
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var approvalCode = request.GetApprovalCode();
        logger.LogInformation("开始创建审批 - 类型: {ApprovalCode}", approvalCode);

        // 通过Registry获取对应的Handler（用于校验和生命周期钩子）
        var handler = handlerRegistry.GetHandler<T>(serviceProvider);
        if (handler == null)
        {
            var errorMsg = $"未找到审批类型 '{approvalCode}' 对应的处理器，请确保已注册相应的Handler";
            logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        try
        {
            // 1. 委托给Handler进行验证
            await handler.ValidateAsync(request);
            
            // 2. 委托给Handler进行预处理
            await handler.PreProcessAsync(request);

            // 3. ApprovalService负责实际的API调用
            logger.LogDebug("调用飞书API创建审批实例 - 类型: {ApprovalCode}", approvalCode);

            var userOpenId = await userService.GetUserOpenIdAsync(userId.ToString(), "user_id");
            var feishuRequestBody = await definitionService.CreateFeishuApprovalRequestBody(userOpenId, request);


            var result = await instanceService.CallFeishuCreateInstanceApiAsync(feishuRequestBody);

            if (!result.IsSuccess || result.Data == null)
            {
                throw new InvalidOperationException($"创建审批实例失败: {result.Message}");
            }

            // 4. 委托给Handler进行后处理
            await handler.PostProcessAsync(request, result.Data);

            logger.LogInformation("审批创建成功 - 类型: {ApprovalCode}, 实例ID: {InstanceCode}", 
                approvalCode, result.Data.InstanceCode);
            
            return result.Data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建审批失败 - 类型: {ApprovalCode}", approvalCode);
            
            // 委托给Handler处理创建失败
            try
            {
                await handler.HandleCreateFailureAsync(request, ex);
            }
            catch (Exception handlerEx)
            {
                logger.LogWarning(handlerEx, "Handler处理创建失败时发生异常");
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
        var ApprovalCode = ExtractApprovalCodeFromCallback(callbackEvent);
        
        if (string.IsNullOrEmpty(ApprovalCode))
        {
            logger.LogWarning("无法从回调事件中解析审批类型 - 实例: {InstanceCode}", callbackEvent.Event.ApprovalCode);
            throw new InvalidOperationException("无法确定审批类型，回调事件缺少必要信息");
        }

        await HandleApprovalCallbackAsync(ApprovalCode, callbackEvent);
    }

    /// <summary>
    /// 根据审批类型处理回调事件
    /// </summary>
    public async Task HandleApprovalCallbackAsync(string ApprovalCode, FeishuCallbackEvent callbackEvent)
    {
        if (string.IsNullOrEmpty(ApprovalCode))
            throw new ArgumentException("审批类型不能为空", nameof(ApprovalCode));
        
        if (callbackEvent == null)
            throw new ArgumentNullException(nameof(callbackEvent));

        logger.LogInformation("开始处理审批回调 - 类型: {ApprovalCode}, 实例: {InstanceCode}, 状态: {Status}", 
            ApprovalCode, callbackEvent.Event.ApprovalCode, callbackEvent.Type);

        // 通过Registry获取对应的Handler
        var handler = handlerRegistry.GetHandler(ApprovalCode, serviceProvider);
        if (handler == null)
        {
            var errorMsg = $"未找到审批类型 '{ApprovalCode}' 对应的处理器";
            logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        try
        {
            // 委托给Handler处理回调
            await handler.HandleCallbackAsync(callbackEvent);
            
            logger.LogInformation("审批回调处理完成 - 类型: {ApprovalCode}, 实例: {InstanceCode}", 
                ApprovalCode, callbackEvent.Event.ApprovalCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理审批回调失败 - 类型: {ApprovalCode}, 实例: {InstanceCode}", 
                ApprovalCode, callbackEvent.Event.ApprovalCode);
            throw;
        }
    }

    /// <summary>
    /// 检查指定审批类型是否已注册Handler
    /// </summary>
    public bool IsApprovalCodeSupported(string ApprovalCode)
    {
        if (string.IsNullOrEmpty(ApprovalCode))
            return false;
        
        return handlerRegistry.IsRegistered(ApprovalCode);
    }

    /// <summary>
    /// 检查指定审批请求类型是否已注册Handler
    /// </summary>
    public bool IsApprovalCodeSupported<T>() where T : class, IFeishuApprovalRequest, new()
    {
        var ApprovalCode = new T().GetApprovalCode();
        return IsApprovalCodeSupported(ApprovalCode);
    }

    /// <summary>
    /// 获取所有支持的审批类型
    /// </summary>
    public string[] GetSupportedApprovalCodes()
    {
        return handlerRegistry.GetRegisteredApprovalTypes().ToArray();
    }

    /// <summary>
    /// 从回调事件中提取审批类型
    /// 这是一个策略方法，可能需要根据实际情况调整实现逻辑
    /// </summary>
    private string ExtractApprovalCodeFromCallback(FeishuCallbackEvent callbackEvent)
    {
        // 策略1: 尝试从ApprovalCode直接获取（如果回调事件包含这个字段）
        if (!string.IsNullOrEmpty(callbackEvent.Event.ApprovalCode))
        {
            return callbackEvent.Event.ApprovalCode;
        }

        // 策略2: 从Form数据中解析（需要根据实际JSON结构调整）
        if (!string.IsNullOrEmpty(callbackEvent.Form))
        {
            try
            {
                // 这里可能需要解析JSON来获取审批类型信息
                // 具体实现依赖于飞书回调的数据结构
                // 例如：从Form中的某个字段推断，或者维护InstanceCode到ApprovalCode的映射表
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "解析回调事件Form数据失败 - 实例: {InstanceCode}", callbackEvent.Event.ApprovalCode);
            }
        }

        // 策略3: 通过数据库查询InstanceCode对应的审批类型
        // 这里需要注入相应的Repository来查询
        // 例如：await _repository.GetApprovalCodeByInstanceCodeAsync(callbackEvent.InstanceCode);

        return null; // 无法确定审批类型
    }
}