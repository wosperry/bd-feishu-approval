using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Dashboard.Templates;

/// <summary>
/// 模板渲染器接口
/// 负责处理模板变量替换和内容渲染
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// 渲染模板
    /// </summary>
    /// <param name="templateStream">模板流</param>
    /// <param name="variables">模板变量</param>
    /// <returns>渲染后的内容</returns>
    Task<string> RenderAsync(Stream templateStream, Dictionary<string, string> variables);
    
    /// <summary>
    /// 渲染模板（字符串版本）
    /// </summary>
    /// <param name="templateContent">模板内容</param>
    /// <param name="variables">模板变量</param>
    /// <returns>渲染后的内容</returns>
    Task<string> RenderAsync(string templateContent, Dictionary<string, string> variables);
}

/// <summary>
/// 默认模板渲染器实现
/// 支持 {{variableName}} 格式的变量替换
/// </summary>
public class DefaultTemplateRenderer : ITemplateRenderer
{
    public async Task<string> RenderAsync(Stream templateStream, Dictionary<string, string> variables)
    {
        using var reader = new StreamReader(templateStream);
        var templateContent = await reader.ReadToEndAsync();
        return await RenderAsync(templateContent, variables);
    }
    
    public async Task<string> RenderAsync(string templateContent, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(templateContent))
            return string.Empty;
        
        var result = templateContent;
        
        // 处理标准变量替换 {{variableName}}
        if (variables != null)
        {
            foreach (var variable in variables)
            {
                var placeholder = $"{{{{{variable.Key}}}}}";
                result = result.Replace(placeholder, variable.Value);
            }
        }
        
        return result;
    }
}