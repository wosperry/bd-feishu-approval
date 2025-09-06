# 🔧 飞书管理 API 使用指南

本SDK提供了完整的管理API，让您可以构建自己的管理界面，而不依赖内置的 `/feishu` 页面。

## 🚀 快速开始

### 1️⃣ 启用管理 API

```csharp
// Program.cs
using BD.FeishuApproval.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 基础飞书审批服务
builder.Services.AddFeishuApprovalWithAutoDetectDb(connectionString);

// 🔑 添加管理 API 支持
builder.Services.AddFeishuManagementApi();

var app = builder.Build();

// 启用控制器路由
app.MapControllers();

app.Run();
```

### 2️⃣ 直接使用管理服务

```csharp
public class CustomManagementController : ControllerBase
{
    private readonly IFeishuManagementService _managementService;

    public CustomManagementController(IFeishuManagementService managementService)
    {
        _managementService = managementService;
    }

    [HttpGet("my-custom-config-status")]
    public async Task<IActionResult> GetMyConfigStatus()
    {
        var status = await _managementService.GetConfigurationStatusAsync();
        
        // 自定义返回格式
        return Ok(new
        {
            configured = status.IsConfigured,
            completion = status.CompletionPercentage,
            appId = status.AppIdMasked
        });
    }
}
```

### 3️⃣ 使用便利客户端

```csharp
public class MyManagementService
{
    private readonly FeishuManagementApiClient _apiClient;

    public MyManagementService(HttpClient httpClient)
    {
        _apiClient = new FeishuManagementApiClient(httpClient, "https://my-app.com");
    }

    public async Task SetupSystem()
    {
        // 1. 设置管理员密码
        var passwordResult = await _apiClient.SetAdminPasswordAsync("my-secure-password");
        if (!passwordResult.IsSuccess)
        {
            throw new Exception($"设置密码失败: {passwordResult.ErrorMessage}");
        }

        // 2. 配置飞书应用
        var configResult = await _apiClient.SaveFeishuConfigAsync(
            appId: "cli_your_app_id",
            appSecret: "your_app_secret", 
            adminPassword: "my-secure-password"
        );
        
        if (!configResult.IsSuccess)
        {
            throw new Exception($"配置失败: {configResult.ErrorMessage}");
        }

        // 3. 登记审批流程
        var approvalResult = await _apiClient.RegisterApprovalAsync(
            approvalCode: "leave_approval",
            adminPassword: "my-secure-password",
            displayName: "请假申请",
            description: "员工请假审批流程"
        );

        Console.WriteLine($"审批登记成功，ID: {approvalResult.Data}");
    }
}
```

## 📋 完整 API 列表

### 🔧 系统配置 API

| 端点 | 方法 | 功能 | 返回类型 |
|------|------|------|---------|
| `/api/feishu-management/configuration-status` | GET | 获取系统配置状态 | `ConfigurationStatus` |
| `/api/feishu-management/admin-password` | POST | 设置管理员密码 | `ManagementOperationResult` |
| `/api/feishu-management/admin-password/verify` | POST | 验证管理员密码 | `bool` |
| `/api/feishu-management/feishu-config` | POST | 保存飞书应用配置 | `ManagementOperationResult` |

### 📋 审批管理 API

| 端点 | 方法 | 功能 | 返回类型 |
|------|------|------|---------|
| `/api/feishu-management/approvals` | POST | 登记审批流程 | `ManagementOperationResult<long>` |
| `/api/feishu-management/approvals` | GET | 获取已登记审批列表 | `List<FeishuApprovalRegistration>` |
| `/api/feishu-management/definitions/{code}` | GET | 获取审批定义详情 | `ApprovalDefinitionDetail` |
| `/api/feishu-management/code-generation/{code}` | GET | 生成实体类代码 | `string` |

### 🔍 系统监控 API

| 端点 | 方法 | 功能 | 返回类型 |
|------|------|------|---------|
| `/api/feishu-management/health` | GET | 系统健康检查 | `FeishuHealthCheckResult` |
| `/api/feishu-management/failed-jobs` | GET | 获取失败任务列表 | `PagedResult<FailedJob>` |
| `/api/feishu-management/failed-jobs/{id}/resolve` | POST | 标记失败任务为成功 | `ManagementOperationResult` |

### 📊 日志查询 API

| 端点 | 方法 | 功能 | 返回类型 |
|------|------|------|---------|
| `/api/feishu-management/logs/requests` | GET | 查询请求日志 | `PagedResult<FeishuRequestLog>` |
| `/api/feishu-management/logs/responses` | GET | 查询响应日志 | `PagedResult<FeishuResponseLog>` |
| `/api/feishu-management/logs/management` | GET | 查询管理操作日志 | `PagedResult<FeishuManageLog>` |

## 📖 详细使用示例

### 🎨 构建自定义管理界面

