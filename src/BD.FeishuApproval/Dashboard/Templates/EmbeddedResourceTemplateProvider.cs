using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BD.FeishuApproval.Dashboard.Templates;

/// <summary>
/// 基于嵌入资源的模板提供者
/// 从程序集的嵌入资源中加载HTML模板
/// </summary>
public class EmbeddedResourceTemplateProvider : IFeishuDashboardTemplateProvider
{
    private readonly Assembly _assembly;
    private readonly string _resourcePrefix;
    private readonly ILogger<EmbeddedResourceTemplateProvider> _logger;
    
    private static readonly Dictionary<string, DateTime> _resourceTimestamps = new()
    {
        // 使用编译时间作为资源的"修改时间"
        ["dashboard.html"] = DateTime.UtcNow,
        ["manage.html"] = DateTime.UtcNow,
        ["css/dashboard.css"] = DateTime.UtcNow,
        ["js/dashboard.js"] = DateTime.UtcNow
    };
    
    public EmbeddedResourceTemplateProvider(ILogger<EmbeddedResourceTemplateProvider> logger)
    {
        _assembly = typeof(EmbeddedResourceTemplateProvider).Assembly;
        _resourcePrefix = "BD.FeishuApproval.Dashboard.Templates.Resources.";
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<Stream> GetTemplateAsync(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));
        
        var resourceName = _resourcePrefix + templateName.Replace('/', '.');
        
        var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            _logger.LogWarning("Embedded template resource not found: {ResourceName}", resourceName);
            throw new FileNotFoundException($"Embedded template resource not found: {templateName}");
        }
        
        _logger.LogDebug("Loaded embedded template: {TemplateName}", templateName);
        return stream;
    }
    
    public async Task<bool> TemplateExistsAsync(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return false;
            
        var resourceName = _resourcePrefix + templateName.Replace('/', '.');
        var resourceNames = _assembly.GetManifestResourceNames();
        
        return resourceNames.Contains(resourceName);
    }
    
    public async Task<DateTime?> GetTemplateLastModifiedAsync(string templateName)
    {
        if (!await TemplateExistsAsync(templateName))
            return null;
        
        // 对于嵌入资源，返回预设的时间戳或程序集的编译时间
        return _resourceTimestamps.TryGetValue(templateName, out var timestamp) 
            ? timestamp 
            : GetAssemblyTimestamp();
    }
    
    public async Task<IEnumerable<string>> GetAvailableTemplatesAsync()
    {
        var resourceNames = _assembly.GetManifestResourceNames();
        var templateNames = resourceNames
            .Where(name => name.StartsWith(_resourcePrefix) && name.EndsWith(".html"))
            .Select(name => name.Substring(_resourcePrefix.Length))
            .Select(name => name.Replace('.', '/'))
            .Select(name => name.Substring(0, name.Length - 5) + ".html") // 修正扩展名
            .ToList();
        
        _logger.LogDebug("Found {Count} embedded templates", templateNames.Count);
        return templateNames;
    }
    
    private DateTime GetAssemblyTimestamp()
    {
        try
        {
            var location = _assembly.Location;
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
            {
                return File.GetLastWriteTimeUtc(location);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get assembly timestamp");
        }
        
        return DateTime.UtcNow;
    }
}