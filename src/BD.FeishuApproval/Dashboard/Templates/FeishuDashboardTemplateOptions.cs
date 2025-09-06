using System;
using System.Collections.Generic;

namespace BD.FeishuApproval.Dashboard.Templates;

/// <summary>
/// 飞书Dashboard模板配置选项
/// </summary>
public class FeishuDashboardTemplateOptions
{
    /// <summary>
    /// 模板提供者类型
    /// </summary>
    public Type TemplateProviderType { get; set; } = typeof(EmbeddedResourceTemplateProvider);
    
    /// <summary>
    /// 自定义模板根路径（当使用FileSystemTemplateProvider时）
    /// </summary>
    public string CustomTemplatePath { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否启用模板缓存
    /// </summary>
    public bool CacheEnabled { get; set; } = true;
    
    /// <summary>
    /// 缓存过期时间（分钟）
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// 是否启用热重载（开发模式）
    /// </summary>
    public bool HotReloadEnabled { get; set; } = false;
    
    /// <summary>
    /// 自定义模板变量
    /// 这些变量可以在模板中使用 {{variableName}} 的形式替换
    /// </summary>
    public Dictionary<string, string> CustomVariables { get; set; } = new();
    
    /// <summary>
    /// API路径前缀
    /// </summary>
    public string ApiPrefix { get; set; } = "/feishu/api";
    
    /// <summary>
    /// Dashboard路径前缀
    /// </summary>
    public string PathPrefix { get; set; } = "/feishu";
    
    /// <summary>
    /// 管理页面路径
    /// </summary>
    public string ManagePath { get; set; } = "/feishu/manage";
    
    /// <summary>
    /// 使用文件系统模板提供者
    /// </summary>
    /// <param name="templatePath">模板文件路径</param>
    /// <returns>配置对象</returns>
    public FeishuDashboardTemplateOptions UseFileSystemTemplates(string templatePath)
    {
        TemplateProviderType = typeof(FileSystemTemplateProvider);
        CustomTemplatePath = templatePath;
        return this;
    }
    
    /// <summary>
    /// 使用内嵌资源模板提供者（默认）
    /// </summary>
    /// <returns>配置对象</returns>
    public FeishuDashboardTemplateOptions UseEmbeddedTemplates()
    {
        TemplateProviderType = typeof(EmbeddedResourceTemplateProvider);
        return this;
    }
    
    /// <summary>
    /// 使用自定义模板提供者
    /// </summary>
    /// <typeparam name="T">自定义提供者类型</typeparam>
    /// <returns>配置对象</returns>
    public FeishuDashboardTemplateOptions UseCustomTemplateProvider<T>()
        where T : class, IFeishuDashboardTemplateProvider
    {
        TemplateProviderType = typeof(T);
        return this;
    }
    
    /// <summary>
    /// 添加自定义变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="value">变量值</param>
    /// <returns>配置对象</returns>
    public FeishuDashboardTemplateOptions AddVariable(string name, string value)
    {
        CustomVariables[name] = value;
        return this;
    }
    
    /// <summary>
    /// 启用开发模式（热重载、禁用缓存等）
    /// </summary>
    /// <returns>配置对象</returns>
    public FeishuDashboardTemplateOptions EnableDevelopmentMode()
    {
        HotReloadEnabled = true;
        CacheEnabled = false;
        return this;
    }
}