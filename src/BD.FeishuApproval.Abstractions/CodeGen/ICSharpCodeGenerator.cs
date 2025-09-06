using BD.FeishuApproval.Shared.Dtos.Definitions;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.CodeGen;

/// <summary>
/// C# 代码生成器接口
/// </summary>
public interface ICSharpCodeGenerator
{
    /// <summary>
    /// 从审批定义生成 C# DTO 类代码
    /// </summary>
    /// <param name="approvalDetail">审批定义详情</param>
    /// <param name="className">类名（可选，默认使用审批名称）</param>
    /// <param name="namespace">命名空间（可选）</param>
    /// <returns>生成的C#代码</returns>
    Task<string> GenerateCSharpDtoAsync(ApprovalDefinitionDetail approvalDetail, string? className = null, string? @namespace = null);

    /// <summary>
    /// 从表单JSON生成 C# DTO 类代码
    /// </summary>
    /// <param name="formJson">表单JSON字符串</param>
    /// <param name="className">类名</param>
    /// <param name="approvalCode">审批代码</param>
    /// <param name="namespace">命名空间（可选）</param>
    /// <returns>生成的C#代码</returns>
    Task<string> GenerateCSharpDtoFromFormJsonAsync(string formJson, string className, string approvalCode, string? @namespace = null);

    /// <summary>
    /// 生成审批处理器模板代码
    /// </summary>
    /// <param name="approvalDetail">审批定义详情</param>
    /// <param name="dtoClassName">DTO类名</param>
    /// <param name="handlerClassName">处理器类名（可选）</param>
    /// <param name="namespace">命名空间（可选）</param>
    /// <returns>生成的处理器代码</returns>
    Task<string> GenerateApprovalHandlerAsync(ApprovalDefinitionDetail approvalDetail, string dtoClassName, string? handlerClassName = null, string? @namespace = null);

    /// <summary>
    /// 生成完整的审批代码包（DTO + Handler）
    /// </summary>
    /// <param name="approvalDetail">审批定义详情</param>
    /// <param name="dtoClassName">DTO类名（可选）</param>
    /// <param name="handlerClassName">处理器类名（可选）</param>
    /// <param name="namespace">命名空间（可选）</param>
    /// <returns>包含DTO和Handler的完整代码</returns>
    Task<(string DtoCode, string HandlerCode)> GenerateCompleteApprovalCodeAsync(ApprovalDefinitionDetail approvalDetail, string? dtoClassName = null, string? handlerClassName = null, string? @namespace = null);
}