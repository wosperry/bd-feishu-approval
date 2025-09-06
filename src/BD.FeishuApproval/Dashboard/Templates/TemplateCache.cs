using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BD.FeishuApproval.Dashboard.Templates;

/// <summary>
/// 模板缓存项
/// </summary>
public class TemplateCacheItem
{
    public string Content { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime? LastModified { get; set; }
    
    public bool IsExpired(int expirationMinutes)
    {
        return DateTime.UtcNow - CachedAt > TimeSpan.FromMinutes(expirationMinutes);
    }
    
    public bool IsStale(DateTime? currentLastModified)
    {
        if (currentLastModified == null || LastModified == null)
            return false;
            
        return currentLastModified > LastModified;
    }
}

/// <summary>
/// 模板缓存服务
/// 提供模板内容的内存缓存功能
/// </summary>
public interface ITemplateCache
{
    Task<string> GetAsync(string key);
    Task SetAsync(string key, string content, DateTime? lastModified = null);
    Task RemoveAsync(string key);
    Task ClearAsync();
    Task<bool> IsValidAsync(string key, DateTime? currentLastModified);
}

/// <summary>
/// 默认内存模板缓存实现
/// </summary>
public class InMemoryTemplateCache : ITemplateCache
{
    private readonly ConcurrentDictionary<string, TemplateCacheItem> _cache = new();
    private readonly FeishuDashboardTemplateOptions _options;
    private readonly ILogger<InMemoryTemplateCache> _logger;
    
    public InMemoryTemplateCache(FeishuDashboardTemplateOptions options, ILogger<InMemoryTemplateCache> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<string> GetAsync(string key)
    {
        if (!_options.CacheEnabled)
            return null;
        
        if (_cache.TryGetValue(key, out var item))
        {
            if (item.IsExpired(_options.CacheExpirationMinutes))
            {
                _cache.TryRemove(key, out _);
                _logger.LogDebug("Cache item expired and removed: {Key}", key);
                return null;
            }
            
            _logger.LogDebug("Cache hit for template: {Key}", key);
            return item.Content;
        }
        
        return null;
    }
    
    public async Task SetAsync(string key, string content, DateTime? lastModified = null)
    {
        if (!_options.CacheEnabled)
            return;
        
        var item = new TemplateCacheItem
        {
            Content = content,
            CachedAt = DateTime.UtcNow,
            LastModified = lastModified
        };
        
        _cache.AddOrUpdate(key, item, (k, existing) => item);
        _logger.LogDebug("Cached template: {Key}", key);
    }
    
    public async Task RemoveAsync(string key)
    {
        if (_cache.TryRemove(key, out _))
        {
            _logger.LogDebug("Removed cached template: {Key}", key);
        }
    }
    
    public async Task ClearAsync()
    {
        _cache.Clear();
        _logger.LogDebug("Cleared all cached templates");
    }
    
    public async Task<bool> IsValidAsync(string key, DateTime? currentLastModified)
    {
        if (!_options.CacheEnabled)
            return false;
        
        if (_cache.TryGetValue(key, out var item))
        {
            return !item.IsExpired(_options.CacheExpirationMinutes) && 
                   !item.IsStale(currentLastModified);
        }
        
        return false;
    }
}