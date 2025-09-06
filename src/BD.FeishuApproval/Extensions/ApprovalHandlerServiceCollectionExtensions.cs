using BD.FeishuApproval.Abstractions.Handlers;
using BD.FeishuApproval.Handlers;
using BD.FeishuApproval.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Extensions;

/// <summary>
/// 审批处理器依赖注入扩展
/// </summary>
public static class ApprovalHandlerServiceCollectionExtensions
{
    /// <summary>
    /// 添加审批处理器支持
    /// </summary>
    public static IServiceCollection AddApprovalHandlers(this IServiceCollection services)
    {
        // 注册审批处理器注册表
        services.AddSingleton<IApprovalHandlerRegistry, ApprovalHandlerRegistry>();

        return services;
    }

    /// <summary>
    /// 从指定程序集自动发现并注册审批处理器
    /// </summary>
    public static IServiceCollection AddApprovalHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => typeof(IApprovalHandler).IsAssignableFrom(type))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            // 注册处理器到DI容器
            services.AddTransient(handlerType);

            // 获取审批类型
            var approvalType = GetApprovalTypeFromHandler(handlerType);
            if (!string.IsNullOrEmpty(approvalType))
            {
                // 在服务提供程序构建后注册到注册表
                services.AddSingleton(provider =>
                {
                    var registry = provider.GetRequiredService<IApprovalHandlerRegistry>();
                    registry.RegisterHandler(handlerType, approvalType);
                    return registry;
                });
            }
        }

        return services;
    }

    /// <summary>
    /// 从当前应用程序域的所有程序集自动发现并注册审批处理器
    /// </summary>
    public static IServiceCollection AddApprovalHandlersFromAllAssemblies(this IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Where(assembly => !assembly.GlobalAssemblyCache)
            .ToList();

        foreach (var assembly in assemblies)
        {
            try
            {
                services.AddApprovalHandlersFromAssembly(assembly);
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 忽略无法加载的程序集
                Console.WriteLine($"警告: 无法从程序集 {assembly.FullName} 加载类型: {string.Join(", ", ex.LoaderExceptions?.Select(e => e.Message) ?? Array.Empty<string>())}");
            }
        }

        return services;
    }

    /// <summary>
    /// 手动注册审批处理器
    /// </summary>
    public static IServiceCollection AddApprovalHandler<THandler, TApprovalDto>(this IServiceCollection services)
        where THandler : class, IApprovalHandler<TApprovalDto>
        where TApprovalDto : class, IFeishuApprovalRequest, new()
    {
        // 注册到DI容器
        services.AddTransient<THandler>();
        services.AddTransient<IApprovalHandler<TApprovalDto>, THandler>();

        // 注册一个启动时执行的hosted service来初始化注册表
        services.AddHostedService<HandlerRegistrationHostedService<THandler, TApprovalDto>>();

        return services;
    }

    /// <summary>
    /// 从处理器类型获取审批类型
    /// </summary>
    private static string GetApprovalTypeFromHandler(Type handlerType)
    {
        // 尝试从ApprovalType属性获取
        var approvalTypeAttribute = handlerType.GetCustomAttribute<ApprovalTypeAttribute>();
        if (approvalTypeAttribute != null)
        {
            return approvalTypeAttribute.Type;
        }

        // 尝试从泛型参数获取
        var genericInterfaces = handlerType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IApprovalHandler<>))
            .ToList();

        if (genericInterfaces.Any())
        {
            var dtoType = genericInterfaces.First().GetGenericArguments().First();
            try
            {
                var dtoInstance = Activator.CreateInstance(dtoType) as IFeishuApprovalRequest;
                return dtoInstance?.GetApprovalType() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // 尝试创建实例并获取ApprovalType属性
        try
        {
            if (Activator.CreateInstance(handlerType) is IApprovalHandler handler)
            {
                return handler.ApprovalType;
            }
        }
        catch
        {
            // 忽略创建失败的情况
        }

        return string.Empty;
    }
}

/// <summary>
/// Handler注册初始化服务
/// </summary>
internal class HandlerRegistrationHostedService<THandler, TApprovalDto> : IHostedService
    where THandler : class, IApprovalHandler<TApprovalDto>
    where TApprovalDto : class, IFeishuApprovalRequest, new()
{
    private readonly IServiceProvider _serviceProvider;

    public HandlerRegistrationHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IApprovalHandlerRegistry>();
        var approvalType = new TApprovalDto().GetApprovalType();
        
        registry.RegisterHandler(typeof(THandler), approvalType);
        
        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}