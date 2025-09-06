using BD.FeishuApproval.Abstractions.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Handlers;

/// <summary>
/// 审批处理器注册表
/// 负责管理所有已注册的审批处理器
/// </summary>
public class ApprovalHandlerRegistry : IApprovalHandlerRegistry
{
    private readonly ConcurrentDictionary<string, Type> _handlerTypes = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApprovalHandlerRegistry> _logger;

    public ApprovalHandlerRegistry(IServiceProvider serviceProvider, ILogger<ApprovalHandlerRegistry> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 注册审批处理器类型
    /// </summary>
    public void RegisterHandler<THandler>(string approvalCode) where THandler : class, IApprovalHandler
    {
        RegisterHandler(typeof(THandler), approvalCode);
    }

    /// <summary>
    /// 注册审批处理器类型
    /// </summary>
    public void RegisterHandler(Type handlerType, string approvalCode)
    {
        if (!typeof(IApprovalHandler).IsAssignableFrom(handlerType))
        {
            throw new ArgumentException($"处理器类型 {handlerType.Name} 必须实现 IApprovalHandler 接口");
        }

        _handlerTypes.TryAdd(approvalCode, handlerType);
        _logger.LogInformation("注册审批处理器: {ApprovalCode} -> {HandlerType}", approvalCode, handlerType.Name);
    }

    /// <summary>
    /// 获取审批处理器实例
    /// </summary>
    public IApprovalHandler? GetHandler(string approvalCode, IServiceProvider? serviceProvider = null)
    {
        if (!_handlerTypes.TryGetValue(approvalCode, out var handlerType))
        {
            _logger.LogWarning("未找到审批类型 {ApprovalType} 的处理器", approvalCode);
            return null;
        }

        try
        {
            // 优先使用传入的服务提供程序（通常是scoped），否则使用默认的根服务提供程序
            var provider = serviceProvider ?? _serviceProvider;
            return (IApprovalHandler)provider.GetRequiredService(handlerType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建审批处理器实例失败: {HandlerType}", handlerType.Name);
            return null;
        }
    }

    /// <summary>
    /// 获取泛型审批处理器实例
    /// </summary>
    public IApprovalHandler<T>? GetHandler<T>(IServiceProvider? serviceProvider = null) where T : class, BD.FeishuApproval.Shared.Abstractions.IFeishuApprovalRequest, new()
    {
        var approvalCode = new T().GetApprovalCode();
        var handler = GetHandler(approvalCode, serviceProvider);
        return handler as IApprovalHandler<T>;
    }

    /// <summary>
    /// 获取所有已注册的审批类型
    /// </summary>
    public IEnumerable<string> GetRegisteredApprovalTypes()
    {
        return _handlerTypes.Keys.ToList();
    }

    /// <summary>
    /// 检查是否已注册指定的审批类型
    /// </summary>
    public bool IsRegistered(string approvalCode)
    {
        return _handlerTypes.ContainsKey(approvalCode);
    }
}

/// <summary>
/// 审批处理器注册表接口
/// </summary>
public interface IApprovalHandlerRegistry
{
    /// <summary>
    /// 注册审批处理器类型
    /// </summary>
    void RegisterHandler<THandler>(string approvalCode) where THandler : class, IApprovalHandler;

    /// <summary>
    /// 注册审批处理器类型
    /// </summary>
    void RegisterHandler(Type handlerType, string approvalCode);

    /// <summary>
    /// 获取审批处理器实例
    /// </summary>
    IApprovalHandler? GetHandler(string approvalCode, IServiceProvider? serviceProvider = null);

    /// <summary>
    /// 获取泛型审批处理器实例
    /// </summary>
    IApprovalHandler<T>? GetHandler<T>(IServiceProvider? serviceProvider = null) where T : class, BD.FeishuApproval.Shared.Abstractions.IFeishuApprovalRequest, new();

    /// <summary>
    /// 获取所有已注册的审批类型
    /// </summary>
    IEnumerable<string> GetRegisteredApprovalTypes();

    /// <summary>
    /// 检查是否已注册指定的审批类型
    /// </summary>
    bool IsRegistered(string approvalCode);
}