using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Dashboard.Templates;

/// <summary>
/// 飞书Dashboard模板提供者接口
/// 支持从不同来源加载HTML模板文件
/// </summary>
public interface IFeishuDashboardTemplateProvider
{
    /// <summary>
    /// 获取模板内容
    /// </summary>
    /// <param name="templateName">模板名称，如 "dashboard.html", "manage.html"</param>
    /// <returns>模板内容流</returns>
    Task<Stream> GetTemplateAsync(string templateName);
    
    /// <summary>
    /// 检查模板是否存在
    /// </summary>
    /// <param name="templateName">模板名称</param>
    /// <returns>是否存在</returns>
    Task<bool> TemplateExistsAsync(string templateName);
    
    /// <summary>
    /// 获取模板的最后修改时间（用于缓存）
    /// </summary>
    /// <param name="templateName">模板名称</param>
    /// <returns>最后修改时间</returns>
    Task<DateTime?> GetTemplateLastModifiedAsync(string templateName);
    
    /// <summary>
    /// 获取所有可用的模板列表
    /// </summary>
    /// <returns>模板名称列表</returns>
    Task<IEnumerable<string>> GetAvailableTemplatesAsync();
}