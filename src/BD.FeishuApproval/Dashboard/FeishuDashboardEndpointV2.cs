#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BD.FeishuApproval.Abstractions.Persistence;
using BD.FeishuApproval.Shared.Models;
using BD.FeishuApproval.Dashboard.Templates;
using BD.FeishuApproval.Abstractions.Definitions;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Abstractions.CodeGen;

namespace BD.FeishuApproval.Dashboard;

/// <summary>
/// 新版飞书Dashboard端点处理器 (基于模板系统)
/// 支持灵活的HTML模板和第三方自定义
/// </summary>
public static class FeishuDashboardEndpointV2
{
    /// <summary>
    /// 映射飞书Dashboard端点
    /// </summary>
    /// <param name="endpoints">端点构建器</param>
    /// <param name="options">Dashboard选项</param>
    /// <returns>端点构建器</returns>
    public static IEndpointRouteBuilder MapFeishuDashboardV2(
        this IEndpointRouteBuilder endpoints,
        FeishuDashboardOptions options = null)
    {
        options ??= new FeishuDashboardOptions();
        
        // 主控制面板
        endpoints.MapGet(options.PathPrefix, async context =>
        {
            await ServeTemplateAsync(context, "dashboard.html", options);
        });

        // 管理页面
        endpoints.MapGet(options.ManagePath, async context =>
        {
            await ServeTemplateAsync(context, "manage.html", options);
        });

        // API端点保持不变，继续使用现有逻辑
        MapApiEndpoints(endpoints, options);
        
        return endpoints;
    }

    private static async Task ServeTemplateAsync(HttpContext context, string templateName, FeishuDashboardOptions options)
    {
        try
        {
            var templateService = context.RequestServices.GetRequiredService<IFeishuDashboardTemplateService>();
            var logger = context.RequestServices.GetService<ILogger>();
            
            // 检查模板是否存在
            if (!await templateService.TemplateExistsAsync(templateName))
            {
                logger.LogWarning("Template not found: {TemplateName}", templateName);
                await TrySetResponseAsync(context, 404, $"Template not found: {templateName}");
                return;
            }

            // 准备模板变量
            var variables = new Dictionary<string, string>
            {
                ["ApiPrefix"] = options.ApiPrefix,
                ["PathPrefix"] = options.PathPrefix,
                ["ManagePath"] = options.ManagePath,
                ["Title"] = GetPageTitle(templateName),
                ["Timestamp"] = DateTime.UtcNow.ToString("O")
            };

            // 渲染模板
            var html = await templateService.RenderTemplateAsync(templateName, variables);

            // 返回HTML
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(html);
            
            logger.LogDebug("Served template: {TemplateName}", templateName);
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetService<ILogger>();
            logger?.LogError(ex, "Failed to serve template: {TemplateName}", templateName);
            
            await TrySetResponseAsync(context, 500, "Internal server error");
        }
    }
    
    private static string GetPageTitle(string templateName)
    {
        return templateName switch
        {
            "dashboard.html" => "飞书 SDK - 控制面板",
            "manage.html" => "飞书管理页 - 系统配置",
            _ => "飞书 SDK"
        };
    }

    private static void MapApiEndpoints(IEndpointRouteBuilder endpoints, FeishuDashboardOptions options)
    {
        // 保持所有现有的API端点不变
        // 配置状态查询
        endpoints.MapGet($"{options.ApiPrefix}/config/status", async context =>
        {
            try
            {
                var repo = context.RequestServices.GetRequiredService<IFeishuApprovalRepository>();
                var cfg = await repo.GetAccessConfigAsync();
                var shaped = new { configured = !string.IsNullOrEmpty(cfg.AppId) && !string.IsNullOrEmpty(cfg.AppSecret) };
                
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(shaped));
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to get config status");
                await TrySetResponseAsync(context, 500, "Internal server error");
            }
        });

