# BD.FeishuSDK.Approval

[![NuGet](https://img.shields.io/nuget/v/BD.FeishuSDK.Approval.svg)](https://www.nuget.org/packages/BD.FeishuSDK.Approval)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download)

**极简的飞书审批集成方案** - 三步完成企业级审批系统搭建

## 为什么选择我们？

✅ **开门见山** - 三行代码完成集成，无复杂配置  
✅ **思路清晰** - 请求类 + 处理器，业务逻辑一目了然  
✅ **布局清爽** - 开箱即用的管理界面，无需前端开发  
✅ **生产就绪** - 多数据库支持、健康检查、批量操作全都有  

## 三分钟上手

### 1. 安装包

```bash
dotnet add package BD.FeishuSDK.Approval
```

### 2. 注册服务

```csharp
var builder = WebApplication.CreateBuilder(args);

// 一行代码集成飞书审批
builder.Services.AddFeishuApproval("Data Source=app.db", "sqlite");

var app = builder.Build();
app.MapControllers();
app.MapFeishuDashboardV2(); // 管理界面

app.Run();
```

### 3. 定义审批流程

**请求类** - 定义表单字段：
```csharp
[ApprovalCode("your-approval-code")]
public class LeaveRequest : FeishuApprovalRequestBase
{
    [JsonPropertyName("widget_name")]
    public string Name { get; set; }
    
    [JsonPropertyName("widget_days")]
    public int Days { get; set; }
}
```

**处理器** - 实现业务逻辑：
```csharp
public class LeaveHandler : ApprovalHandlerBase<LeaveRequest>
{
    public override async Task HandleApprovalApprovedAsync(ApprovalContext<LeaveRequest> context)
    {
        // 审批通过 - 你的业务逻辑
        var request = context.FormData;
        await UpdateLeaveStatus(request.Name, "approved");
    }

    public override async Task HandleApprovalRejectedAsync(ApprovalContext<LeaveRequest> context)
    {
        // 审批拒绝 - 你的业务逻辑
        await UpdateLeaveStatus(context.FormData.Name, "rejected");
    }
}
```

**就这么简单！** 访问 `/feishu` 进入管理界面，系统自动处理所有底层细节。

## 文件布局

清晰的项目结构，易于理解和维护：

```
你的项目/
├── FeishuApprovals/
│   └── LeaveRequests          
│       └── LeaveRequest.cs          # 审批请求类
│       └── LeaveRequest.cs          # 审批处理器
│   └── OtherRequests          
│       └── OtherRequest.cs          # 审批请求类
│       └── OtherRequest.cs          # 审批处理器
│
└── Program.cs                   # 三行代码完成集成
```

## 核心特性

### 🎯 极简 API
```csharp
// 发起审批 - 一行代码
await approvalService.CreateAsync(new LeaveRequest { Name = "张三", Days = 3 });

// 批量操作
await batchService.CreateManyAsync(requests);

// 健康检查
var health = await healthService.CheckAsync();
```

### 🗄️ 多数据库支持
```csharp
// 自动检测数据库类型
services.AddFeishuApprovalWithAutoDetectDb(connectionString);

// 或手动指定
services.AddFeishuApprovalWithMySql(connectionString);
services.AddFeishuApprovalWithSqlServer(connectionString);
services.AddFeishuApprovalWithPostgreSql(connectionString);
services.AddFeishuApprovalWithSQLite(connectionString);
```

### 🎨 管理界面

内置美观的管理界面，支持：
- 📊 系统概览和统计
- ⚙️ 飞书应用配置
- 📋 审批实例管理
- 🔍 健康状态监控
- 📝 实时日志查看
- 🛠️ 代码自动生成

```csharp
// 使用默认界面
app.MapFeishuDashboardV2();

// 自定义界面模板
builder.Services.AddFeishuDashboardTemplatesForDevelopment("./templates");
```

### 🔍 完整的监控

```csharp
// 系统健康检查
var health = await managementService.GetSystemHealthAsync();
Console.WriteLine($"数据库: {health.Database.IsHealthy}");
Console.WriteLine($"飞书API: {health.FeishuApi.IsHealthy}");

// 配置状态检查  
var config = await managementService.GetConfigurationStatusAsync();
Console.WriteLine($"配置完成度: {config.CompletionPercentage}%");
```

## 生产环境配置

### 安全配置
```csharp
builder.Services.AddFeishuApproval(connectionString, "mysql", options =>
{
    options.AppId = Environment.GetEnvironmentVariable("FEISHU_APP_ID");
    options.AppSecret = Environment.GetEnvironmentVariable("FEISHU_APP_SECRET");
    options.EncryptKey = Environment.GetEnvironmentVariable("FEISHU_ENCRYPT_KEY");
    options.VerificationToken = Environment.GetEnvironmentVariable("FEISHU_VERIFICATION_TOKEN");
});
```

### 性能优化
```csharp
// 生产环境模板配置
builder.Services.AddFeishuDashboardTemplatesForProduction();

// 启用请求/响应日志
options.EnableRequestLogging = true;
options.EnableResponseLogging = true;
```

## 完整示例

查看 [`samples/FeishuApproval.SampleWeb`](samples/FeishuApproval.SampleWeb) 获取完整的示例项目，包含：
- 完整的请假审批流程
- 自定义验证和业务逻辑
- 管理界面集成
- 数据库配置

## 文档

- [Dashboard V2 使用指南](DASHBOARD_V2_USAGE.md) - 自定义界面模板
- [Demo 审批使用指南](DEMO_APPROVAL_USAGE.md) - 快速上手示例  
- [管理 API 使用指南](MANAGEMENT_API.md) - 构建自定义管理功能
- [改进总结](IMPROVEMENTS_SUMMARY.md) - 版本更新说明

## 许可证

MIT License - 详见 [LICENSE](LICENSE)

## 支持

- 🐛 [提交 Issue](https://github.com/wosperry/bd-feishu-sdk/issues)
- 💬 [参与讨论](https://github.com/wosperry/bd-feishu-sdk/discussions)  
- 📚 [查看文档](https://github.com/wosperry/bd-feishu-sdk)

---

⭐ 如果这个项目对你有帮助，请给一个 Star！