```html
<!DOCTYPE html>
<html>
<head>
    <title>飞书审批管理</title>
    <style>
        .status-card { padding: 20px; margin: 10px; border-radius: 8px; }
        .healthy { background: #d4edda; }
        .unhealthy { background: #f8d7da; }
    </style>
</head>
<body>
    <div id="status-dashboard">
        <h1>飞书审批系统状态</h1>
        <div id="config-status" class="status-card"></div>
        <div id="health-status" class="status-card"></div>
    </div>

    <script>
        async function loadDashboard() {
            try {
                // 获取配置状态
                const configResponse = await fetch('/api/feishu-management/configuration-status');
                const configStatus = await configResponse.json();
                
                document.getElementById('config-status').innerHTML = `
                    <h3>配置状态</h3>
                    <p>完成度: ${configStatus.completionPercentage}%</p>
                    <p>应用ID: ${configStatus.appIdMasked}</p>
                    <p>状态: ${configStatus.isConfigured ? '✅ 已配置' : '❌ 未配置'}</p>
                `;

                // 获取健康状态
                const healthResponse = await fetch('/api/feishu-management/health');
                const healthStatus = await healthResponse.json();
                
                const healthCard = document.getElementById('health-status');
                healthCard.className = `status-card ${healthStatus.overallStatus === 'Healthy' ? 'healthy' : 'unhealthy'}`;
                healthCard.innerHTML = `
                    <h3>系统健康</h3>
                    <p>整体状态: ${healthStatus.overallStatus}</p>
                    <p>数据库: ${healthStatus.databaseStatus}</p>
                    <p>飞书API: ${healthStatus.feishuApiStatus}</p>
                    <p>检查时间: ${new Date(healthStatus.checkTime).toLocaleString()}</p>
                `;

            } catch (error) {
                console.error('加载面板失败:', error);
            }
        }

        // 页面加载完成后初始化
        document.addEventListener('DOMContentLoaded', loadDashboard);
    </script>
</body>
</html>
```

### 🔐 带身份验证的管理操作

```csharp
public class SecureManagementService
{
    private readonly IFeishuManagementService _managementService;
    private readonly ILogger<SecureManagementService> _logger;

    public SecureManagementService(
        IFeishuManagementService managementService,
        ILogger<SecureManagementService> logger)
    {
        _managementService = managementService;
        _logger = logger;
    }

    public async Task<bool> ConfigureSystemAsync(
        string adminPassword,
        string appId, 
        string appSecret,
        string encryptKey = null,
        string verificationToken = null)
    {
        try
        {
            // 1. 验证管理员密码
            var passwordValid = await _managementService.VerifyAdminPasswordAsync(adminPassword);
            if (!passwordValid)
            {
                _logger.LogWarning("管理员密码验证失败");
                return false;
            }

            // 2. 保存飞书配置
            var config = new FeishuConfigRequest
            {
                AppId = appId,
                AppSecret = appSecret,
                EncryptKey = encryptKey,
                VerificationToken = verificationToken
            };

            var result = await _managementService.SaveFeishuConfigAsync(config, adminPassword);
            
            if (!result.IsSuccess)
            {
                _logger.LogError("保存飞书配置失败: {Error}", result.ErrorMessage);
                return false;
            }

            _logger.LogInformation("飞书配置保存成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置系统时发生异常");
            return false;
        }
    }

    public async Task<List<ApprovalSummary>> GetApprovalSummaryAsync()
    {
        var approvals = await _managementService.GetRegisteredApprovalsAsync();
        
        var summaries = new List<ApprovalSummary>();
        
        foreach (var approval in approvals)
        {
            try
            {
                var definition = await _managementService.GetApprovalDefinitionAsync(approval.ApprovalCode);
                
                summaries.Add(new ApprovalSummary
                {
                    Code = approval.ApprovalCode,
                    DisplayName = approval.DisplayName,
                    Description = approval.Description,
                    IsActive = true, // 可以根据定义状态判断
                    FieldCount = CountFormFields(definition),
                    RegisterTime = approval.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取审批 {Code} 的定义失败", approval.ApprovalCode);
                
                summaries.Add(new ApprovalSummary
                {
                    Code = approval.ApprovalCode,
                    DisplayName = approval.DisplayName,
                    Description = approval.Description,
                    IsActive = false,
                    FieldCount = 0,
                    RegisterTime = approval.CreatedAt
                });
            }
        }
        
        return summaries;
    }

    private int CountFormFields(ApprovalDefinitionDetail definition)
    {
        // 简化的字段计数逻辑
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(definition);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("form", out var form))
            {
                if (form.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    return form.GetArrayLength();
                }
            }
            
            return 0;
        }
        catch
        {
            return 0;
        }
    }
}

public class ApprovalSummary
{
    public string Code { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public int FieldCount { get; set; }
    public DateTime RegisterTime { get; set; }
}
```

### 📊 批量操作示例

