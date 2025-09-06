using BD.FeishuApproval.Abstractions.CodeGen;
using BD.FeishuApproval.Shared.Dtos.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BD.FeishuApproval.CodeGen;

/// <summary>
/// C# 代码生成器实现
/// </summary>
public class CSharpCodeGenerator : ICSharpCodeGenerator
{
    /// <inheritdoc />
    public async Task<string> GenerateCSharpDtoAsync(ApprovalDefinitionDetail approvalDetail, string? className = null, string? @namespace = null)
    {
        if (string.IsNullOrEmpty(approvalDetail.Form))
        {
            throw new ArgumentException("审批定义中缺少表单结构信息", nameof(approvalDetail));
        }

        className ??= SanitizeIdentifier(approvalDetail.ApprovalName ?? "ApprovalRequest") + "Dto";
        return await GenerateCSharpDtoFromFormJsonAsync(approvalDetail.Form, className, approvalDetail.ApprovalCode, @namespace);
    }

    /// <inheritdoc />
    public async Task<string> GenerateCSharpDtoFromFormJsonAsync(string formJson, string className, string approvalCode, string? @namespace = null)
    {
        var widgets = JsonSerializer.Deserialize<List<FormWidget>>(formJson);
        if (widgets == null || widgets.Count == 0)
        {
            throw new ArgumentException("无法解析表单结构或表单为空", nameof(formJson));
        }

        var code = new StringBuilder();
        
        // 添加 using 语句
        code.AppendLine("using System;");
        code.AppendLine("using System.Text.Json.Serialization;");
        code.AppendLine("using BD.FeishuApproval.Shared.Abstractions;");
        code.AppendLine();

        // 添加命名空间
        if (!string.IsNullOrEmpty(@namespace))
        {
            code.AppendLine($"namespace {@namespace}");
            code.AppendLine("{");
        }

        // 添加类注释
        code.AppendLine($"{GetIndent(@namespace)}/// <summary>");
        code.AppendLine($"{GetIndent(@namespace)}/// {approvalCode} 审批请求实体");
        code.AppendLine($"{GetIndent(@namespace)}/// 此代码由飞书SDK自动生成，支持类型安全的审批操作");
        code.AppendLine($"{GetIndent(@namespace)}/// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        code.AppendLine($"{GetIndent(@namespace)}/// </summary>");
        
        // 添加特性和类声明
        code.AppendLine($"{GetIndent(@namespace)}[ApprovalCode(\"{approvalCode}\")]");
        code.AppendLine($"{GetIndent(@namespace)}public class {SanitizeIdentifier(className)} : FeishuApprovalRequestBase");
        code.AppendLine($"{GetIndent(@namespace)}{{");

        // 生成属性
        foreach (var widget in widgets)
        {
            var propertyName = SanitizeIdentifier(ToPascalCase(widget.Name));
            var propertyType = MapFeishuTypeToCSharp(widget.Type);
            var defaultValue = GetDefaultValueForCSharpType(propertyType);

            // 添加属性注释
            code.AppendLine($"{GetIndent(@namespace)}    /// <summary>");
            code.AppendLine($"{GetIndent(@namespace)}    /// {widget.Name} ({widget.Type})");
            if (widget.Required)
            {
                code.AppendLine($"{GetIndent(@namespace)}    /// 必填字段");
            }
            code.AppendLine($"{GetIndent(@namespace)}    /// </summary>");
            
            // 添加JSON序列化特性
            code.AppendLine($"{GetIndent(@namespace)}    [JsonPropertyName(\"{widget.Id}\")]");
            
            // 添加属性声明
            code.AppendLine($"{GetIndent(@namespace)}    public {propertyType} {propertyName} {{ get; set; }} = {defaultValue};");
            code.AppendLine();
        }

        code.AppendLine($"{GetIndent(@namespace)}}}");

        // 结束命名空间
        if (!string.IsNullOrEmpty(@namespace))
        {
            code.AppendLine("}");
        }

        return await Task.FromResult(code.ToString());
    }

    /// <inheritdoc />
    public async Task<string> GenerateApprovalHandlerAsync(ApprovalDefinitionDetail approvalDetail, string dtoClassName, string? handlerClassName = null, string? @namespace = null)
    {
        handlerClassName ??= SanitizeIdentifier(approvalDetail.ApprovalName ?? "Approval") + "Handler";
        
        var code = new StringBuilder();
        
        // 添加 using 语句
        code.AppendLine("using BD.FeishuApproval.Abstractions.Handlers;");
        code.AppendLine("using BD.FeishuApproval.Abstractions.Instances;");
        code.AppendLine("using BD.FeishuApproval.Handlers;");
        code.AppendLine("using Microsoft.Extensions.Logging;");
        code.AppendLine("using System;");
        code.AppendLine("using System.Text.Json;");
        code.AppendLine("using System.Threading.Tasks;");
        code.AppendLine();

        // 添加命名空间
        if (!string.IsNullOrEmpty(@namespace))
        {
            code.AppendLine($"namespace {@namespace}");
            code.AppendLine("{");
        }

        // 添加类注释
        code.AppendLine($"{GetIndent(@namespace)}/// <summary>");
        code.AppendLine($"{GetIndent(@namespace)}/// {approvalDetail.ApprovalName} 审批处理器");
        code.AppendLine($"{GetIndent(@namespace)}/// ");
        code.AppendLine($"{GetIndent(@namespace)}/// 正确的职责划分：");
        code.AppendLine($"{GetIndent(@namespace)}/// - Handler 负责回调处理 + 提供校验能力（给 ApprovalService 调用）");
        code.AppendLine($"{GetIndent(@namespace)}/// - ApprovalService 负责实际的创建审批操作");
        code.AppendLine($"{GetIndent(@namespace)}/// - 第三方开发者继承此基类，实现回调方法 + 可选的校验逻辑");
        code.AppendLine($"{GetIndent(@namespace)}/// ");
        code.AppendLine($"{GetIndent(@namespace)}/// 处理 {approvalDetail.ApprovalCode} 类型的审批流程");
        code.AppendLine($"{GetIndent(@namespace)}/// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        code.AppendLine($"{GetIndent(@namespace)}/// </summary>");
        
        // 添加类声明
        code.AppendLine($"{GetIndent(@namespace)}public class {SanitizeIdentifier(handlerClassName)} : ApprovalHandlerBase<{SanitizeIdentifier(dtoClassName)}>");
        code.AppendLine($"{GetIndent(@namespace)}{{");
        
        // 添加构造函数
        code.AppendLine($"{GetIndent(@namespace)}    public {SanitizeIdentifier(handlerClassName)}(");
        code.AppendLine($"{GetIndent(@namespace)}        IFeishuApprovalInstanceService instanceService,");
        code.AppendLine($"{GetIndent(@namespace)}        ILogger<{SanitizeIdentifier(handlerClassName)}> logger)");
        code.AppendLine($"{GetIndent(@namespace)}        : base(instanceService, logger)");
        code.AppendLine($"{GetIndent(@namespace)}    {{");
        code.AppendLine($"{GetIndent(@namespace)}    }}");
        code.AppendLine();

        // 生成必须实现的回调处理方法
        GenerateRequiredHandlerMethods(code, @namespace, dtoClassName);
        
        // 生成可选的校验和生命周期钩子
        GenerateOptionalHandlerMethods(code, @namespace, dtoClassName);

        code.AppendLine($"{GetIndent(@namespace)}}}");

        // 结束命名空间
        if (!string.IsNullOrEmpty(@namespace))
        {
            code.AppendLine("}");
        }

        return await Task.FromResult(code.ToString());
    }

    /// <inheritdoc />
    public async Task<(string DtoCode, string HandlerCode)> GenerateCompleteApprovalCodeAsync(ApprovalDefinitionDetail approvalDetail, string? dtoClassName = null, string? handlerClassName = null, string? @namespace = null)
    {
        // 自动生成类名
        dtoClassName ??= SanitizeIdentifier(approvalDetail.ApprovalName ?? "Approval") + "Request";
        handlerClassName ??= SanitizeIdentifier(approvalDetail.ApprovalName ?? "Approval") + "Handler";
        
        // 生成DTO代码
        var dtoCode = await GenerateCSharpDtoAsync(approvalDetail, dtoClassName, @namespace);
        
        // 生成Handler代码
        var handlerCode = await GenerateApprovalHandlerAsync(approvalDetail, dtoClassName, handlerClassName, @namespace);
        
        return (dtoCode, handlerCode);
    }

    #region Handler方法生成
    
    private void GenerateRequiredHandlerMethods(StringBuilder code, string? @namespace, string dtoClassName)
    {
        var indent = GetIndent(@namespace);
        
        code.AppendLine($"{indent}    #region ===== 必须实现的回调处理方法 =====");
        code.AppendLine();

        // HandleApprovalApprovedAsync
        code.AppendLine($"{indent}    /// <summary>");
        code.AppendLine($"{indent}    /// 审批通过后的处理逻辑");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 实现建议：");
        code.AppendLine($"{indent}    /// 1. 更新业务数据状态为\"已通过\"");
        code.AppendLine($"{indent}    /// 2. 发送通知给申请人和相关人员");
        code.AppendLine($"{indent}    /// 3. 调用其他系统API（如资源分配、权限开通等）");
        code.AppendLine($"{indent}    /// 4. 记录审计日志");
        code.AppendLine($"{indent}    /// 5. 触发后续业务流程");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 示例代码：");
        code.AppendLine($"{indent}    /// await _businessService.UpdateStatusAsync(context.Data, \"Approved\");");
        code.AppendLine($"{indent}    /// await _notificationService.SendApprovedNotificationAsync(context.Data);");
        code.AppendLine($"{indent}    /// await _auditService.LogAsync(\"ApprovalApproved\", context.Data);");
        code.AppendLine($"{indent}    /// </summary>");
        code.AppendLine($"{indent}    public override Task HandleApprovalApprovedAsync(ApprovalContext<{SanitizeIdentifier(dtoClassName)}> context)");
        code.AppendLine($"{indent}    {{");
        code.AppendLine($"{indent}        throw new NotImplementedException(\"请实现审批通过后的业务逻辑处理\");");
        code.AppendLine($"{indent}    }}");
        code.AppendLine();

        // HandleApprovalRejectedAsync
        code.AppendLine($"{indent}    /// <summary>");
        code.AppendLine($"{indent}    /// 审批拒绝后的处理逻辑");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 实现建议：");
        code.AppendLine($"{indent}    /// 1. 更新业务数据状态为\"已拒绝\"");
        code.AppendLine($"{indent}    /// 2. 记录拒绝原因（context.Callback.Comment）");
        code.AppendLine($"{indent}    /// 3. 回滚相关预处理操作");
        code.AppendLine($"{indent}    /// 4. 发送拒绝通知给申请人");
        code.AppendLine($"{indent}    /// 5. 清理临时分配的资源");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 示例代码：");
        code.AppendLine($"{indent}    /// await _businessService.UpdateStatusAsync(context.Data, \"Rejected\", context.Callback.Comment);");
        code.AppendLine($"{indent}    /// await _businessService.RollbackAsync(context.Data);");
        code.AppendLine($"{indent}    /// await _notificationService.SendRejectedNotificationAsync(context.Data, context.Callback.Comment);");
        code.AppendLine($"{indent}    /// </summary>");
        code.AppendLine($"{indent}    public override Task HandleApprovalRejectedAsync(ApprovalContext<{SanitizeIdentifier(dtoClassName)}> context)");
        code.AppendLine($"{indent}    {{");
        code.AppendLine($"{indent}        throw new NotImplementedException(\"请实现审批拒绝后的业务逻辑处理\");");
        code.AppendLine($"{indent}    }}");
        code.AppendLine();

        // HandleApprovalCancelledAsync
        code.AppendLine($"{indent}    /// <summary>");
        code.AppendLine($"{indent}    /// 审批撤回后的处理逻辑");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 实现建议：");
        code.AppendLine($"{indent}    /// 1. 更新业务数据状态为\"已撤回\"");
        code.AppendLine($"{indent}    /// 2. 清理临时数据和预分配的资源");
        code.AppendLine($"{indent}    /// 3. 通知申请人撤回成功");
        code.AppendLine($"{indent}    /// 4. 记录撤回操作的审计日志");
        code.AppendLine($"{indent}    /// 5. 如有必要，触发补偿机制");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 示例代码：");
        code.AppendLine($"{indent}    /// await _businessService.UpdateStatusAsync(context.Data, \"Cancelled\");");
        code.AppendLine($"{indent}    /// await _resourceService.ReleasePreAllocatedAsync(context.Data);");
        code.AppendLine($"{indent}    /// await _notificationService.SendCancelledNotificationAsync(context.Data);");
        code.AppendLine($"{indent}    /// </summary>");
        code.AppendLine($"{indent}    public override Task HandleApprovalCancelledAsync(ApprovalContext<{SanitizeIdentifier(dtoClassName)}> context)");
        code.AppendLine($"{indent}    {{");
        code.AppendLine($"{indent}        throw new NotImplementedException(\"请实现审批撤回后的业务逻辑处理\");");
        code.AppendLine($"{indent}    }}");
        code.AppendLine();

        // HandleUnknownStatusAsync
        code.AppendLine($"{indent}    /// <summary>");
        code.AppendLine($"{indent}    /// 审批状态未知时的处理逻辑");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 实现建议：");
        code.AppendLine($"{indent}    /// 1. 记录详细的告警日志，包含所有回调信息");
        code.AppendLine($"{indent}    /// 2. 发送告警通知给系统管理员");
        code.AppendLine($"{indent}    /// 3. 将事件记录到失败任务表，等待人工处理");
        code.AppendLine($"{indent}    /// 4. 避免执行不可逆的业务操作");
        code.AppendLine($"{indent}    /// 5. 考虑实现重试机制或状态查询");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 示例代码：");
        code.AppendLine($"{indent}    /// _logger.LogWarning(\"收到未知审批状态: {{Status}}, 实例: {{InstanceCode}}\", context.Callback.Type, context.Callback.InstanceCode);");
        code.AppendLine($"{indent}    /// await _alertService.SendUnknownStatusAlertAsync(context);");
        code.AppendLine($"{indent}    /// await _failedJobService.AddAsync(context, \"Unknown approval status received\");");
        code.AppendLine($"{indent}    /// </summary>");
        code.AppendLine($"{indent}    public override Task HandleUnknownStatusAsync(ApprovalContext<{SanitizeIdentifier(dtoClassName)}> context)");
        code.AppendLine($"{indent}    {{");
        code.AppendLine($"{indent}        throw new NotImplementedException(\"请实现未知状态的处理逻辑\");");
        code.AppendLine($"{indent}    }}");
        code.AppendLine();

        // HandleBusinessExceptionAsync
        code.AppendLine($"{indent}    /// <summary>");
        code.AppendLine($"{indent}    /// 业务异常处理逻辑");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 实现建议：");
        code.AppendLine($"{indent}    /// 1. 详细记录异常信息和上下文数据");
        code.AppendLine($"{indent}    /// 2. 发送异常告警给开发和运维团队");
        code.AppendLine($"{indent}    /// 3. 将失败的任务记录到补偿队列");
        code.AppendLine($"{indent}    /// 4. 考虑是否需要回滚已执行的操作");
        code.AppendLine($"{indent}    /// 5. 提供人工介入的补偿机制");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 示例代码：");
        code.AppendLine($"{indent}    /// _logger.LogError(exception, \"审批业务处理异常: {{Data}}\", JsonSerializer.Serialize(context.Data));");
        code.AppendLine($"{indent}    /// await _alertService.SendExceptionAlertAsync(context, exception);");
        code.AppendLine($"{indent}    /// await _failedJobService.AddAsync(context, exception);");
        code.AppendLine($"{indent}    /// </summary>");
        code.AppendLine($"{indent}    public override Task HandleBusinessExceptionAsync(ApprovalContext<{SanitizeIdentifier(dtoClassName)}> context, Exception exception)");
        code.AppendLine($"{indent}    {{");
        code.AppendLine($"{indent}        throw new NotImplementedException(\"请实现业务异常处理逻辑\");");
        code.AppendLine($"{indent}    }}");
        code.AppendLine();

        code.AppendLine($"{indent}    #endregion");
        code.AppendLine();
    }

    private void GenerateOptionalHandlerMethods(StringBuilder code, string? @namespace, string dtoClassName)
    {
        var indent = GetIndent(@namespace);
        
        code.AppendLine($"{indent}    #region ===== 可选的校验和生命周期钩子 =====");
        code.AppendLine();

        // ValidateApprovalRequestAsync
        code.AppendLine($"{indent}    /// <summary>");
        code.AppendLine($"{indent}    /// 审批请求验证逻辑（可选重写）");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 实现建议：");
        code.AppendLine($"{indent}    /// 1. 验证必填字段");
        code.AppendLine($"{indent}    /// 2. 验证数据格式和范围");
        code.AppendLine($"{indent}    /// 3. 执行业务规则校验");
        code.AppendLine($"{indent}    /// 4. 检查用户权限和状态");
        code.AppendLine($"{indent}    /// 5. 验证关联数据的有效性");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 示例代码：");
        code.AppendLine($"{indent}    /// if (string.IsNullOrEmpty(request.SomeField))");
        code.AppendLine($"{indent}    ///     throw new ArgumentException(\"字段不能为空\");");
        code.AppendLine($"{indent}    /// if (await _userService.IsBlacklistedAsync(request.UserId))");
        code.AppendLine($"{indent}    ///     throw new InvalidOperationException(\"用户已被列入黑名单\");");
        code.AppendLine($"{indent}    /// </summary>");
        code.AppendLine($"{indent}    protected override async Task ValidateApprovalRequestAsync({SanitizeIdentifier(dtoClassName)} request)");
        code.AppendLine($"{indent}    {{");
        code.AppendLine($"{indent}        // 在这里实现自定义的验证逻辑");
        code.AppendLine($"{indent}        // 如果验证失败，抛出相应的异常");
        code.AppendLine($"{indent}        await Task.CompletedTask;");
        code.AppendLine($"{indent}    }}");
        code.AppendLine();

        // PreProcessApprovalAsync
        code.AppendLine($"{indent}    /// <summary>");
        code.AppendLine($"{indent}    /// 审批创建前预处理逻辑（可选重写）");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 实现建议：");
        code.AppendLine($"{indent}    /// 1. 预分配必要的资源");
        code.AppendLine($"{indent}    /// 2. 通知相关人员审批开始");
        code.AppendLine($"{indent}    /// 3. 准备审批所需的补充数据");
        code.AppendLine($"{indent}    /// 4. 执行前置业务逻辑");
        code.AppendLine($"{indent}    /// 5. 更新关联数据状态");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 示例代码：");
        code.AppendLine($"{indent}    /// await _resourceService.PreAllocateAsync(request);");
        code.AppendLine($"{indent}    /// await _notificationService.NotifyApproversAsync(request);");
        code.AppendLine($"{indent}    /// request.PrepareAdditionalData();");
        code.AppendLine($"{indent}    /// </summary>");
        code.AppendLine($"{indent}    protected override async Task PreProcessApprovalAsync({SanitizeIdentifier(dtoClassName)} request)");
        code.AppendLine($"{indent}    {{");
        code.AppendLine($"{indent}        // 在这里实现创建审批前的预处理逻辑");
        code.AppendLine($"{indent}        await Task.CompletedTask;");
        code.AppendLine($"{indent}    }}");
        code.AppendLine();

        // PostProcessApprovalAsync
        code.AppendLine($"{indent}    /// <summary>");
        code.AppendLine($"{indent}    /// 审批创建成功后处理逻辑（可选重写）");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 实现建议：");
        code.AppendLine($"{indent}    /// 1. 更新业务数据状态为\"审批中\"");
        code.AppendLine($"{indent}    /// 2. 保存审批实例ID到业务数据");
        code.AppendLine($"{indent}    /// 3. 发送提交成功通知");
        code.AppendLine($"{indent}    /// 4. 记录创建操作的审计日志");
        code.AppendLine($"{indent}    /// 5. 触发相关的业务流程");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 示例代码：");
        code.AppendLine($"{indent}    /// await _businessService.UpdateStatusAsync(request, \"InApproval\", result.InstanceCode);");
        code.AppendLine($"{indent}    /// await _notificationService.SendSubmissionSuccessAsync(request, result);");
        code.AppendLine($"{indent}    /// await _auditService.LogCreationAsync(request, result);");
        code.AppendLine($"{indent}    /// </summary>");
        code.AppendLine($"{indent}    protected override async Task PostProcessApprovalAsync({SanitizeIdentifier(dtoClassName)} request, BD.FeishuApproval.Shared.Dtos.Instances.CreateInstanceResult result)");
        code.AppendLine($"{indent}    {{");
        code.AppendLine($"{indent}        // 在这里实现审批创建成功后的处理逻辑");
        code.AppendLine($"{indent}        await Task.CompletedTask;");
        code.AppendLine($"{indent}    }}");
        code.AppendLine();

        // HandleCreateFailureInternalAsync
        code.AppendLine($"{indent}    /// <summary>");
        code.AppendLine($"{indent}    /// 审批创建失败处理逻辑（可选重写）");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 实现建议：");
        code.AppendLine($"{indent}    /// 1. 回滚预处理阶段的操作");
        code.AppendLine($"{indent}    /// 2. 释放预分配的资源");
        code.AppendLine($"{indent}    /// 3. 发送创建失败通知");
        code.AppendLine($"{indent}    /// 4. 记录失败日志和异常信息");
        code.AppendLine($"{indent}    /// 5. 考虑是否需要重试机制");
        code.AppendLine($"{indent}    /// ");
        code.AppendLine($"{indent}    /// 示例代码：");
        code.AppendLine($"{indent}    /// await _resourceService.RollbackPreAllocationAsync(request);");
        code.AppendLine($"{indent}    /// await _notificationService.SendCreationFailedAsync(request, exception);");
        code.AppendLine($"{indent}    /// _logger.LogError(exception, \"审批创建失败: {{Request}}\", JsonSerializer.Serialize(request));");
        code.AppendLine($"{indent}    /// </summary>");
        code.AppendLine($"{indent}    protected override async Task HandleCreateFailureInternalAsync({SanitizeIdentifier(dtoClassName)} request, Exception exception)");
        code.AppendLine($"{indent}    {{");
        code.AppendLine($"{indent}        // 在这里实现审批创建失败时的处理逻辑");
        code.AppendLine($"{indent}        await Task.CompletedTask;");
        code.AppendLine($"{indent}    }}");
        code.AppendLine();

        code.AppendLine($"{indent}    #endregion");
    }

    #endregion
    
    #region 私有辅助方法

    private string MapFeishuTypeToCSharp(string feishuType)
    {
        return feishuType?.ToLower() switch
        {
            "input" => "string",
            "number" => "int",
            "textarea" => "string",
            "select" => "string",
            "multiselect" => "string[]",
            "date" => "string",
            "datetime" => "string",
            "time" => "string",
            "attachment" => "string[]",
            "image" => "string[]",
            "department" => "string",
            "user" => "string",
            "checkbox" => "bool",
            "radio" => "string",
            "rich_text" => "string",
            "file" => "string[]",
            "location" => "string",
            _ => "string"
        };
    }

    private string GetDefaultValueForCSharpType(string csharpType)
    {
        return csharpType switch
        {
            "int" => "0",
            "bool" => "false",
            "string[]" => "Array.Empty<string>()",
            "string" => "string.Empty",
            _ => "string.Empty"
        };
    }

    private string ToPascalCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var words = str.Replace("(", " ").Replace(")", " ").Replace("-", " ")
                      .Replace("_", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length == 0)
            return str;

        var result = new StringBuilder();
        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                result.Append(char.ToUpper(word[0]));
                result.Append(word.Substring(1).ToLower());
            }
        }

        return result.ToString();
    }

    private string SanitizeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return "Unknown";

        var sanitized = new StringBuilder();
        foreach (char c in identifier)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sanitized.Append(c);
            }
        }

        var result = sanitized.ToString();
        
        if (result.Length > 0 && !char.IsLetter(result[0]))
        {
            result = "C" + result;
        }

        return string.IsNullOrEmpty(result) ? "Unknown" : result;
    }

    private string GetIndent(string? @namespace)
    {
        return string.IsNullOrEmpty(@namespace) ? "" : "    ";
    }

    #endregion
}