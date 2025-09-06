using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BD.FeishuApproval.Abstractions.CodeGen;
using BD.FeishuApproval.Shared.Dtos.Definitions;

namespace BD.FeishuApproval.CodeGen;

/// <summary>
/// TypeScript代码生成器实现
/// </summary>
public class TypeScriptCodeGenerator : ITypeScriptCodeGenerator
{
    /// <inheritdoc />
    public async Task<string> GenerateTypeScriptInterfaceAsync(ApprovalDefinitionDetail approvalDetail, string interfaceName = null)
    {
        if (string.IsNullOrEmpty(approvalDetail.Form))
        {
            throw new ArgumentException("审批定义中缺少表单结构信息", nameof(approvalDetail));
        }

        interfaceName ??= SanitizeIdentifier(approvalDetail.ApprovalName ?? "ApprovalRequest");
        return await GenerateTypeScriptInterfaceFromFormJsonAsync(approvalDetail.Form, interfaceName);
    }

    /// <inheritdoc />
    public async Task<string> GenerateTypeScriptInterfaceFromFormJsonAsync(string formJson, string interfaceName)
    {
        var widgets = JsonSerializer.Deserialize<List<FormWidget>>(formJson);
        if (widgets == null || widgets.Count == 0)
        {
            throw new ArgumentException("无法解析表单结构或表单为空", nameof(formJson));
        }

        var code = new StringBuilder();
        
        // 添加文件头注释
        code.AppendLine("/**");
        code.AppendLine(" * 自动生成的TypeScript接口");
        code.AppendLine($" * 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        code.AppendLine(" * 请勿手动修改此文件");
        code.AppendLine(" */");
        code.AppendLine();

        // 生成接口定义
        code.AppendLine($"export interface {SanitizeIdentifier(interfaceName)} {{");

        foreach (var widget in widgets)
        {
            var fieldName = SanitizeIdentifier(ToCamelCase(widget.Name));
            var tsType = MapFeishuTypeToTypeScript(widget.Type);
            var optional = widget.Required ? "" : "?";

            // 添加字段注释
            code.AppendLine($"  /** {widget.Name} ({widget.Type}) */");
            
            code.AppendLine($"  {fieldName}{optional}: {tsType};");
            code.AppendLine();
        }

        code.AppendLine("}");
        code.AppendLine();

        // 生成验证函数
        code.AppendLine($"/**");
        code.AppendLine($" * 验证 {interfaceName} 对象");
        code.AppendLine($" */");
        code.AppendLine($"export function validate{interfaceName}(obj: any): obj is {interfaceName} {{");
        code.AppendLine("  if (typeof obj !== 'object' || obj === null) return false;");
        code.AppendLine();

        foreach (var widget in widgets.Where(w => w.Required))
        {
            var fieldName = SanitizeIdentifier(ToCamelCase(widget.Name));
            var validation = GenerateValidationCheck(fieldName, widget.Type);
            code.AppendLine($"  {validation}");
        }

        code.AppendLine();
        code.AppendLine("  return true;");
        code.AppendLine("}");
        code.AppendLine();

        // 生成创建函数
        code.AppendLine($"/**");
        code.AppendLine($" * 创建 {interfaceName} 对象");
        code.AppendLine($" */");
        code.AppendLine($"export function create{interfaceName}(): {interfaceName} {{");
        code.AppendLine("  return {");
        
        foreach (var widget in widgets)
        {
            var fieldName = SanitizeIdentifier(ToCamelCase(widget.Name));
            var defaultValue = GetDefaultValueForType(widget.Type);
            code.AppendLine($"    {fieldName}: {defaultValue},");
        }

        code.AppendLine("  };");
        code.AppendLine("}");

        return await Task.FromResult(code.ToString());
    }

    private string MapFeishuTypeToTypeScript(string feishuType)
    {
        return feishuType?.ToLower() switch
        {
            "input" => "string",
            "number" => "number",
            "textarea" => "string",
            "select" => "string",
            "multiselect" => "string[]",
            "date" => "string", // ISO date string
            "datetime" => "string", // ISO datetime string
            "time" => "string", // HH:mm format
            "attachment" => "string[]", // file URLs
            "image" => "string[]", // image URLs
            "department" => "string",
            "user" => "string", 
            "checkbox" => "boolean",
            "radio" => "string",
            "rich_text" => "string", // 富文本
            "file" => "string[]", // 文件
            "location" => "string", // 位置
            _ => "string" // Default to string for unknown types
        };
    }

    private string GenerateValidationCheck(string fieldName, string feishuType)
    {
        return feishuType?.ToLower() switch
        {
            "number" => $"if (typeof obj.{fieldName} !== 'number') return false;",
            "checkbox" => $"if (typeof obj.{fieldName} !== 'boolean') return false;",
            "multiselect" => $"if (!Array.isArray(obj.{fieldName})) return false;",
            "attachment" => $"if (!Array.isArray(obj.{fieldName})) return false;",
            "image" => $"if (!Array.isArray(obj.{fieldName})) return false;",
            "file" => $"if (!Array.isArray(obj.{fieldName})) return false;",
            _ => $"if (typeof obj.{fieldName} !== 'string') return false;"
        };
    }

    private string GetDefaultValueForType(string feishuType)
    {
        return feishuType?.ToLower() switch
        {
            "number" => "0",
            "checkbox" => "false",
            "multiselect" => "[]",
            "attachment" => "[]",
            "image" => "[]",
            "file" => "[]",
            _ => "\"\""
        };
    }

    private string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        // 移除特殊字符并用空格分隔
        var words = str.Replace("(", " ").Replace(")", " ").Replace("-", " ")
                      .Replace("_", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length == 0)
            return str;

        // 第一个单词小写，后续单词首字母大写
        var result = words[0].ToLower();
        for (int i = 1; i < words.Length; i++)
        {
            result += char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
        }

        return result;
    }

    private string SanitizeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return "Unknown";

        // 移除非法字符
        var sanitized = new StringBuilder();
        foreach (char c in identifier)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sanitized.Append(c);
            }
        }

        var result = sanitized.ToString();
        
        // 确保以字母开头
        if (result.Length > 0 && !char.IsLetter(result[0]))
        {
            result = "I" + result;
        }

        return string.IsNullOrEmpty(result) ? "Unknown" : result;
    }
}