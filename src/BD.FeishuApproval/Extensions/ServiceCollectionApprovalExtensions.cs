using BD.FeishuApproval.Abstractions.Services;
using BD.FeishuApproval.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace BD.FeishuApproval.Extensions;

/// <summary>
/// 审批服务的简化注册扩展
/// 为第三方开发者提供简洁的API
/// </summary>
public static class ServiceCollectionApprovalExtensions
{
    /// <summary>
    /// 一站式注册审批服务
    /// 包括ApprovalService、Registry以及自动扫描当前程序集的Handler
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuApprovalServices(this IServiceCollection services)
    {
        return services.AddFeishuApprovalServices(Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// 一站式注册审批服务并扫描指定程序集
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描Handler的程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuApprovalServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        // 注册核心审批服务
        services.TryAddScoped<IApprovalService, ApprovalService>();
        
        // 注册Handler基础设施
        services.AddApprovalHandlers();
        
        // 扫描并注册Handler
        foreach (var assembly in assemblies)
        {
            services.AddApprovalHandlersFromAssembly(assembly);
        }
        
        return services;
    }

    /// <summary>
    /// 仅注册审批服务核心组件（不自动扫描Handler）
    /// 适用于需要手动注册Handler的场景
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuApprovalCoreServices(this IServiceCollection services)
    {
        services.TryAddScoped<IApprovalService, ApprovalService>();
        services.AddApprovalHandlers();
        return services;
    }
}