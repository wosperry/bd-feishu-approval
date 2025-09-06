using System;
using BD.FeishuApproval.Abstractions.Http;
using BD.FeishuApproval.Abstractions.Persistence;
using BD.FeishuApproval.Abstractions.Hooks;
using BD.FeishuApproval.Abstractions.Auth;
using BD.FeishuApproval.Abstractions.Configs;
using BD.FeishuApproval.Abstractions.CodeGen;
using BD.FeishuApproval.Abstractions.Handlers;
using BD.FeishuApproval.Http;
using BD.FeishuApproval.Persistence;
using BD.FeishuApproval.CodeGen;
using BD.FeishuApproval.Handlers;
using BD.FeishuApproval.Callbacks;
using Microsoft.Extensions.DependencyInjection;
using BD.FeishuApproval.Abstractions.Strategies;
using BD.FeishuApproval.Strategies;
using BD.FeishuApproval.Abstractions.Definitions;
using BD.FeishuApproval.Definitions;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Instances;
using BD.FeishuApproval.Auth;
using System.Reflection;

namespace BD.FeishuApproval.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加飞书审批服务到服务容器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="baseAddress">飞书开放平台基础地址，默认为 https://open.feishu.cn</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuApproval(this IServiceCollection services, Uri baseAddress = null)
    {
        services.AddHttpClient<IFeishuApiClient, FeishuApiClient>(client =>
        {
            client.BaseAddress = baseAddress ?? new Uri("https://open.feishu.cn");
        });

        services.AddScoped<IFeishuApprovalRepository, SqlSugarFeishuApprovalRepository>();

        // 默认失败钩子
        services.AddScoped<IFeishuFailureHook, Hooks.DefaultFailureHook>();

        // 默认认证服务
        services.AddHttpClient(nameof(Auth.FeishuAuthService));
        services.AddScoped<IFeishuAuthService, Auth.FeishuAuthService>();

        // 提供一个默认的（基于 DB 的）配置提供者，若使用方未注册自定义实现，则使用该实现
        services.AddScoped<IFeishuConfigProvider, DbFeishuConfigProvider>();

        // 策略工厂
        services.AddSingleton<IApprovalStrategyFactory, ApprovalStrategyFactory>();

        // 审批定义服务
        services.AddScoped<IFeishuApprovalDefinitionService, FeishuApprovalDefinitionService>();

        // 审批订阅服务
        services.AddScoped<IFeishuApprovalSubscriptionService, FeishuApprovalSubscriptionService>();

        // TypeScript代码生成服务
        services.AddScoped<ITypeScriptCodeGenerator, TypeScriptCodeGenerator>();

        // C#代码生成服务
        services.AddScoped<ICSharpCodeGenerator, CSharpCodeGenerator>();

        // 审批处理器注册表
        services.AddApprovalHandlers();

        // 回调服务
        services.AddScoped<IFeishuCallbackService, FeishuCallbackService>();

        // 默认自动扫描当前执行程序集中的处理器（更安全的默认行为）
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            services.AddApprovalHandlersFromAssembly(entryAssembly);
        }

        // 审批实例服务
        services.AddScoped<IFeishuApprovalInstanceService, FeishuApprovalInstanceService>();

        // 健康检查服务
        services.AddScoped<BD.FeishuApproval.Abstractions.Health.IFeishuHealthCheckService, BD.FeishuApproval.Health.FeishuHealthCheckService>();

        // 批量操作服务
        services.AddScoped<BD.FeishuApproval.Abstractions.Batch.IFeishuBatchService, BD.FeishuApproval.Batch.FeishuBatchService>();

        // 管理服务
        services.AddScoped<BD.FeishuApproval.Abstractions.Management.IFeishuManagementService, BD.FeishuApproval.Management.FeishuManagementService>();

        // IFeishuConfigProvider 由使用方实现并注册；若未注册可在运行时抛出明显错误

        return services;
    }

    /// <summary>
    /// 添加飞书审批服务到服务容器，并指定要扫描的程序集
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="baseAddress">飞书开放平台基础地址，默认为 https://open.feishu.cn</param>
    /// <param name="assembliesForHandlerScan">要扫描审批处理器的程序集列表。如果为null或空，则扫描入口程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuApproval(this IServiceCollection services, Uri baseAddress, params Assembly[] assembliesForHandlerScan)
    {
        // 添加基础服务
        services.AddFeishuApproval(baseAddress);

        // 移除默认的程序集扫描
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            // 这里需要先移除默认添加的扫描，但由于已经调用了AddFeishuApproval，所以我们重新设计
            // 让用户可以显式控制扫描
        }

        // 扫描指定的程序集
        if (assembliesForHandlerScan?.Length > 0)
        {
            foreach (var assembly in assembliesForHandlerScan)
            {
                services.AddApprovalHandlersFromAssembly(assembly);
            }
        }

        return services;
    }

    /// <summary>
    /// 添加飞书审批服务，不自动扫描任何程序集（需要手动注册处理器）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="baseAddress">飞书开放平台基础地址</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuApprovalWithoutAutoScan(this IServiceCollection services, Uri baseAddress = null)
    {
        services.AddHttpClient<IFeishuApiClient, FeishuApiClient>(client =>
        {
            client.BaseAddress = baseAddress ?? new Uri("https://open.feishu.cn");
        });

        services.AddScoped<IFeishuApprovalRepository, SqlSugarFeishuApprovalRepository>();

        // 默认失败钩子
        services.AddScoped<IFeishuFailureHook, Hooks.DefaultFailureHook>();

        // 默认认证服务
        services.AddHttpClient(nameof(Auth.FeishuAuthService));
        services.AddScoped<IFeishuAuthService, Auth.FeishuAuthService>();

        // 提供一个默认的（基于 DB 的）配置提供者，若使用方未注册自定义实现，则使用该实现
        services.AddScoped<IFeishuConfigProvider, DbFeishuConfigProvider>();

        // 策略工厂
        services.AddSingleton<IApprovalStrategyFactory, ApprovalStrategyFactory>();

        // 审批定义服务
        services.AddScoped<IFeishuApprovalDefinitionService, FeishuApprovalDefinitionService>();

        // 审批订阅服务
        services.AddScoped<IFeishuApprovalSubscriptionService, FeishuApprovalSubscriptionService>();

        // TypeScript代码生成服务
        services.AddScoped<ITypeScriptCodeGenerator, TypeScriptCodeGenerator>();

        // C#代码生成服务
        services.AddScoped<ICSharpCodeGenerator, CSharpCodeGenerator>();

        // 审批处理器注册表（不自动扫描）
        services.AddApprovalHandlers();

        // 回调服务
        services.AddScoped<IFeishuCallbackService, FeishuCallbackService>();

        // 审批实例服务
        services.AddScoped<IFeishuApprovalInstanceService, FeishuApprovalInstanceService>();

        // 健康检查服务
        services.AddScoped<BD.FeishuApproval.Abstractions.Health.IFeishuHealthCheckService, BD.FeishuApproval.Health.FeishuHealthCheckService>();

        // 批量操作服务
        services.AddScoped<BD.FeishuApproval.Abstractions.Batch.IFeishuBatchService, BD.FeishuApproval.Batch.FeishuBatchService>();

        // 管理服务
        services.AddScoped<BD.FeishuApproval.Abstractions.Management.IFeishuManagementService, BD.FeishuApproval.Management.FeishuManagementService>();

        return services;
    }

    // auto-migrate extension removed from core; implement in host app instead
}


