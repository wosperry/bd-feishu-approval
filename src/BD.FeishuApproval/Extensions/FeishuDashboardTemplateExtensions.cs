#if NET8_0_OR_GREATER
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BD.FeishuApproval.Dashboard;
using BD.FeishuApproval.Dashboard.Templates;

namespace BD.FeishuApproval.Extensions;

/// <summary>
/// 飞书Dashboard模板系统的扩展方法
/// </summary>
public static class FeishuDashboardTemplateExtensions
{
    /// <summary>
    /// 添加飞书Dashboard模板服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuDashboardTemplates(
        this IServiceCollection services, 
        Action<FeishuDashboardTemplateOptions> configureOptions = null)
    {
        // 配置选项
        var options = new FeishuDashboardTemplateOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        
        // 注册模板提供者
        if (options.TemplateProviderType == typeof(FileSystemTemplateProvider))
        {
            services.AddSingleton<IFeishuDashboardTemplateProvider>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<FileSystemTemplateProvider>>();
                return new FileSystemTemplateProvider(options.CustomTemplatePath, logger);
            });
        }
        else if (options.TemplateProviderType == typeof(EmbeddedResourceTemplateProvider))
        {
            services.AddSingleton<IFeishuDashboardTemplateProvider, EmbeddedResourceTemplateProvider>();
        }
        else
        {
            // 自定义模板提供者
            services.AddSingleton(typeof(IFeishuDashboardTemplateProvider), options.TemplateProviderType);
        }
        
        // 注册核心服务
        services.AddSingleton<ITemplateRenderer, DefaultTemplateRenderer>();
        services.AddSingleton<ITemplateCache, InMemoryTemplateCache>();
        services.AddSingleton<IFeishuDashboardTemplateService, FeishuDashboardTemplateService>();
        
        return services;
    }
    
    /// <summary>
    /// 使用新的飞书Dashboard (基于模板系统)
    /// </summary>
    /// <param name="endpoints">端点构建器</param>
    /// <param name="options">Dashboard选项</param>
    /// <returns>端点构建器</returns>
    public static IEndpointRouteBuilder MapFeishuDashboardV2(
        this IEndpointRouteBuilder endpoints,
        FeishuDashboardOptions options = null)
    {
        return FeishuDashboardEndpointV2.MapFeishuDashboardV2(endpoints, options);
    }
    
    /// <summary>
    /// 使用新的飞书Dashboard (带模板配置)
    /// </summary>
    /// <param name="endpoints">端点构建器</param>
    /// <param name="configureDashboard">Dashboard配置</param>
    /// <param name="configureTemplates">模板配置</param>
    /// <returns>端点构建器</returns>
    public static IEndpointRouteBuilder MapFeishuDashboardV2(
        this IEndpointRouteBuilder endpoints,
        Action<FeishuDashboardOptions> configureDashboard = null,
        Action<FeishuDashboardTemplateOptions> configureTemplates = null)
    {
        var dashboardOptions = new FeishuDashboardOptions();
        configureDashboard?.Invoke(dashboardOptions);
        
        // 如果还未注册模板服务，则注册它们
        var serviceProvider = endpoints.ServiceProvider;
        if (serviceProvider.GetService<IFeishuDashboardTemplateService>() == null)
        {
            throw new InvalidOperationException(
                "飞书Dashboard模板服务未注册。请在ConfigureServices中调用 services.AddFeishuDashboardTemplates()");
        }
        
        return FeishuDashboardEndpointV2.MapFeishuDashboardV2(endpoints, dashboardOptions);
    }
}

/// <summary>
/// 飞书Dashboard模板系统的便捷配置扩展
/// </summary>
public static class FeishuDashboardTemplateConfigurationExtensions
{
    /// <summary>
    /// 使用开发模式配置（热重载、禁用缓存）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="templatePath">自定义模板路径（可选）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuDashboardTemplatesForDevelopment(
        this IServiceCollection services,
        string templatePath = null)
    {
        return services.AddFeishuDashboardTemplates(options =>
        {
            options.EnableDevelopmentMode();
            
            if (!string.IsNullOrEmpty(templatePath))
            {
                options.UseFileSystemTemplates(templatePath);
            }
        });
    }
    
    /// <summary>
    /// 使用生产模式配置（启用缓存、使用嵌入资源）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuDashboardTemplatesForProduction(
        this IServiceCollection services)
    {
        return services.AddFeishuDashboardTemplates(options =>
        {
            options.UseEmbeddedTemplates()
                   .AddVariable("Environment", "Production");
        });
    }
    
    /// <summary>
    /// 使用文件系统模板（适合自定义场景）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="templatePath">模板文件路径</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuDashboardTemplatesFromFileSystem(
        this IServiceCollection services,
        string templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
            throw new ArgumentException("Template path cannot be null or empty", nameof(templatePath));
            
        return services.AddFeishuDashboardTemplates(options =>
        {
            options.UseFileSystemTemplates(templatePath);
        });
    }
}
#endif