        // 管理员密码状态查询
        endpoints.MapGet($"{options.ApiPrefix}/admin/password-status", async context =>
        {
            try
            {
                var repo = context.RequestServices.GetRequiredService<IFeishuApprovalRepository>();
                // 通过尝试验证一个假密码来检查是否存在密码
                var hasPassword = !await repo.VerifyAdminPasswordAsync("__temp_check_password__");
                var result = new { hasPassword = hasPassword, isInitialized = hasPassword };
                
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to check admin password status");
                await TrySetResponseAsync(context, 500, "Internal server error");
            }
        });

        // 设置管理员密码
        endpoints.MapPost($"{options.ApiPrefix}/admin/password", async context =>
        {
            try
            {
                var repo = context.RequestServices.GetRequiredService<IFeishuApprovalRepository>();
                var request = await JsonSerializer.DeserializeAsync<SetPasswordRequestDto>(context.Request.Body);
                
                if (string.IsNullOrWhiteSpace(request?.Password))
                {
                    await TrySetResponseAsync(context, 400, "密码不能为空");
                    return;
                }

                if (request.Password.Length < 6)
                {
                    await TrySetResponseAsync(context, 400, "密码长度至少6位");
                    return;
                }

                await repo.SetAdminPasswordAsync(request.Password);
                
                var result = new { success = true, message = "密码设置成功" };
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to set admin password");
                await TrySetResponseAsync(context, 500, "Internal server error");
            }
        });

