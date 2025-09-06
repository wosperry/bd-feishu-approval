# BD.FeishuSDK - 企业级飞书审批工作流SDK

[![NuGet](https://img.shields.io/nuget/v/BD.FeishuSDK.Approval.svg)](https://www.nuget.org/packages/BD.FeishuSDK.Approval/)
[![Downloads](https://img.shields.io/nuget/dt/BD.FeishuSDK.Approval.svg)](https://www.nuget.org/packages/BD.FeishuSDK.Approval/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()

> **一行代码集成**飞书审批工作流，2分钟内完成企业级功能集成，开箱即用。

## 🚀 快速开始（2分钟）

### 1. 安装NuGet包

```bash
dotnet add package BD.FeishuSDK.Approval
```

### 2. 在Program.cs中加一行代码

```csharp
using BD.FeishuApproval.Extensions;
using BD.FeishuApproval.Dashboard;

var builder = WebApplication.CreateBuilder(args);

// 🎯 一行代码集成 - 智能扫描当前项目处理器
builder.Services.AddFeishuApproval("Data Source=feishu.db", "sqlite");

// 🎨 添加新一代模板系统支持（可自定义UI）
builder.Services.AddFeishuDashboardTemplatesForProduction();

var app = builder.Build();

// 🚀 启用内置管理界面 V2 (基于模板系统)
FeishuDashboardTemplateExtensions.MapFeishuDashboardV2(app, new FeishuDashboardOptions());

app.Run();
```

> **🔒 企业级安全提示**：SDK默认只扫描你的主程序集，不会随意扫描第三方DLL。如需更精确控制，请参考下方的高级配置选项。

### 3. 推荐的项目结构

为了更好地组织审批相关代码，建议在项目中创建以下文件夹结构：

```
YourProject/
├── Models/
│   └── Approvals/          # 审批参数类文件夹
│       ├── LeaveApprovalDto.cs
│       ├── ExpenseApprovalDto.cs
│       └── PurchaseApprovalDto.cs
├── Handlers/
│   └── Approvals/          # 审批处理器文件夹
│       ├── LeaveApprovalHandler.cs
│       ├── ExpenseApprovalHandler.cs
│       └── PurchaseApprovalHandler.cs
├── Controllers/
│   └── ApprovalController.cs
└── Program.cs
```

### 4. 配置并使用

1. **启动应用** → 访问 `http://localhost:5000/feishu`
2. **设置管理口令** → 保护管理界面安全
3. **添加飞书凭证** → 输入你的 `app_id` 和 `app_secret`
4. **登记审批类型** → 添加飞书中的审批代码
5. **生成DTO类** → 复制生成的代码到 `Models/Approvals/` 文件夹
6. **创建处理器** → 在 `Handlers/Approvals/` 文件夹中实现处理器
7. **注册处理器** → 在 Program.cs 中注册
8. **开始创建审批** → 在控制器中使用服务

## 💡 为什么选择这个SDK？

| 功能特性 | 本SDK | 自己开发 |
|---------|-------|---------|
| **集成时间** | 2分钟 | 数周 |
| **数据库支持** | 自动检测14种数据库 | 手动配置 |
| **管理界面** | 内置Web控制台 | 从零开发 |
| **代码生成** | 自动生成DTO+处理器 | 手工编码 |
| **类型安全** | 编译时强制检查 | 运行时才发现错误 |
| **处理器扫描** | 智能扫描+精确控制 | 手动注册 |
| **回调处理** | 自动分发+策略模式 | 复杂路由逻辑 |
| **错误处理** | 企业级异常处理 | 自己实现 |
| **批量操作** | 内置支持 | 复杂实现 |
| **健康监控** | 完整监控体系 | 额外工作 |
| **API文档** | 完整文档 | 自己编写 |

## 🛠️ 开发者指南

### 🗂️ 代码组织最佳实践

#### 1. 审批参数类管理 (`Models/Approvals/`)

将生成的审批DTO类统一放在此文件夹：

```csharp
// Models/Approvals/LeaveApprovalDto.cs
using BD.FeishuApproval.Shared.Abstractions;

namespace YourProject.Models.Approvals;

[ApprovalCode("LEAVE_APPROVAL_001", Type = "leave")]
public class LeaveApprovalDto : FeishuApprovalRequestBase
{
    [JsonPropertyName("leave_type")]
    public string LeaveType { get; set; } = string.Empty;
    
    [JsonPropertyName("days")]
    public int Days { get; set; }
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
```

#### 2. 审批处理器管理 (`Handlers/Approvals/`)

为每个审批类型创建对应的处理器：

```csharp
// Handlers/Approvals/LeaveApprovalHandler.cs
using BD.FeishuApproval.Handlers;
using YourProject.Models.Approvals;

namespace YourProject.Handlers.Approvals;

public class LeaveApprovalHandler : ApprovalHandlerBase<LeaveApprovalDto>
{
    private readonly IEmailService _emailService;
    private readonly ILeaveService _leaveService;

    public LeaveApprovalHandler(
        IFeishuApprovalInstanceService instanceService,
        ILogger<LeaveApprovalHandler> logger,
        IEmailService emailService,
        ILeaveService leaveService) 
        : base(instanceService, logger)
    {
        _emailService = emailService;
        _leaveService = leaveService;
    }

    // 审批通过后的业务处理
    public override async Task HandleApprovalApprovedAsync(
        LeaveApprovalDto approvalData, 
        FeishuCallbackEvent callbackEvent)
    {
        // 1. 更新假期余额
        await _leaveService.DeductLeaveBalanceAsync(
            approvalData.UserId, 
            approvalData.Days);

        // 2. 发送通知邮件
        await _emailService.SendApprovalNotificationAsync(
            approvalData.UserId, 
            $"您的{approvalData.Days}天{approvalData.LeaveType}已通过");

        // 3. 记录到HR系统
        await _leaveService.RecordApprovedLeaveAsync(approvalData);

        await base.HandleApprovalApprovedAsync(approvalData, callbackEvent);
    }

    // 审批前的验证
    protected override async Task ValidateApprovalRequestAsync(LeaveApprovalDto request)
    {
        // 检查假期余额
        var balance = await _leaveService.GetLeaveBalanceAsync(request.UserId);
        if (balance < request.Days)
        {
            throw new InvalidOperationException($"假期余额不足，剩余{balance}天");
        }

        await base.ValidateApprovalRequestAsync(request);
    }
}
```

#### 3. 处理器注册 (`Program.cs`) - **多种灵活选项**

```csharp
// 🎯 方式一：默认智能扫描（推荐）
// 自动扫描当前项目程序集，安全且便捷
builder.Services.AddFeishuApproval("Data Source=feishu.db", "sqlite");

// 🎯 方式二：指定程序集扫描（推荐用于多项目解决方案）
// 只扫描你指定的程序集，完全可控
builder.Services.AddFeishuApproval(
    baseAddress: null, // 使用默认飞书地址
    typeof(Program).Assembly,           // 扫描主程序集
    typeof(LeaveApprovalHandler).Assembly  // 扫描业务程序集
);

// 🎯 方式三：完全手动控制（推荐用于企业级项目）
// 不自动扫描任何程序集，手动精确控制
builder.Services.AddFeishuApprovalWithoutAutoScan();

// 然后手动注册具体处理器
builder.Services.AddApprovalHandler<LeaveApprovalHandler, LeaveApprovalDto>();
builder.Services.AddApprovalHandler<ExpenseApprovalHandler, ExpenseApprovalDto>();

// 🎯 方式四：混合模式（灵活性最高）
builder.Services.AddFeishuApprovalWithoutAutoScan();

// 手动注册重要的处理器
builder.Services.AddApprovalHandler<CriticalApprovalHandler, CriticalApprovalDto>();

// 自动扫描特定程序集中的其他处理器
builder.Services.AddApprovalHandlersFromAssembly(typeof(CommonHandlers).Assembly);
```

**🚀 哪种方式适合你？**

| 场景 | 推荐方式 | 优势 |
|------|---------|------|
| 🏃 快速原型/小项目 | 方式一：默认智能扫描 | 零配置，开箱即用 |
| 🏢 企业多项目解决方案 | 方式二：指定程序集 | 精确控制，性能最优 |
| 🔒 安全敏感/大型项目 | 方式三：完全手动 | 绝对可控，无意外扫描 |
| ⚡ 复杂业务场景 | 方式四：混合模式 | 兼顾便利性和控制力 |

#### 4. 控制器使用 (`Controllers/`)

```csharp
// Controllers/ApprovalController.cs
[ApiController]
[Route("api/[controller]")]
public class ApprovalController : ControllerBase
{
    // 方式一：直接注入具体处理器（类型安全）
    private readonly IApprovalHandler<LeaveApprovalDto> _leaveHandler;
    private readonly IApprovalHandler<ExpenseApprovalDto> _expenseHandler;

    // 方式二：注入处理器注册表（动态路由）
    private readonly IApprovalHandlerRegistry _handlerRegistry;

    public ApprovalController(
        IApprovalHandler<LeaveApprovalDto> leaveHandler,
        IApprovalHandler<ExpenseApprovalDto> expenseHandler,
        IApprovalHandlerRegistry handlerRegistry)
    {
        _leaveHandler = leaveHandler;
        _expenseHandler = expenseHandler;
        _handlerRegistry = handlerRegistry;
    }

    // 类型安全的审批创建
    [HttpPost("leave")]
    public async Task<IActionResult> CreateLeaveApproval([FromBody] LeaveApprovalDto request)
    {
        var result = await _leaveHandler.CreateApprovalAsync(request);
        return Ok(result);
    }

    [HttpPost("expense")]  
    public async Task<IActionResult> CreateExpenseApproval([FromBody] ExpenseApprovalDto request)
    {
        var result = await _expenseHandler.CreateApprovalAsync(request);
        return Ok(result);
    }

    // 动态路由方式（适合审批类型很多的场景）
    [HttpPost("create/{approvalType}")]
    public async Task<IActionResult> CreateApproval(string approvalType, [FromBody] JsonElement requestData)
    {
        var handler = _handlerRegistry.GetHandler(approvalType);
        if (handler == null)
        {
            return BadRequest($"不支持的审批类型: {approvalType}");
        }

        // 创建回调事件来处理动态数据
        var callbackEvent = new FeishuCallbackEvent 
        { 
            Form = requestData.GetRawText(),
            ApprovalCode = approvalType
        };
        
        await handler.HandleCallbackAsync(callbackEvent);
        return Ok("审批创建成功");
    }
}
```

#### 5. 自定义飞书回调处理 (`Controllers/`)

SDK提供了内置的飞书回调处理基类，你只需继承并自定义相关逻辑：

```csharp
// Controllers/FeishuCallbackController.cs
using BD.FeishuApproval.Callbacks;
using BD.FeishuApproval.Controllers;
using BD.FeishuApproval.Shared.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[Route("api/feishu/approval/callback")]
public class FeishuCallbackController : FeishuCallbackControllerBase
{
    public FeishuCallbackController(
        IFeishuCallbackService callbackService,
        ILogger<FeishuCallbackController> logger) 
        : base(callbackService, logger)
    {
    }

    // ✅ 可选：自定义回调日志记录
    protected override async Task LogCallbackReceived(object callbackData)
    {
        _logger.LogInformation("🔔 收到飞书审批回调: {Data}", callbackData);
        
        // 可以添加自定义日志逻辑，如写入审计日志
        // await _auditService.LogCallbackAsync(callbackData);
        
        await base.LogCallbackReceived(callbackData);
    }

    // ✅ 可选：自定义成功响应格式
    protected override async Task<IActionResult> HandleCallbackSuccess(FeishuCallbackEvent callbackEvent)
    {
        return Ok(new 
        { 
            success = true, 
            message = "✅ 回调处理成功", 
            instanceCode = callbackEvent.InstanceCode,
            timestamp = DateTime.UtcNow
        });
    }

    // ✅ 可选：自定义回调验证逻辑
    protected override async Task<ValidationResult> ValidateCallback(FeishuCallbackEvent callbackEvent)
    {
        // 基础验证
        var baseResult = await base.ValidateCallback(callbackEvent);
        if (!baseResult.IsValid)
            return baseResult;

        // 自定义业务验证
        if (string.IsNullOrEmpty(callbackEvent.ApprovalCode))
        {
            return new ValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "缺少审批代码" 
            };
        }

        // 可以添加更多验证逻辑，如签名验证、IP白名单等
        // if (!await _securityService.ValidateCallbackSignatureAsync(callbackEvent))
        //     return new ValidationResult { IsValid = false, ErrorMessage = "签名验证失败" };

        return new ValidationResult { IsValid = true };
    }

    // ✅ 可选：自定义异常处理
    protected override async Task<IActionResult> HandleException(Exception ex, object callbackData)
    {
        _logger.LogError(ex, "❌ 处理飞书回调异常");
        
        // 可以添加自定义错误处理逻辑
        // await _alertService.SendErrorAlertAsync(ex, callbackData);
        
        return StatusCode(500, new 
        { 
            success = false, 
            message = "回调处理失败", 
            error = ex.Message,
            timestamp = DateTime.UtcNow
        });
    }
}
```

**🎯 可重写的方法说明**：

| 方法 | 用途 | 说明 |
|------|------|------|
| `LogCallbackReceived()` | 自定义日志记录 | 记录回调接收日志，支持审计 |
| `ParseCallbackData()` | 自定义数据解析 | 解析飞书回调数据格式 |
| `ValidateCallback()` | 自定义验证逻辑 | 添加签名、IP、业务验证 |
| `PreProcessCallback()` | 回调前预处理 | 回调处理前的准备工作 |
| `PostProcessCallback()` | 回调后后处理 | 回调处理后的清理工作 |
| `HandleCallbackSuccess()` | 自定义成功响应 | 定制成功响应格式 |
| `HandleCallbackFailure()` | 自定义失败响应 | 定制失败响应格式 |
| `HandleException()` | 自定义异常处理 | 添加错误告警、监控等 |

**💡 企业级安全提示**：
- ✅ **签名验证**：建议在 `ValidateCallback()` 中验证飞书回调签名
- ✅ **IP白名单**：限制只允许飞书服务器IP访问回调端点
- ✅ **审计日志**：记录所有回调请求用于审计和故障排查
- ✅ **监控告警**：回调失败时及时通知运维团队

### 🎯 快速开发工作流

1. **生成DTO类**：访问管理界面 → 输入审批代码 → 复制生成的代码到 `Models/Approvals/`
2. **创建处理器**：继承 `ApprovalHandlerBase<YourDto>` → 实现业务逻辑 → 放入 `Handlers/Approvals/`
3. **注册处理器**：在 `Program.cs` 中注册
4. **测试功能**：在控制器中注入并使用

### 数据库选项

```csharp
// 显式指定数据库类型（推荐）
builder.Services.AddFeishuApproval("你的连接字符串", "mysql");

// 或者使用便捷扩展方法
builder.Services.AddFeishuApprovalWithMySql("Server=localhost;Database=feishu;...");
builder.Services.AddFeishuApprovalWithSqlServer("Server=.;Database=feishu;...");
builder.Services.AddFeishuApprovalWithPostgreSql("Host=localhost;Database=feishu;...");
builder.Services.AddFeishuApprovalWithSQLite("Data Source=feishu.db");

// 测试环境使用内存数据库
builder.Services.AddFeishuApprovalWithInMemorySQLite();

// 支持的数据库类型字符串：
// "mysql", "sqlserver", "postgresql", "sqlite", "oracle", "clickhouse", 
// "dm", "kdbndp", "oscar", "mysqlconnector", "access", "openguass", 
// "questtdb", "hg", "custom"
```

### 🎨 自定义管理界面（新一代模板系统）

#### 方案一：使用内置模板（推荐）
```csharp
// 生产环境：使用嵌入式模板，启用缓存
builder.Services.AddFeishuDashboardTemplatesForProduction();

// 开发环境：支持模板热重载
builder.Services.AddFeishuDashboardTemplatesForDevelopment();
```

#### 方案二：使用自定义HTML模板
```csharp
// 从文件系统加载自定义模板
builder.Services.AddFeishuDashboardTemplatesFromFileSystem("./templates");

// 在 ./templates 目录放置:
// - dashboard.html （主控制面板）
// - manage.html （管理配置页面）
```

#### 方案三：完全自定义模板提供者
```csharp
builder.Services.AddFeishuDashboardTemplates(options =>
{
    // 使用自定义模板提供者类
    options.UseCustomTemplateProvider<MyCustomTemplateProvider>()
           .AddVariable("CompanyName", "我的企业")
           .AddVariable("Theme", "dark");
});
```

模板支持变量替换（如 `{{ApiPrefix}}`, `{{Title}}` 等），完全兼容现有API。

不想用模板系统？直接用管理API构建自己的界面：

```csharp
// 启用管理API
builder.Services.AddFeishuManagementApi();
app.MapControllers();

// 使用管理服务
public class CustomDashboardController : ControllerBase
{
    private readonly IFeishuManagementService _managementService;

    [HttpPost("setup")]
    public async Task<IActionResult> Setup([FromBody] SetupRequest request)
    {
        // 设置管理员密码
        await _managementService.SetAdminPasswordAsync(request.Password);
        
        // 配置飞书应用
        var config = new FeishuConfigRequest 
        { 
            AppId = request.AppId, 
            AppSecret = request.AppSecret 
        };
        await _managementService.SaveFeishuConfigAsync(config, request.Password);
        
        // 登记审批
        var approval = new ApprovalRegistrationRequest
        {
            ApprovalCode = request.ApprovalCode,
            DisplayName = request.DisplayName
        };
        await _managementService.RegisterApprovalAsync(approval, request.Password);
        
        return Ok("设置完成");
    }
}
```

或者使用便利客户端：

```csharp
public class MyService
{
    private readonly FeishuManagementApiClient _apiClient;

    public MyService(HttpClient httpClient)
    {
        _apiClient = new FeishuManagementApiClient(httpClient, "https://my-app.com");
    }

    public async Task<string> GenerateEntityCode(string approvalCode)
    {
        return await _apiClient.GenerateEntityCodeAsync(approvalCode);
    }
}
```

### 🔒 类型安全的审批实体

SDK提供类型安全的审批操作，防止开发者传错类型或审批代码：

#### ✅ 类型安全的优势

| 传统方式问题 | 类型安全解决方案 |
|------------|----------------|
| 手动传审批代码容易出错 | 审批代码从特性自动获取 |
| 可以传入任意对象类型 | 编译时强制类型检查 |
| 运行时才发现错误 | 编译时就能发现错误 |
| 代码重构困难 | IDE支持安全重构 |

### 自动生成的实体类

SDK自动从飞书审批表单生成类型安全的C#类：

```csharp
// 从飞书审批定义生成 - 实现IFeishuApprovalRequest接口确保类型安全
[ApprovalCode("leave_approval")]
public class LeaveApprovalRequest : FeishuApprovalRequestBase
{
    [JsonPropertyName("leave_type")]
    public string LeaveType { get; set; }

    [JsonPropertyName("start_time")]
    public string StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public string EndTime { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; }
}

// 在代码中类型安全地使用
var leaveRequest = new LeaveApprovalRequest
{
    LeaveType = "年假",
    StartTime = "2024-01-01",
    EndTime = "2024-01-03",
    Reason = "家庭旅行"
};

// 推荐：使用类型安全的扩展方法 - 审批代码自动从特性获取
await _instanceService.CreateTypedInstanceAsync(leaveRequest);

// 也可以使用原始方法，但容易出错
// await _instanceService.CreateInstanceAsync(new CreateInstanceRequest
// {
//     ApprovalCode = "leave_approval", // 容易与实际代码不一致
//     FormData = JsonSerializer.Serialize(leaveRequest)
// });
```

## 🏗️ 架构和扩展性

### 健康监控

```csharp
public class HealthController : ControllerBase
{
    private readonly IFeishuHealthCheckService _healthService;

    [HttpGet("health")]
    public async Task<IActionResult> CheckHealth()
    {
        var health = await _healthService.CheckHealthAsync();
        
        return health.OverallStatus switch
        {
            HealthStatus.Healthy => Ok(health),
            HealthStatus.Degraded => Ok(health),
            HealthStatus.Unhealthy => StatusCode(503, health),
            _ => StatusCode(500, health)
        };
    }
}
```

### 自定义审批策略

```csharp
public class LeaveApprovalStrategy : IApprovalStrategy
{
    public string ApprovalCode => "leave_approval";

    public async Task<bool> BeforeCreateAsync(CreateInstanceRequest request)
    {
        // 自定义验证逻辑
        var leaveData = JsonSerializer.Deserialize<LeaveApprovalRequest>(request.FormData);
        return DateTime.Parse(leaveData.StartTime) > DateTime.Now;
    }

    public async Task AfterCreateAsync(CreateInstanceResult result)
    {
        // 创建后的业务逻辑（通知等）
        await SendNotificationAsync(result);
    }
}

// 注册你的策略
builder.Services.AddScoped<IApprovalStrategy, LeaveApprovalStrategy>();
```

### 自定义配置提供者

```csharp
public class CustomConfigProvider : IFeishuConfigProvider
{
    public FeishuApiOptions GetApiOptions() => new()
    {
        AppId = Environment.GetEnvironmentVariable("FEISHU_APP_ID"),
        AppSecret = Environment.GetEnvironmentVariable("FEISHU_APP_SECRET"),
        BaseUrl = "https://open.feishu.cn"
    };

    public FeishuCallbackOptions GetCallbackOptions() => new()
    {
        EncryptKey = Environment.GetEnvironmentVariable("FEISHU_ENCRYPT_KEY"),
        VerificationToken = Environment.GetEnvironmentVariable("FEISHU_VERIFICATION_TOKEN")
    };
}

builder.Services.AddScoped<IFeishuConfigProvider, CustomConfigProvider>();
```

## 📊 功能路线图

### ✅ 已完成功能

- [x] **一行代码集成** - `AddFeishuApproval(connectionString, databaseType)`
- [x] **多数据库支持** - 14种数据库类型，包括MySQL、SQL Server、PostgreSQL、SQLite、Oracle、ClickHouse等
- [x] **类型安全约束** - 编译时强制类型检查，防止传错审批类型
- [x] **内置Web控制台** - `/feishu` 完整管理界面
- [x] **自动实体生成** - 从飞书表单生成C#类
- [x] **批量操作支持** - 创建、查询、撤销多个审批
- [x] **健康监控系统** - 实时系统状态检查
- [x] **完整日志记录** - 请求/响应/管理操作追踪
- [x] **管理API接口** - 所有控制台功能通过REST API暴露
- [x] **API客户端库** - 便利的API调用客户端
- [x] **安全加固** - PBKDF2密码哈希、SQL注入防护
- [x] **异常处理机制** - 完善的异常处理和重试机制
- [x] **自定义策略支持** - 可扩展的审批工作流模式
- [x] **NuGet包配置** - 生产就绪的包配置

### 🚧 开发中功能

### 📋 计划功能

### 🔮 未来考虑

## 🔧 生产环境配置

### 安全最佳实践

```csharp
// 生产环境使用HTTPS
app.UseHttpsRedirection();

// 添加安全头
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});

// 使用环境变量存储敏感数据
var connectionString = Environment.GetEnvironmentVariable("FEISHU_DB_CONNECTION")
    ?? throw new InvalidOperationException("数据库连接字符串未配置");

builder.Services.AddFeishuApproval(connectionString, "sqlite");

// 限制管理界面访问
app.MapFeishuDashboard(options =>
{
    options.ManagePath = "/internal/feishu/manage"; // 使用不易猜测的路径
});
```

### 性能优化

```csharp
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "BD.FeishuApproval": "Warning" // 生产环境减少日志输出
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db;Database=feishu;Max Pool Size=100;Command Timeout=30;"
  }
}

// 连接池优化
builder.Services.AddFeishuApprovalWithMySql(connectionString, options =>
{
    options.CommandTimeout = 30;
    options.ConnectionPoolSize = 50;
});
```

## 📊 监控和可观测性

### 健康检查

监控应用程序健康状态：

```http
GET /feishu/api/health
```

### 指标端点

```http
GET /feishu/api/failed-jobs     # 失败任务监控
GET /feishu/api/logs/requests   # 请求日志
GET /feishu/api/logs/responses  # 响应日志
GET /feishu/api/logs/manage     # 管理操作日志
```

### 控制台功能

- **🏠 控制面板** - `/feishu` - 系统状态概览
- **⚙️ 管理界面** - `/feishu/manage` - 配置和设置
- **📊 实时监控** - 实时系统健康和性能指标
- **🔍 日志浏览** - 可搜索的请求/响应日志
- **🛠️ 代码生成** - 一键生成C#实体类
- **📋 审批管理** - 登记和管理审批工作流

## 🤝 贡献和支持

### 贡献指南

我们欢迎社区贡献！请查看 [CONTRIBUTING.md](CONTRIBUTING.md) 了解详情。

### 问题和功能请求

### 文档资源

- [管理API指南](MANAGEMENT_API.md) - 完整的API参考
- [飞书开放平台](https://open.feishu.cn/document/) - 飞书官方文档
- [SqlSugar ORM](https://www.donet5.com/) - 数据库ORM文档

## 📄 许可证

本项目使用 [MIT License](LICENSE) 许可证。

---

<div align="center">

**⭐ 如果这个项目对您有帮助，请给我们一个星标！**

[⭐ 在GitHub上给星](https://github.com/wosperry/bd-feishu-sdk)

Made with ❤️ by wosperry

</div>