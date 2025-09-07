using BD.FeishuApproval.Abstractions.CodeGen;
using BD.FeishuApproval.Abstractions.Definitions;
using BD.FeishuApproval.Abstractions.Health;
using BD.FeishuApproval.Abstractions.Management;
using BD.FeishuApproval.Abstractions.Persistence;
using BD.FeishuApproval.Callbacks;
using BD.FeishuApproval.Health;
using BD.FeishuApproval.Shared.Dtos.Definitions;
using BD.FeishuApproval.Shared.Events;
using BD.FeishuApproval.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using static Dm.net.buffer.ByteArrayBuffer;

namespace BD.FeishuApproval.Management;

/// <summary>
/// 飞书管理服务实现
/// </summary>
public class FeishuManagementService : IFeishuManagementService
{
    private readonly IFeishuApprovalRepository _repository;
    private readonly IFeishuApprovalDefinitionService _definitionService;
    private readonly IFeishuApprovalSubscriptionService _subscriptionService;
    private readonly ITypeScriptCodeGenerator _typeScriptCodeGenerator;
    private readonly IFeishuHealthCheckService _healthService;
    private readonly IFeishuCallbackService _callbackService;
    private readonly ILogger<FeishuManagementService> _logger;

    public FeishuManagementService(
        IFeishuApprovalRepository repository,
        IFeishuApprovalDefinitionService definitionService,
        IFeishuApprovalSubscriptionService subscriptionService,
        ITypeScriptCodeGenerator typeScriptCodeGenerator,
        IFeishuHealthCheckService healthService,
        IFeishuCallbackService callbackService,
        ILogger<FeishuManagementService> logger)
    {
        _repository = repository;
        _definitionService = definitionService;
        _subscriptionService = subscriptionService;
        _typeScriptCodeGenerator = typeScriptCodeGenerator;
        _healthService = healthService;
        _callbackService = callbackService;
        _logger = logger;
    }

