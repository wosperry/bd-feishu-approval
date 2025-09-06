using System.Threading.Tasks;
using BD.FeishuApproval.Shared.Dtos.Definitions;

namespace BD.FeishuApproval.Abstractions.CodeGen;

/// <summary>
/// TypeScript代码生成器接口
/// </summary>
public interface ITypeScriptCodeGenerator
{
    /// <summary>
    /// 根据审批定义生成TypeScript接口代码
    /// </summary>
    /// <param name="approvalDetail">审批定义详情</param>
    /// <param name="interfaceName">接口名称（可选）</param>
    /// <returns>生成的TypeScript代码</returns>
    Task<string> GenerateTypeScriptInterfaceAsync(ApprovalDefinitionDetail approvalDetail, string interfaceName = null);

    /// <summary>
    /// 根据表单JSON生成TypeScript接口代码
    /// </summary>
    /// <param name="formJson">表单JSON字符串</param>
    /// <param name="interfaceName">接口名称</param>
    /// <returns>生成的TypeScript代码</returns>
    Task<string> GenerateTypeScriptInterfaceFromFormJsonAsync(string formJson, string interfaceName);
}