```csharp
public class BatchManagementOperations
{
    private readonly IFeishuManagementService _managementService;

    public BatchManagementOperations(IFeishuManagementService managementService)
    {
        _managementService = managementService;
    }

    /// <summary>
    /// 批量登记多个审批流程
    /// </summary>
    public async Task<List<(string Code, bool Success, string Error)>> BatchRegisterApprovalsAsync(
        List<(string Code, string DisplayName, string Description)> approvals,
        string adminPassword)
    {
        var results = new List<(string Code, bool Success, string Error)>();

        foreach (var (code, displayName, description) in approvals)
        {
            try
            {
                var registration = new ApprovalRegistrationRequest
                {
                    ApprovalCode = code,
                    DisplayName = displayName,
                    Description = description
                };

                var result = await _managementService.RegisterApprovalAsync(registration, adminPassword);
                
                results.Add((code, result.IsSuccess, result.ErrorMessage));
            }
            catch (Exception ex)
            {
                results.Add((code, false, ex.Message));
            }
        }

        return results;
    }

    /// <summary>
    /// 批量生成实体代码
    /// </summary>
    public async Task<Dictionary<string, string>> BatchGenerateEntityCodesAsync(List<string> approvalCodes)
    {
        var results = new Dictionary<string, string>();

        var tasks = approvalCodes.Select(async code =>
        {
            try
            {
                var entityCode = await _managementService.GenerateEntityCodeAsync(code);
                return (code, entityCode);
            }
            catch (Exception ex)
            {
                return (code, $"// 生成失败: {ex.Message}");
            }
        });

        var completed = await Task.WhenAll(tasks);
        
        foreach (var (code, entityCode) in completed)
        {
            results[code] = entityCode;
        }

        return results;
    }

    /// <summary>
    /// 清理失败任务
    /// </summary>
    public async Task<int> CleanupFailedJobsAsync(TimeSpan olderThan)
    {
        var cutoffTime = DateTime.Now - olderThan;
        var page = 1;
        var cleanedCount = 0;

        while (true)
        {
            var failedJobs = await _managementService.GetFailedJobsAsync(page, 50);
            
            if (!failedJobs.Items.Any())
                break;

            var oldJobs = failedJobs.Items
                .Where(job => job.CreatedAt < cutoffTime)
                .ToList();

            foreach (var job in oldJobs)
            {
                try
                {
                    var result = await _managementService.ResolveFailedJobAsync(job.Id);
                    if (result.IsSuccess)
                    {
                        cleanedCount++;
                    }
                }
                catch
                {
                    // 忽略清理失败的任务
                }
            }

            if (failedJobs.Items.Count < 50)
                break;

            page++;
        }

        return cleanedCount;
    }
}
```

## 🔒 安全注意事项

### 1. 密码保护
```csharp
// ❌ 错误：硬编码密码
var result = await managementService.SaveFeishuConfigAsync(config, "123456");

// ✅ 正确：从安全配置读取
var adminPassword = configuration["FeishuManagement:AdminPassword"];
var result = await managementService.SaveFeishuConfigAsync(config, adminPassword);
```

### 2. API 访问控制
```csharp
[Authorize(Roles = "Admin")]
[ApiController]
public class MyManagementController : ControllerBase
{
    // 在自己的控制器中添加身份验证
}
```

### 3. 敏感数据处理
```csharp
// 记录操作日志时避免记录敏感信息
_logger.LogInformation("配置已更新，AppId: {AppId}", config.AppId.Substring(0, 8) + "****");
```

## 🎯 最佳实践

### 1. 错误处理
```csharp
public async Task<IActionResult> SafeOperation()
{
    try
    {
        var result = await _managementService.GetConfigurationStatusAsync();
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取配置状态失败");
        return StatusCode(500, new { error = "Internal server error" });
    }
}
```

### 2. 分页处理
```csharp
public async Task<IActionResult> GetLogs(int page = 1, int pageSize = 20)
{
    // 参数验证
    page = Math.Max(1, page);
    pageSize = Math.Min(Math.Max(1, pageSize), 100);
    
    var logs = await _managementService.GetManageLogsAsync(page, pageSize);
    
    return Ok(new
    {
        data = logs.Items,
        pagination = new
        {
            currentPage = logs.CurrentPage,
            totalPages = logs.TotalPages,
            totalCount = logs.TotalCount,
            hasNext = logs.HasNextPage,
            hasPrevious = logs.HasPreviousPage
        }
    });
}
```

### 3. 缓存优化
```csharp
[HttpGet("configuration-status")]
[ResponseCache(Duration = 30)] // 缓存30秒
public async Task<ActionResult<ConfigurationStatus>> GetConfigurationStatus()
{
    return await _managementService.GetConfigurationStatusAsync();
}
```

---

通过以上管理API，您可以完全替代内置的 `/feishu` 管理界面，构建符合自己业务需求的定制化管理系统！