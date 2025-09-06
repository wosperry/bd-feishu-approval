using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BD.FeishuApproval.Dashboard.Templates;

/// <summary>
/// 飞书Dashboard模板服务
/// 统一管理模板加载、缓存和渲染
/// </summary>
public interface IFeishuDashboardTemplateService
{
    /// <summary>
    /// 渲染模板
    /// </summary>
    /// <param name="templateName">模板名称</param>
    /// <param name="variables">模板变量</param>
    /// <returns>渲染后的HTML内容</returns>
    Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> variables = null);
    
    /// <summary>
    /// 检查模板是否存在
    /// </summary>
    /// <param name="templateName">模板名称</param>
    /// <returns>是否存在</returns>
    Task<bool> TemplateExistsAsync(string templateName);
    
    /// <summary>
    /// 清除缓存
    /// </summary>
    /// <returns></returns>
    Task ClearCacheAsync();
}

/// <summary>
/// 默认飞书Dashboard模板服务实现
/// </summary>
public class FeishuDashboardTemplateService : IFeishuDashboardTemplateService
{
    private readonly IFeishuDashboardTemplateProvider _templateProvider;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ITemplateCache _templateCache;
    private readonly FeishuDashboardTemplateOptions _options;
    private readonly ILogger<FeishuDashboardTemplateService> _logger;
    
    public FeishuDashboardTemplateService(
        IFeishuDashboardTemplateProvider templateProvider,
        ITemplateRenderer templateRenderer,
        ITemplateCache templateCache,
        FeishuDashboardTemplateOptions options,
        ILogger<FeishuDashboardTemplateService> logger)
    {
        _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _templateCache = templateCache ?? throw new ArgumentNullException(nameof(templateCache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> variables = null)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));
        
        try
        {
            // 合并默认变量和用户变量
            var allVariables = new Dictionary<string, string>(_options.CustomVariables);
            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    allVariables[variable.Key] = variable.Value;
                }
            }
            
            // 添加系统预定义变量
            AddSystemVariables(allVariables);
            
            // 生成缓存键
            var cacheKey = GenerateCacheKey(templateName, allVariables);
            
            // 检查缓存
            var lastModified = await _templateProvider.GetTemplateLastModifiedAsync(templateName);
            var cachedContent = await _templateCache.GetAsync(cacheKey);
            
            if (cachedContent != null && await _templateCache.IsValidAsync(cacheKey, lastModified))
            {
                _logger.LogDebug("Using cached template: {TemplateName}", templateName);
                return cachedContent;
            }
            
            // 加载和渲染模板
            using var templateStream = await _templateProvider.GetTemplateAsync(templateName);
            var renderedContent = await _templateRenderer.RenderAsync(templateStream, allVariables);
            
            // 缓存结果
            await _templateCache.SetAsync(cacheKey, renderedContent, lastModified);
            
            _logger.LogDebug("Rendered template: {TemplateName}", templateName);
            return renderedContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template: {TemplateName}", templateName);
            throw;
        }
    }
    
    public async Task<bool> TemplateExistsAsync(string templateName)
    {
        try
        {
            return await _templateProvider.TemplateExistsAsync(templateName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check template existence: {TemplateName}", templateName);
            return false;
        }
    }
    
    public async Task ClearCacheAsync()
    {
        await _templateCache.ClearAsync();
        _logger.LogInformation("Template cache cleared");
    }
    
    private void AddSystemVariables(Dictionary<string, string> variables)
    {
        variables["ApiPrefix"] = _options.ApiPrefix;
        variables["PathPrefix"] = _options.PathPrefix;
        variables["ManagePath"] = _options.ManagePath;
        variables["Version"] = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown";
        variables["Timestamp"] = DateTime.UtcNow.ToString("O");
    }
    
    private string GenerateCacheKey(string templateName, Dictionary<string, string> variables)
    {
        // 简单的缓存键生成，包含模板名称和变量哈希
        var variablesHash = string.Join("|", variables.Select(kv => $"{kv.Key}={kv.Value}"));
        return $"{templateName}#{variablesHash.GetHashCode():X}";
    }
}