        // 验证管理员密码
        endpoints.MapPost($"{options.ApiPrefix}/admin/password/verify", async context =>
        {
            try
            {
                var repo = context.RequestServices.GetRequiredService<IFeishuApprovalRepository>();
                var request = await JsonSerializer.DeserializeAsync<SetPasswordRequestDto>(context.Request.Body);
                
                if (string.IsNullOrWhiteSpace(request?.Password))
                {
                    await TrySetResponseAsync(context, 400, "密码不能为空");
                    return;
                }

                var isValid = await repo.VerifyAdminPasswordAsync(request.Password);
                
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(isValid));
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to verify admin password");
                await TrySetResponseAsync(context, 500, "Internal server error");
            }
        });

        // 保存飞书配置
        endpoints.MapPost($"{options.ApiPrefix}/config", async context =>
        {
            var clientIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
            var repo = context.RequestServices.GetRequiredService<IFeishuApprovalRepository>();
            
            var pwd = context.Request.Headers["X-Feishu-Admin-Password"].FirstOrDefault();
            
            // 开发环境调试日志
            Console.WriteLine($"[DEBUG] 配置保存 - 收到密码头: {!string.IsNullOrEmpty(pwd)}");
            Console.WriteLine($"[DEBUG] 配置保存 - 密码长度: {pwd?.Length ?? 0}");
            Console.WriteLine($"[DEBUG] 配置保存 - 密码前5字符: {(string.IsNullOrEmpty(pwd) ? "null" : pwd.Substring(0, Math.Min(5, pwd.Length)))}");
            
            if (string.IsNullOrEmpty(pwd))
            {
                Console.WriteLine("[DEBUG] 配置保存 - 密码为空");
                await repo.SaveManageLogAsync(new FeishuManageLog
                {
                    Operation = "ConfigSave",
                    Description = "保存飞书应用配置",
                    Parameters = "认证失败-密码为空",
                    Result = "Failed",
                    ErrorMessage = "管理口令为空",
                    ClientIP = clientIP,
                    UserAgent = userAgent
                });
                
                await TrySetResponseAsync(context, 401, "Unauthorized");
                return;
            }
            
            var isValidPassword = await repo.VerifyAdminPasswordAsync(pwd);
            Console.WriteLine($"[DEBUG] 配置保存 - 密码验证结果: {isValidPassword}");
            
            if (!isValidPassword)
            {
                Console.WriteLine("[DEBUG] 配置保存 - 密码验证失败");
                await repo.SaveManageLogAsync(new FeishuManageLog
                {
                    Operation = "ConfigSave",
                    Description = "保存飞书应用配置",
                    Parameters = "认证失败-密码错误",
                    Result = "Failed",
                    ErrorMessage = "管理口令验证失败",
                    ClientIP = clientIP,
                    UserAgent = userAgent
                });
                
                await TrySetResponseAsync(context, 401, "Unauthorized");
                return;
            }
            
            try
            {
                using var doc = await JsonDocument.ParseAsync(context.Request.Body);
                var root = doc.RootElement;
                var cfg = new FeishuAccessConfig
                {
                    AppId = root.GetProperty("appId").GetString(),
                    AppSecret = root.GetProperty("appSecret").GetString(),
                    EncryptKey = root.TryGetProperty("encryptKey", out var ek) ? ek.GetString() : string.Empty,
                    VerificationToken = root.TryGetProperty("verificationToken", out var vt) ? vt.GetString() : string.Empty
                };

                await repo.UpsertAccessConfigAsync(cfg);
                
                await repo.SaveManageLogAsync(new FeishuManageLog
                {
                    Operation = "ConfigSave",
                    Description = "保存飞书应用配置",
                    Parameters = JsonSerializer.Serialize(new { appId = cfg.AppId, hasSecret = !string.IsNullOrEmpty(cfg.AppSecret) }),
                    Result = "Success",
                    ErrorMessage = string.Empty,
                    ClientIP = clientIP,
                    UserAgent = userAgent
                });

                await context.Response.WriteAsync("OK");
            }
            catch (Exception ex)
            {
                await repo.SaveManageLogAsync(new FeishuManageLog
                {
                    Operation = "ConfigSave",
                    Description = "保存飞书应用配置",
                    Parameters = "异常",
                    Result = "Failed",
                    ErrorMessage = ex.Message,
                    ClientIP = clientIP,
                    UserAgent = userAgent
                });
                
                await TrySetResponseAsync(context, 500, "保存失败: " + ex.Message);
            }
        });

        // 登记审批流程
        endpoints.MapPost($"{options.ApiPrefix}/approvals", async context =>
        {
            var clientIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
            var repo = context.RequestServices.GetRequiredService<IFeishuApprovalRepository>();
            
            var pwd = context.Request.Headers["X-Feishu-Admin-Password"].FirstOrDefault();
            
            // 开发环境调试日志
            Console.WriteLine($"[DEBUG] 审批登记 - 收到密码头: {!string.IsNullOrEmpty(pwd)}");
            Console.WriteLine($"[DEBUG] 审批登记 - 密码长度: {pwd?.Length ?? 0}");
            
            if (string.IsNullOrEmpty(pwd))
            {
                Console.WriteLine("[DEBUG] 审批登记 - 密码为空");
                await TrySetResponseAsync(context, 401, "Unauthorized");
                return;
            }
            
            var isValidPassword = await repo.VerifyAdminPasswordAsync(pwd);
            Console.WriteLine($"[DEBUG] 审批登记 - 密码验证结果: {isValidPassword}");
            
            if (!isValidPassword)
            {
                Console.WriteLine("[DEBUG] 审批登记 - 密码验证失败");
                await TrySetResponseAsync(context, 401, "Unauthorized");
                return;
            }
            
            try
            {
                using var doc = await JsonDocument.ParseAsync(context.Request.Body);
                var root = doc.RootElement;
                
                var reg = new FeishuApprovalRegistration
                {
                    ApprovalCode = root.GetProperty("approvalCode").GetString(),
                    DisplayName = root.GetProperty("displayName").GetString(),
                    Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : string.Empty
                };

                var id = await repo.RegisterApprovalAsync(reg);
                
                await repo.SaveManageLogAsync(new FeishuManageLog
                {
                    Operation = "RegisterApproval",
                    Description = "登记审批流程",
                    Parameters = JsonSerializer.Serialize(reg),
                    Result = "Success",
                    ClientIP = clientIP,
                    UserAgent = userAgent
                });

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = true, data = id }));
            }
            catch (Exception ex)
            {
                await repo.SaveManageLogAsync(new FeishuManageLog
                {
                    Operation = "RegisterApproval",
                    Description = "登记审批流程",
                    Result = "Failed",
                    ErrorMessage = ex.Message,
                    ClientIP = clientIP,
                    UserAgent = userAgent
                });
                
                await TrySetResponseAsync(context, 500, "登记失败: " + ex.Message);
            }
        });

        // 查询已登记的审批流程
        endpoints.MapGet($"{options.ApiPrefix}/approvals", async context =>
        {
            try
            {
                var repo = context.RequestServices.GetRequiredService<IFeishuApprovalRepository>();
                var approvals = await repo.ListApprovalsAsync();
                
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(approvals));
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to list approvals");
                await TrySetResponseAsync(context, 500, "查询失败: " + ex.Message);
            }
        });

        // 代码生成
        endpoints.MapGet($"{options.ApiPrefix}/codegen/{{code}}", async context =>
        {
            var code = context.Request.RouteValues["code"]?.ToString();
            var className = context.Request.Query["className"].FirstOrDefault();
            Console.WriteLine($"[DEBUG] 代码生成 - 审批代码: {code}, 类名: {className}");
            
            if (string.IsNullOrWhiteSpace(code))
            {
                Console.WriteLine("[DEBUG] 代码生成 - 审批代码为空");
                await TrySetResponseAsync(context, 400, "审批代码不能为空");
                return;
            }

            try
            {
                Console.WriteLine($"[DEBUG] 代码生成 - 开始获取审批定义: {code}");
                var defSvc = context.RequestServices.GetRequiredService<IFeishuApprovalDefinitionService>();
                var def = await defSvc.GetDefinitionDetailAsync(code);
                
                Console.WriteLine($"[DEBUG] 代码生成 - 获取定义结果: Success={def.IsSuccess}, Message={def.Message}");
                
                if (!def.IsSuccess)
                {
                    Console.WriteLine($"[DEBUG] 代码生成 - 获取定义失败: {def.Message}");
                    await TrySetResponseAsync(context, 404, $"获取审批定义失败: {def.Message}");
                    return;
                }

                var generatedCode = GenerateEntityCode(def.Data, code, className);
                
                var repo = context.RequestServices.GetRequiredService<IFeishuApprovalRepository>();
                await repo.SaveManageLogAsync(new FeishuManageLog
                {
                    Operation = "CodeGenerate",
                    Description = $"为审批 {code} 生成代码",
                    Parameters = JsonSerializer.Serialize(new { code }),
                    Result = "Success",
                    ClientIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown"
                });

                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(generatedCode);
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to generate code for approval: {Code}", code);
                await TrySetResponseAsync(context, 500, "代码生成失败: " + ex.Message);
            }
        });

        // C# DTO类代码生成
        endpoints.MapGet($"{options.ApiPrefix}/codegen/csharp/{{code}}", async context =>
        {
            var code = context.Request.RouteValues["code"]?.ToString();
            var className = context.Request.Query["className"].FirstOrDefault();
            var namespaceName = context.Request.Query["namespace"].FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(code))
            {
                await TrySetResponseAsync(context, 400, "审批代码不能为空");
                return;
            }

            try
            {
                var defSvc = context.RequestServices.GetRequiredService<IFeishuApprovalDefinitionService>();
                var csharpGenerator = context.RequestServices.GetRequiredService<ICSharpCodeGenerator>();
                
                var def = await defSvc.GetDefinitionDetailAsync(code);
                if (!def.IsSuccess)
                {
                    await TrySetResponseAsync(context, 404, $"获取审批定义失败: {def.Message}");
                    return;
                }

                // 生成完整的代码包（DTO + Handler）
                var (dtoCode, handlerCode) = await csharpGenerator.GenerateCompleteApprovalCodeAsync(def.Data, className, null, namespaceName);
                
                // 组合代码，用分隔符区分
                var combinedCode = $@"// ===========================================
// {def.Data.ApprovalName} - 审批请求 DTO
// ===========================================

{dtoCode}

// ===========================================
// {def.Data.ApprovalName} - 审批处理器
// ===========================================

{handlerCode}";
                
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(combinedCode);
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to generate C# DTO code for approval: {Code}", code);
                await TrySetResponseAsync(context, 500, "C#代码生成失败: " + ex.Message);
            }
        });

        // C# 处理器类代码生成
        endpoints.MapGet($"{options.ApiPrefix}/codegen/handler/{{code}}", async context =>
        {
            var code = context.Request.RouteValues["code"]?.ToString();
            var dtoClassName = context.Request.Query["dtoClassName"].FirstOrDefault();
            var handlerClassName = context.Request.Query["handlerClassName"].FirstOrDefault();
            var namespaceName = context.Request.Query["namespace"].FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(code))
            {
                await TrySetResponseAsync(context, 400, "审批代码不能为空");
                return;
            }

            try
            {
                var defSvc = context.RequestServices.GetRequiredService<IFeishuApprovalDefinitionService>();
                var csharpGenerator = context.RequestServices.GetRequiredService<ICSharpCodeGenerator>();
                
                var def = await defSvc.GetDefinitionDetailAsync(code);
                if (!def.IsSuccess)
                {
                    await TrySetResponseAsync(context, 404, $"获取审批定义失败: {def.Message}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(dtoClassName))
                {
                    dtoClassName = SanitizeIdentifier(def.Data.ApprovalName ?? "Approval") + "Dto";
                }

                var generatedCode = await csharpGenerator.GenerateApprovalHandlerAsync(def.Data, dtoClassName, handlerClassName, namespaceName);
                
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(generatedCode);
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to generate handler code for approval: {Code}", code);
                await TrySetResponseAsync(context, 500, "处理器代码生成失败: " + ex.Message);
            }
        });

        // 订阅审批
        endpoints.MapPost($"{options.ApiPrefix}/approvals/{{code}}/subscribe", async context =>
        {
            var code = context.Request.RouteValues["code"]?.ToString();
            var pwd = context.Request.Headers["X-Feishu-Admin-Password"].FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(code))
            {
                await TrySetResponseAsync(context, 400, "审批代码不能为空");
                return;
            }

            if (string.IsNullOrEmpty(pwd))
            {
                await TrySetResponseAsync(context, 401, "Unauthorized");
                return;
            }

            try
            {
                var repo = context.RequestServices.GetRequiredService<IFeishuApprovalRepository>();
                var isValidPassword = await repo.VerifyAdminPasswordAsync(pwd);
                if (!isValidPassword)
                {
                    await TrySetResponseAsync(context, 401, "Unauthorized");
                    return;
                }

                var defSvc = context.RequestServices.GetRequiredService<IFeishuApprovalDefinitionService>();
                var result = await defSvc.SubscribeApprovalAsync(code);
                
                await repo.SaveManageLogAsync(new FeishuManageLog
                {
                    Operation = "SubscribeApproval",
                    Description = $"订阅审批回调: {code}",
                    Parameters = JsonSerializer.Serialize(new { approvalCode = code }),
                    Result = result.IsSuccess ? "Success" : "Failed",
                    ErrorMessage = result.IsSuccess ? string.Empty : result.Message,
                    ClientIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown"
                });

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new 
                { 
                    success = result.IsSuccess, 
                    message = result.IsSuccess ? "订阅成功" : result.Message 
                }));
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to subscribe approval: {Code}", code);
                await TrySetResponseAsync(context, 500, "订阅失败: " + ex.Message);
            }
        });

        // 取消订阅审批
        endpoints.MapPost($"{options.ApiPrefix}/approvals/{{code}}/unsubscribe", async context =>
        {
            var code = context.Request.RouteValues["code"]?.ToString();
            var pwd = context.Request.Headers["X-Feishu-Admin-Password"].FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(code))
            {
                await TrySetResponseAsync(context, 400, "审批代码不能为空");
                return;
            }

            if (string.IsNullOrEmpty(pwd))
            {
                await TrySetResponseAsync(context, 401, "Unauthorized");
                return;
            }

            try
            {
                var repo = context.RequestServices.GetRequiredService<IFeishuApprovalRepository>();
                var isValidPassword = await repo.VerifyAdminPasswordAsync(pwd);
                if (!isValidPassword)
                {
                    await TrySetResponseAsync(context, 401, "Unauthorized");
                    return;
                }

                var defSvc = context.RequestServices.GetRequiredService<IFeishuApprovalDefinitionService>();
                var result = await defSvc.UnsubscribeApprovalAsync(code);
                
                await repo.SaveManageLogAsync(new FeishuManageLog
                {
                    Operation = "UnsubscribeApproval", 
                    Description = $"取消订阅审批回调: {code}",
                    Parameters = JsonSerializer.Serialize(new { approvalCode = code }),
                    Result = result.IsSuccess ? "Success" : "Failed",
                    ErrorMessage = result.IsSuccess ? string.Empty : result.Message,
                    ClientIP = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown"
                });

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new 
                { 
                    success = result.IsSuccess, 
                    message = result.IsSuccess ? "取消订阅成功" : result.Message 
                }));
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to unsubscribe approval: {Code}", code);
                await TrySetResponseAsync(context, 500, "取消订阅失败: " + ex.Message);
            }
        });

        // 查询审批订阅状态
        endpoints.MapGet($"{options.ApiPrefix}/approvals/{{code}}/status", async context =>
        {
            var code = context.Request.RouteValues["code"]?.ToString();
            
            if (string.IsNullOrWhiteSpace(code))
            {
                await TrySetResponseAsync(context, 400, "审批代码不能为空");
                return;
            }

            try
            {
                var defSvc = context.RequestServices.GetRequiredService<IFeishuApprovalDefinitionService>();
                var result = await defSvc.GetApprovalSubscriptionStatusAsync(code);
                
                if (!result.IsSuccess)
                {
                    await TrySetResponseAsync(context, 404, $"获取审批状态失败: {result.Message}");
                    return;
                }

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new 
                { 
                    success = true,
                    data = result.Data 
                }));
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger>();
                logger?.LogError(ex, "Failed to get approval status: {Code}", code);
                await TrySetResponseAsync(context, 500, "查询状态失败: " + ex.Message);
            }
        });
    }

    private static string SanitizeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return "Unknown";

        var sanitized = new System.Text.StringBuilder();
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

    private static string GenerateEntityCode(object definitionData, string approvalCode, string customClassName = null)
    {
        // 确定类名：用户指定的类名 > Demo类名
        var className = !string.IsNullOrWhiteSpace(customClassName) 
            ? customClassName.Trim()
            : $"{ToPascalCase(approvalCode)}Request";
        
        // 如果没有自定义类名且审批代码是UUID格式，使用Demo类名
        if (string.IsNullOrWhiteSpace(customClassName) && IsUuidLike(approvalCode))
        {
            className = "DemoApprovalRequest";
        }

        // 解析表单字段
        var properties = GeneratePropertiesFromDefinition(definitionData);
        
        return $@"using System;
using System.Text.Json.Serialization;
using BD.FeishuApproval.Shared.Abstractions;
using BD.FeishuApproval.Extensions;

namespace YourProject.Models
{{
    /// <summary>
    /// {approvalCode} 审批请求实体
    /// 此代码由飞书SDK自动生成，支持类型安全的审批操作
    /// </summary>
    [ApprovalCode(""{approvalCode}"")]
    public class {className} : FeishuApprovalRequestBase
    {{
        // 根据审批定义生成的属性
{properties}
    }}
}}

// 使用示例 - 类型安全的方式（推荐）:
// var request = new {className}
// {{
//     // 设置对应字段的值
// }};
// 
// // 使用类型安全的扩展方法 - 审批代码自动从特性获取，防止传错类型
// var result = await _instanceService.CreateTypedInstanceAsync(request);

// 传统方式（不推荐，容易出错）:
// var result = await _instanceService.CreateInstanceAsync(new CreateInstanceRequest
// {{
//     ApprovalCode = ""{approvalCode}"", // 容易与实际代码不一致
//     FormData = JsonSerializer.Serialize(request)
// }});";
    }

    private static string GeneratePropertiesFromDefinition(object definitionData)
    {
        try
        {
            if (definitionData is not BD.FeishuApproval.Shared.Dtos.Definitions.ApprovalDefinitionDetail definition)
            {
                return GenerateFallbackProperties();
            }

            if (string.IsNullOrEmpty(definition.Form))
            {
                return GenerateFallbackProperties();
            }

            var widgets = JsonSerializer.Deserialize<List<BD.FeishuApproval.Shared.Dtos.Definitions.FormWidget>>(definition.Form);
            if (widgets == null || widgets.Count == 0)
            {
                return GenerateFallbackProperties();
            }

            var properties = new List<string>();
            foreach (var widget in widgets)
            {
                if (string.IsNullOrEmpty(widget.Name) || string.IsNullOrEmpty(widget.Type))
                    continue;

                var fieldName = ToCamelCase(widget.Name);
                var csharpType = MapFeishuTypeToCSharp(widget.Type);
                var optional = widget.Required ? "" : "?";
                var defaultValue = GetDefaultValueForCSharpType(widget.Type, widget.Required);

                // 添加属性注释和特性
                properties.Add($"        /// <summary>");
                properties.Add($"        /// {widget.Name} ({widget.Type})");
                properties.Add($"        /// </summary>");
                properties.Add($"        [JsonPropertyName(\"{widget.Id}\")]");
                properties.Add($"        public {csharpType}{optional} {ToPascalCase(fieldName)} {{ get; set; }}{defaultValue};");
                properties.Add("");
            }

            return string.Join(Environment.NewLine, properties);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] 解析表单字段失败: {ex.Message}");
            return GenerateFallbackProperties();
        }
    }

    private static string GenerateFallbackProperties()
    {
        return @"        // 无法解析表单字段，使用通用属性
        
        [JsonPropertyName(""title"")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName(""description"")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName(""created_at"")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;";
    }

    private static string MapFeishuTypeToCSharp(string feishuType)
    {
        return feishuType?.ToLower() switch
        {
            "input" => "string",
            "number" => "int",
            "textarea" => "string",
            "select" => "string",
            "multiselect" => "List<string>",
            "date" => "DateTime",
            "datetime" => "DateTime",
            "time" => "TimeSpan",
            "attachment" => "List<string>",
            "image" => "List<string>",
            "department" => "string",
            "user" => "string",
            "checkbox" => "bool",
            "radio" => "string",
            "rich_text" => "string",
            "file" => "List<string>",
            "location" => "string",
            _ => "string"
        };
    }

    private static string GetDefaultValueForCSharpType(string feishuType, bool isRequired)
    {
        if (!isRequired) return "";

        return feishuType?.ToLower() switch
        {
            "number" => " = 0",
            "checkbox" => " = false",
            "multiselect" => " = new List<string>()",
            "attachment" => " = new List<string>()",
            "image" => " = new List<string>()",
            "file" => " = new List<string>()",
            "date" => " = DateTime.Now",
            "datetime" => " = DateTime.Now",
            "time" => " = TimeSpan.Zero",
            _ => " = string.Empty"
        };
    }

    /// <summary>
    /// 安全地设置HTTP响应状态码和内容
    /// </summary>
    private static async Task TrySetResponseAsync(HttpContext context, int statusCode, string message)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(message);
        }
    }

    private static string ToCamelCase(string str)
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

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        var parts = input.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join("", parts.Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLower()));
    }
    
    private static bool IsUuidLike(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        
        // 检查是否包含大量数字或连续的字符组合，通常UUID或ID格式
        return input.Length > 20 && (input.Count(char.IsDigit) > input.Length * 0.3 || 
               input.Any(c => char.IsLower(c) && char.IsUpper(input.FirstOrDefault())));
    }
}

/// <summary>
/// 设置密码请求DTO
/// </summary>
public class SetPasswordRequestDto
{
    public string Password { get; set; }
}

#endif