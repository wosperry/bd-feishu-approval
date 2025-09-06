using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BD.FeishuApproval.Dashboard.Templates;

/// <summary>
/// 基于文件系统的模板提供者
/// 从指定目录加载HTML模板文件
/// </summary>
public class FileSystemTemplateProvider : IFeishuDashboardTemplateProvider
{
    private readonly string _templatePath;
    private readonly ILogger<FileSystemTemplateProvider> _logger;
    
    public FileSystemTemplateProvider(string templatePath, ILogger<FileSystemTemplateProvider> logger)
    {
        _templatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (!Directory.Exists(_templatePath))
        {
            _logger.LogWarning("Template directory does not exist: {TemplatePath}", _templatePath);
        }
    }
    
    public async Task<Stream> GetTemplateAsync(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));
        
        var filePath = Path.Combine(_templatePath, templateName);
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Template file not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Template file not found: {templateName}");
        }
        
        try
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _logger.LogDebug("Loaded template: {TemplateName} from {FilePath}", templateName, filePath);
            return fileStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load template: {TemplateName}", templateName);
            throw;
        }
    }
    
    public async Task<bool> TemplateExistsAsync(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return false;
            
        var filePath = Path.Combine(_templatePath, templateName);
        return File.Exists(filePath);
    }
    
    public async Task<DateTime?> GetTemplateLastModifiedAsync(string templateName)
    {
        if (!await TemplateExistsAsync(templateName))
            return null;
            
        var filePath = Path.Combine(_templatePath, templateName);
        return File.GetLastWriteTimeUtc(filePath);
    }
    
    public async Task<IEnumerable<string>> GetAvailableTemplatesAsync()
    {
        if (!Directory.Exists(_templatePath))
            return Enumerable.Empty<string>();
        
        try
        {
            var files = Directory.GetFiles(_templatePath, "*.html", SearchOption.AllDirectories);
            var templateNames = files.Select(f => Path.GetRelativePath(_templatePath, f))
                                    .Select(f => f.Replace('\\', '/')) // 统一使用正斜杠
                                    .ToList();
            
            _logger.LogDebug("Found {Count} templates in {TemplatePath}", templateNames.Count, _templatePath);
            return templateNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate templates in {TemplatePath}", _templatePath);
            return Enumerable.Empty<string>();
        }
    }
}