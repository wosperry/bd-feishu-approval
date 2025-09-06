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

var app = builder.Build();

app.Run();
```

> **🔒 企业级安全提示**：SDK默认只扫描你的主程序集，不会随意扫描第三方DLL。如需更精确控制，请参考下方的高级配置选项。

### 3. 推荐的项目结构

为了更好地组织审批相关代码，建议在项目中创建以下文件夹结构：

```
YourProject/
├── FeishuApprovals/ # 飞书审批文件夹
│    └── LeaveApproval/        
│          ├── LeaveApprovalDto.cs
│          ├── LeaveApprovalHandler.cs
│    └── PurchaseApproval/          
│          ├── PurchaseApprovalDto.cs
│          ├── PurchaseApprovalHandler.cs
│    └── ExpenseApproval/          
│          ├── ExpenseApprovalDto.cs
│          ├── ExpenseApprovalHandler.cs
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
using BD.FeishuApproval.Abstractions.Handlers;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Handlers;

namespace FeishuApproval.SampleWeb.FeishuApprovals.DemoApproval;

/// <summary>
/// Demo 审批处理器
/// </summary>
public class DemoApprovalHandler(
    IFeishuApprovalInstanceService instanceService,
    ILogger<DemoApprovalHandler> logger)
    : ApprovalHandlerBase<DemoApprovalRequest>(instanceService, logger)
{
    #region ===== 必须实现的回调处理方法 =====

    /// <summary>
    /// 审批通过后的处理逻辑
    /// </summary>
    public override Task HandleApprovalApprovedAsync(ApprovalContext<DemoApprovalRequest> context)
    {
        throw new NotImplementedException("请实现审批通过后的业务逻辑处理");
    }

    /// <summary>
    /// 审批拒绝后的处理逻辑
    /// </summary>
    public override Task HandleApprovalRejectedAsync(ApprovalContext<DemoApprovalRequest> context)
    {
        throw new NotImplementedException("请实现审批拒绝后的业务逻辑处理");
    }

    /// <summary>
    /// 审批撤回后的处理逻辑
    /// </summary>
    public override Task HandleApprovalCancelledAsync(ApprovalContext<DemoApprovalRequest> context)
    {
        throw new NotImplementedException("请实现审批撤回后的业务逻辑处理");
    }

    /// <summary>
    /// 审批状态未知时的处理逻辑
    /// </summary>
    public override Task HandleUnknownStatusAsync(ApprovalContext<DemoApprovalRequest> context)
    {
        throw new NotImplementedException("请实现未知状态的处理逻辑");
    }

    /// <summary>
    /// 业务异常处理逻辑
    /// </summary>
    public override Task HandleBusinessExceptionAsync(ApprovalContext<DemoApprovalRequest> context, Exception exception)
    {
        throw new NotImplementedException("请实现业务异常处理逻辑");
    }

    #endregion

    #region ===== 可选的校验和生命周期钩子 =====

    /// <summary>
    /// 审批请求验证逻辑（可选重写）
    /// </summary>
    protected override async Task ValidateApprovalRequestAsync(DemoApprovalRequest request)
    {
        // 在这里实现自定义的验证逻辑
        // 如果验证失败，抛出相应的异常
        await Task.CompletedTask;
    }

    /// <summary>
    /// 审批创建前预处理逻辑（可选重写）ersAsync(request);
    /// request.PrepareAdditionalData();
    /// </summary>
    protected override async Task PreProcessApprovalAsync(DemoApprovalRequest request)
    {
        // 在这里实现创建审批前的预处理逻辑
        await Task.CompletedTask;
    }

    /// <summary>
    /// 审批创建成功后处理逻辑（可选重写）
    /// </summary>
    protected override async Task PostProcessApprovalAsync(DemoApprovalRequest request, BD.FeishuApproval.Shared.Dtos.Instances.CreateInstanceResult result)
    {
        // 在这里实现审批创建成功后的处理逻辑
        await Task.CompletedTask;
    }

    /// <summary>
    /// 审批创建失败处理逻辑（可选重写
    /// </summary>
    protected override async Task HandleCreateFailureInternalAsync(DemoApprovalRequest request, Exception exception)
    {
        // 在这里实现审批创建失败时的处理逻辑
        await Task.CompletedTask;
    }

    #endregion
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

    [ApiController]
    [Route("api/[controller]")]
    public async Task<IActionResult> CreateDemoApproval([FromBody] DemoApprovalRequest request)
    {
        try
        {
            var demo = new DemoApprovalRequest
            {
                姓名 = "张三",
                年龄_岁 = 15
            };
            var result = await _approvalService.CreateApprovalAsync(demo, _fakeUserId);

            
            return Ok(new
            {
                Success = true,
                Message = "Demo审批创建成功",
                Data = new
                {
                    InstanceCode = result.InstanceCode,
                    ApprovalCode = "6A109ECD-3578-4243-93F9-DBDCF89515AF", // 从特性获取
                    UserId = _fakeUserId,
                    CreateTime = DateTime.Now,
                    FormData = new
                    {
                        Name = request.姓名,
                        Age = request.年龄_岁
                    }
                }
            });
        }
        catch (InvalidOperationException ex){}
        catch (ArgumentException ex){}
        catch (Exception ex){}
    }
```

#### 5. 自定义飞书回调处理 (`Controllers/`)


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

### 🎨 自定义管理界面

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

```

## 🏗️ 架构和扩展性

### 健康监控

```csharp
```


### ✅ 已完成功能

- [x] **一行代码集成** - `AddFeishuApproval(connectionString, databaseType)`
- [x] **多数据库支持** - 14种数据库类型，包括MySQL、SQL Server、PostgreSQL、SQLite、Oracle、ClickHouse等
- [x] **类型安全约束** - 编译时强制类型检查，防止传错审批类型
- [x] **内置Web控制台** - `/feishu` 完整管理界面
- [x] **自动实体生成** - 从飞书表单生成C#类
- [x] **健康监控系统** - 实时系统状态检查
- [x] **完整日志记录** - 请求/响应/管理操作追踪
- [x] **管理API接口** - 所有控制台功能通过REST API暴露
- [x] **API客户端库** - 便利的API调用客户端
- [x] **安全加固** - PBKDF2密码哈希、SQL注入防护 

### 🚧 TODO

- [ ] **自定义策略支持** - 可扩展的审批工作流模式
- [ ] **NuGet包配置** - 生产就绪的包配置
- [ ] **处理飞书回调事件，建 FeishuEventCallback表记录，UUID幂等
- [ ] **处理飞书回调事件，处理器类处理不同审批状态 approve cancel reject等，分发给对应的处理器

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