    public async Task<ConfigurationStatus> GetConfigurationStatusAsync(bool needPassword = true)
    {
        try
        {
            var config = await _repository.GetAccessConfigAsync();
            var hasPassword = !needPassword || await CheckAdminPasswordExistsAsync();

            var isConfigured = !string.IsNullOrEmpty(config?.AppId) && !string.IsNullOrEmpty(config?.AppSecret);
            var completion = CalculateCompletionPercentage(isConfigured, hasPassword);

            return new ConfigurationStatus
            {
                IsConfigured = isConfigured,
                HasAdminPassword = hasPassword,
                AppIdMasked = MaskAppId(config?.AppId),
                CompletionPercentage = completion,
                FeishuConfig = config // 新增：返回完整配置（内部使用）
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取配置状态失败");
            return new ConfigurationStatus
            {
                IsConfigured = false,
                HasAdminPassword = false,
                CompletionPercentage = 0
            };
        }
    }

    public async Task<ManagementOperationResult> SetAdminPasswordAsync(string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return ManagementOperationResult.Failure("密码不能为空");
            }

            if (password.Length < 6)
            {
                return ManagementOperationResult.Failure("密码长度至少6位");
            }

            await _repository.SetAdminPasswordAsync(password);

            await _repository.SaveManageLogAsync(new FeishuManageLog
            {
                Operation = "SetAdminPassword",
                Description = "设置管理员密码",
                Result = "Success",
                ClientIP = "API",
                UserAgent = "ManagementService"
            });

            return ManagementOperationResult.Success("管理员密码设置成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置管理员密码失败");
            return ManagementOperationResult.Failure("设置密码失败", ex.Message);
        }
    }

    public async Task<bool> VerifyAdminPasswordAsync(string password)
    {
        try
        {
            return await _repository.VerifyAdminPasswordAsync(password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证管理员密码失败");
            return false;
        }
    }

    public async Task<ManagementOperationResult> SaveFeishuConfigAsync(FeishuConfigRequest config, string adminPassword)
    {
        try
        {
            if (!await _repository.VerifyAdminPasswordAsync(adminPassword))
            {
                return ManagementOperationResult.Failure("管理员密码验证失败");
            }

            if (string.IsNullOrWhiteSpace(config.AppId) || string.IsNullOrWhiteSpace(config.AppSecret))
            {
                return ManagementOperationResult.Failure("AppId 和 AppSecret 不能为空");
            }

            var accessConfig = new FeishuAccessConfig
            {
                AppId = config.AppId,
                AppSecret = config.AppSecret,
                EncryptKey = config.EncryptKey ?? string.Empty,
                VerificationToken = config.VerificationToken ?? string.Empty,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _repository.UpsertAccessConfigAsync(accessConfig);

            await _repository.SaveManageLogAsync(new FeishuManageLog
            {
                Operation = "SaveConfig",
                Description = "保存飞书应用配置",
                Parameters = System.Text.Json.JsonSerializer.Serialize(new { appId = config.AppId, hasSecret = !string.IsNullOrEmpty(config.AppSecret) }),
                Result = "Success",
                ClientIP = "API",
                UserAgent = "ManagementService"
            });

            return ManagementOperationResult.Success("飞书配置保存成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存飞书配置失败");
            return ManagementOperationResult.Failure("保存配置失败", ex.Message);
        }
    }

    public async Task<ManagementOperationResult<int>> RegisterApprovalAsync(ApprovalRegistrationRequest registration, string adminPassword)
    {
        try
        {
            if (!await _repository.VerifyAdminPasswordAsync(adminPassword))
            {
                return ManagementOperationResult<int>.Failure("管理员密码验证失败");
            }

            if (string.IsNullOrWhiteSpace(registration.ApprovalCode))
            {
                return ManagementOperationResult<int>.Failure("审批代码不能为空");
            }

            var reg = new FeishuApprovalRegistration
            {
                ApprovalCode = registration.ApprovalCode,
                DisplayName = registration.DisplayName ?? registration.ApprovalCode,
                Description = registration.Description ?? string.Empty
            };

            var id = await _repository.RegisterApprovalAsync(reg);

            await _repository.SaveManageLogAsync(new FeishuManageLog
            {
                Operation = "RegisterApproval",
                Description = "登记审批流程",
                Parameters = System.Text.Json.JsonSerializer.Serialize(registration),
                Result = "Success",
                ClientIP = "API",
                UserAgent = "ManagementService"
            });

            return ManagementOperationResult<int>.Success(id, "审批登记成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登记审批流程失败");
            return ManagementOperationResult<int>.Failure("登记审批失败", ex.Message);
        }
    }

    public async Task<List<FeishuApprovalRegistration>> GetRegisteredApprovalsAsync()
    {
        try
        {
            var approvals = await _repository.ListApprovalsAsync();
            return approvals.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取已登记审批列表失败");
            return new List<FeishuApprovalRegistration>();
        }
    }

    public async Task<string> GenerateEntityCodeAsync(string approvalCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(approvalCode))
            {
                return "// 错误: 审批代码不能为空";
            }

            var result = await _definitionService.GetDefinitionDetailAsync(approvalCode);
            if (!result.IsSuccess)
            {
                return $"// 错误: 获取审批定义失败 - {result.Message}";
            }

            return GenerateEntityCode(result.Data, approvalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成实体代码失败，审批代码: {ApprovalCode}", approvalCode);
            return $"// 错误: 代码生成失败 - {ex.Message}";
        }
    }

    public async Task<ApprovalDefinitionDetail> GetApprovalDefinitionAsync(string approvalCode)
    {
        var result = await _definitionService.GetDefinitionDetailAsync(approvalCode);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"获取审批定义失败: {result.Message}");
        }
        return result.Data;
    }

    public async Task<PagedResult<FailedJob>> GetFailedJobsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var jobs = await _repository.QueryFailedJobsAsync(page, pageSize);
            var totalCount = await GetFailedJobsTotalCountAsync();

            return new PagedResult<FailedJob>
            {
                Items = jobs.ToList(),
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询失败任务失败");
            return new PagedResult<FailedJob>();
        }
    }

    public async Task<ManagementOperationResult> ResolveFailedJobAsync(int jobId)
    {
        try
        {
            await _repository.MarkFailedJobSucceededAsync(jobId);

            await _repository.SaveManageLogAsync(new FeishuManageLog
            {
                Operation = "ResolveFailedJob",
                Description = $"标记失败任务 {jobId} 为成功",
                Parameters = System.Text.Json.JsonSerializer.Serialize(new { jobId }),
                Result = "Success",
                ClientIP = "API",
                UserAgent = "ManagementService"
            });

            return ManagementOperationResult.Success($"任务 {jobId} 已标记为成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记失败任务成功失败，任务ID: {JobId}", jobId);
            return ManagementOperationResult.Failure("操作失败", ex.Message);
        }
    }

    public async Task<PagedResult<FeishuRequestLog>> GetRequestLogsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (items, total) = await _repository.QueryRequestLogsAsync(page, pageSize);

            return new PagedResult<FeishuRequestLog>
            {
                Items = items.ToList(),
                TotalCount = total,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询请求日志失败");
            return new PagedResult<FeishuRequestLog>();
        }
    }

    public async Task<PagedResult<FeishuResponseLog>> GetResponseLogsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (items, total) = await _repository.QueryResponseLogsAsync(page, pageSize);

            return new PagedResult<FeishuResponseLog>
            {
                Items = items.ToList(),
                TotalCount = total,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询响应日志失败");
            return new PagedResult<FeishuResponseLog>();
        }
    }

    public async Task<PagedResult<FeishuManageLog>> GetManageLogsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var items = await _repository.QueryManageLogsAsync(page, pageSize);
            var totalCount = await GetManageLogsTotalCountAsync();

            return new PagedResult<FeishuManageLog>
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询管理日志失败");
            return new PagedResult<FeishuManageLog>();
        }
    }

    public async Task<FeishuHealthCheckResult> CheckSystemHealthAsync()
    {
        return await _healthService.CheckHealthAsync();
    }

    public async Task<string> GenerateTypeScriptCodeAsync(string approvalCode, string interfaceName = null)
    {
        try
        {
            _logger.LogInformation("开始生成TypeScript代码，审批代码: {ApprovalCode}", approvalCode);

            var response = await _definitionService.GetDefinitionDetailAsync(approvalCode);
            if (response?.Data == null)
            {
                throw new InvalidOperationException("无法获取审批定义详情");
            }

            var generatedCode = await _typeScriptCodeGenerator.GenerateTypeScriptInterfaceAsync(response.Data, interfaceName);

            _logger.LogInformation("TypeScript代码生成成功，审批代码: {ApprovalCode}", approvalCode);

            await _repository.SaveManageLogAsync(new FeishuManageLog
            {
                Operation = "generate_typescript_code",
                Description = $"生成审批 {approvalCode} 的TypeScript接口代码",
                Result = "Success"
            });

            return generatedCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成TypeScript代码失败，审批代码: {ApprovalCode}", approvalCode);

            await _repository.SaveManageLogAsync(new FeishuManageLog
            {
                Operation = "generate_typescript_code",
                Description = $"生成审批 {approvalCode} 的TypeScript接口代码失败",
                Result = "Failed",
                ErrorMessage = ex.Message
            });

            throw;
        }
    }

    public async Task<ManagementOperationResult> SubscribeApprovalAsync(string approvalCode, string adminPassword)
    {
        try
        {
            if (!await VerifyAdminPasswordAsync(adminPassword))
            {
                return ManagementOperationResult.Failure("管理员密码错误");
            }

            _logger.LogInformation("开始订阅审批，审批代码: {ApprovalCode}", approvalCode);

            await _subscriptionService.SubscribeApprovalAsync(approvalCode);

            _logger.LogInformation("订阅审批成功，审批代码: {ApprovalCode}", approvalCode);

            await _repository.SaveManageLogAsync(new FeishuManageLog
            {
                Operation = "subscribe_approval",
                Description = $"订阅审批 {approvalCode}",
                Result = "Success"
            });

            return ManagementOperationResult.Success($"成功订阅审批 {approvalCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅审批失败，审批代码: {ApprovalCode}", approvalCode);

            await _repository.SaveManageLogAsync(new FeishuManageLog
            {
                Operation = "subscribe_approval",
                Description = $"订阅审批 {approvalCode} 失败",
                Result = "Failed",
                ErrorMessage = ex.Message
            });

            return ManagementOperationResult.Failure("订阅失败", ex.Message);
        }
    }
     
    /// <summary>
    /// 飞书AES解密（AES-256-CBC）
    /// </summary>
    public string DecryptFeishuData(string encryptedData, string encryptKey)
    {
        if (string.IsNullOrWhiteSpace(encryptedData) || string.IsNullOrWhiteSpace(encryptKey))
            return null;
        try
        {
            // 解密Base64编码的数据
            byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

            // 提取IV（前16字节）
            byte[] iv = new byte[16];
            Array.Copy(encryptedBytes, 0, iv, 0, 16);

            // 提取实际加密数据
            byte[] cipherBytes = new byte[encryptedBytes.Length - 16];
            Array.Copy(encryptedBytes, 16, cipherBytes, 0, cipherBytes.Length);

            // 使用SHA256哈希加密密钥
            byte[] keyBytes;
            using (var sha256 = SHA256.Create())
            {
                keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptKey));
            }

            // AES-256-CBC解密
            using (var aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解密失败: {ex.Message}");
            return null;
        }
    }
    
    #region 原有私有方法
    private async Task<bool> CheckAdminPasswordExistsAsync()
    {
        try
        {
            return !await _repository.VerifyAdminPasswordAsync("__temp_check_password__");
        }
        catch
        {
            return false;
        }
    }

    private static string MaskAppId(string appId)
    {
        if (string.IsNullOrEmpty(appId))
            return "未配置";

        if (appId.Length <= 8)
            return appId;

        return $"{appId.Substring(0, 4)}****{appId.Substring(appId.Length - 4)}";
    }

    private static int CalculateCompletionPercentage(bool isConfigured, bool hasPassword)
    {
        var score = 0;
        if (hasPassword) score += 50;
        if (isConfigured) score += 50;
        return score;
    }

    private async Task<int> GetFailedJobsTotalCountAsync()
    {
        var jobs = await _repository.QueryFailedJobsAsync(1, int.MaxValue);
        return jobs.Count();
    }

    private async Task<int> GetManageLogsTotalCountAsync()
    {
        var logs = await _repository.QueryManageLogsAsync(1, int.MaxValue);
        return logs.Count();
    }

    private string GenerateEntityCode(object definitionData, string approvalCode)
    {
        try
        {
            var approvalName = "";
            var json = System.Text.Json.JsonSerializer.Serialize(definitionData);
            using var d = System.Text.Json.JsonDocument.Parse(json);

            if (d.RootElement.TryGetProperty("approval_name", out var nameProperty))
            {
                approvalName = nameProperty.GetString() ?? "";
            }

            var className = GenerateClassName(approvalName, approvalCode);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("// 将以下类复制到你的业务项目中，并根据需要调整命名空间");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine("");
            sb.AppendLine("namespace YourProject.Approvals;");
            sb.AppendLine("");
            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// {approvalName}");
            sb.AppendLine($"/// 审批代码: {approvalCode}");
            sb.AppendLine($"/// 自动生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"/// </summary>");
            sb.AppendLine($"[ApprovalCode(\"{approvalCode}\")]");
            sb.AppendLine($"public class {className}Request");
            sb.AppendLine("{");

            if (definitionData != null)
            {
                var formFields = new List<System.Text.Json.JsonElement>();

                if (d.RootElement.TryGetProperty("form", out var form))
                {
                    if (form.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var formJsonString = form.GetString();
                        if (!string.IsNullOrEmpty(formJsonString))
                        {
                            try
                            {
                                using var formDoc = System.Text.Json.JsonDocument.Parse(formJsonString);
                                if (formDoc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    formFields.AddRange(formDoc.RootElement.EnumerateArray());
                                }
                            }
                            catch
                            {
                                // 忽略解析错误
                            }
                        }
                    }
                    else if (form.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        formFields.AddRange(form.EnumerateArray());
                    }
                }

                if (formFields.Count > 0)
                {
                    foreach (var field in formFields)
                    {
                        var name = field.TryGetProperty("name", out var n) ? n.GetString() : "Field";
                        var key = field.TryGetProperty("custom_id", out var customId) ? customId.GetString() :
                                 field.TryGetProperty("id", out var id) ? id.GetString() :
                                 field.TryGetProperty("key", out var k) ? k.GetString() :
                                 name;
                        var type = field.TryGetProperty("type", out var t) ? t.GetString() : "string";

                        var csharpType = type switch
                        {
                            "input" => "string",
                            "textarea" => "string",
                            "number" => "int?",
                            "decimal" => "decimal?",
                            "date" => "string",
                            "datetime" => "string",
                            "boolean" => "bool?",
                            "amount" => "string",
                            "contact" => "string",
                            "radioV2" => "string",
                            "checkboxV2" => "string[]",
                            "select" => "string",
                            "multiSelect" => "string[]",
                            "fieldList" => "object",
                            "attachment" => "string[]",
                            "image" => "string[]",
                            _ => "string"
                        };

                        if (!string.IsNullOrEmpty(key))
                        {
                            sb.AppendLine($"    /// <summary>{name ?? key}</summary>");
                            sb.AppendLine($"    [JsonPropertyName(\"{key}\")]");
                            sb.AppendLine($"    public {csharpType} {MakeValidIdentifier(key)} {{ get; set; }}");
                            sb.AppendLine("");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("    // 注意: 无法解析审批表单结构，请手动添加字段");
                    sb.AppendLine("    [JsonPropertyName(\"field1\")]");
                    sb.AppendLine("    public string Field1 { get; set; }");
                }
            }
            else
            {
                sb.AppendLine("    // 注意: 审批定义数据为空，请手动添加字段");
                sb.AppendLine("    [JsonPropertyName(\"field1\")]");
                sb.AppendLine("    public string Field1 { get; set; }");
            }

            sb.AppendLine("}");
            sb.AppendLine("");
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// 用于标记审批代码的特性");
            sb.AppendLine("/// </summary>");
            sb.AppendLine("[AttributeUsage(AttributeTargets.Class)]");
            sb.AppendLine("public class ApprovalCodeAttribute : Attribute");
            sb.AppendLine("{");
            sb.AppendLine("    public string Code { get; }");
            sb.AppendLine("");
            sb.AppendLine("    public ApprovalCodeAttribute(string code)");
            sb.AppendLine("    {");
            sb.AppendLine("        Code = code;");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"// 代码生成失败: {ex.Message}\n// 请检查审批定义数据格式\npublic class UnknownApprovalRequest {{ }}";
        }
    }

    private static string GenerateClassName(string approvalName, string approvalCode)
    {
        var name = !string.IsNullOrEmpty(approvalName) ? approvalName : approvalCode;
        name = TranslateToEnglish(name);

        var words = name.Split(new[] { ' ', '-', '_', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            var cleanWord = MakeValidIdentifier(word);
            if (!string.IsNullOrEmpty(cleanWord))
            {
                result.Append(char.ToUpper(cleanWord[0]));
                if (cleanWord.Length > 1)
                {
                    result.Append(cleanWord.Substring(1).ToLower());
                }
            }
        }

        var className = result.ToString();
        return !string.IsNullOrEmpty(className) ? className : "UnknownApproval";
    }

    private static string TranslateToEnglish(string chineseText)
    {
        var translations = new Dictionary<string, string>
        {
            {"销售", "Sales"}, {"退货", "Return"}, {"采购", "Purchase"}, {"报销", "Expense"},
            {"请假", "Leave"}, {"出差", "Travel"}, {"加班", "Overtime"}, {"离职", "Resignation"},
            {"入职", "Onboard"}, {"转正", "Confirm"}, {"调薪", "SalaryAdjust"}, {"合同", "Contract"},
            {"财务", "Finance"}, {"人事", "HR"}, {"技术", "Tech"}, {"市场", "Marketing"}
        };

        var result = chineseText;
        foreach (var kvp in translations)
        {
            result = result.Replace(kvp.Key, kvp.Value);
        }

        var filteredResult = new System.Text.StringBuilder();
        foreach (char c in result)
        {
            if (char.IsLetter(c) || char.IsDigit(c) || c == ' ' || c == '-' || c == '_')
            {
                filteredResult.Append(c);
            }
        }

        return filteredResult.ToString();
    }

    private static string MakeValidIdentifier(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Field";

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsLetter(c) || char.IsDigit(c) || c == '_')
            {
                if (result.Length == 0 && char.IsDigit(c))
                {
                    result.Append('_');
                }
                result.Append(c);
            }
        }

        return result.Length > 0 ? result.ToString() : "Field";
    }

    public async Task<FeishuEventHandleResult> ProcessFeishuEventAsync(string decryptedEventData)
    {
        var eventData = JObject.Parse(decryptedEventData);
        var eventType = eventData["header"]?["event_type"]?.ToString();

        switch (eventType)
        {
            case "approval_task":
            case "approval_instance":
                return await ProcessApprovalEventAsync(eventData);

            case "im.message.receive_v1":
                return await ProcessMessageEventAsync(eventData);

            default:
                _logger.LogWarning("未处理的事件类型: {EventType}", eventType);
                return new FeishuEventHandleResult
                {
                    Success = true,
                    Message = "未处理的事件类型"
                };
        }
    }

    public async Task<FeishuEventHandleResult> ProcessMessageEventAsync(JObject eventData)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("尚未实现");
    }

    public async Task<FeishuEventHandleResult> ProcessApprovalEventAsync(JObject eventData)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("尚未实现");
    } 
    #endregion
}

#region 飞书请求模型（内部使用）
/// <summary>
/// 飞书加密请求模型
/// </summary>
internal class EncryptedFeishuRequest
{
    [JsonProperty("encrypt")]
    public string Encrypt { get; set; }
}

/// <summary>
/// 飞书验证请求模型
/// </summary>
internal class FeishuVerificationRequest
{
    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("challenge")]
    public string Challenge { get; set; }
}
#endregion
