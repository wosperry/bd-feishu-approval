# 飞书Dashboard V2 使用指南

## 🎯 概述

Dashboard V2 采用了全新的模板系统设计，支持：
- 🔧 灵活的HTML模板自定义
- 📁 文件系统和嵌入资源两种模式
- ⚡ 模板缓存和热重载
- 🎨 完全自定义UI界面
- 🔄 向后兼容的API接口

## 🚀 快速开始

### 1. 基础用法（使用默认模板）

```csharp
using BD.FeishuApproval.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 添加飞书审批服务
builder.Services.AddFeishuApprovalWithAutoDetectDb("Data Source=feishu.db");

// 添加模板系统
builder.Services.AddFeishuDashboardTemplates();

var app = builder.Build();

// 使用新版Dashboard
app.MapFeishuDashboardV2();

app.Run();
```

### 2. 开发模式（热重载 + 文件系统）

```csharp
builder.Services.AddFeishuDashboardTemplatesForDevelopment("./custom-templates");

app.MapFeishuDashboardV2(options => {
    options.PathPrefix = "/admin";
    options.ManagePath = "/admin/config";
});
```

### 3. 生产模式（缓存 + 嵌入资源）

```csharp
builder.Services.AddFeishuDashboardTemplatesForProduction();

app.MapFeishuDashboardV2();
```

## 🎨 自定义模板

### 使用文件系统模板

```csharp
builder.Services.AddFeishuDashboardTemplates(options =>
{
    options.UseFileSystemTemplates("./templates")
           .AddVariable("CompanyName", "我的公司")
           .AddVariable("SupportEmail", "support@example.com")
           .EnableDevelopmentMode(); // 开发时启用
});
```

然后在 `./templates` 目录下创建：

```
templates/
├── dashboard.html    # 主控制面板
├── manage.html       # 管理页面
├── css/
│   └── custom.css    # 自定义样式
└── js/
    └── custom.js     # 自定义脚本
```

### 模板变量使用

在HTML模板中使用 `{{变量名}}` 格式：

```html
<title>{{CompanyName}} - 飞书管理系统</title>
<a href="{{ApiPrefix}}/docs">API文档</a>
<p>版本: {{Version}}</p>
<p>更新时间: {{Timestamp}}</p>
```

系统预定义变量：
- `{{ApiPrefix}}` - API路径前缀
- `{{PathPrefix}}` - Dashboard路径前缀
- `{{ManagePath}}` - 管理页面路径
- `{{Version}}` - SDK版本
- `{{Timestamp}}` - 当前时间

## 🏗️ 高级自定义

### 自定义模板提供者

```csharp
public class DatabaseTemplateProvider : IFeishuDashboardTemplateProvider
{
    public async Task<Stream> GetTemplateAsync(string templateName)
    {
        // 从数据库加载模板
        var templateContent = await LoadFromDatabaseAsync(templateName);
        return new MemoryStream(Encoding.UTF8.GetBytes(templateContent));
    }
    
    public async Task<bool> TemplateExistsAsync(string templateName)
    {
        return await CheckTemplateExistsInDatabaseAsync(templateName);
    }
    
    // 实现其他接口方法...
}

// 注册自定义提供者
builder.Services.AddFeishuDashboardTemplates(options =>
{
    options.UseCustomTemplateProvider<DatabaseTemplateProvider>();
});
```

### 完全自定义配置

```csharp
builder.Services.AddFeishuDashboardTemplates(options =>
{
    options.UseFileSystemTemplates("./my-templates")
           .AddVariable("Theme", "dark")
           .AddVariable("Language", "zh-CN")
           .AddVariable("Features", JsonSerializer.Serialize(new[] { "analytics", "notifications" }));
    
    options.CacheEnabled = true;
    options.CacheExpirationMinutes = 60;
    options.HotReloadEnabled = app.Environment.IsDevelopment();
});

app.MapFeishuDashboardV2(dashboardOptions =>
{
    dashboardOptions.PathPrefix = "/management";
    dashboardOptions.ApiPrefix = "/api/feishu";
    dashboardOptions.ManagePath = "/management/settings";
});
```

## 📁 模板文件结构

### 推荐的目录结构

```
templates/
├── dashboard.html           # 主控制面板 (必需)
├── manage.html             # 管理页面 (必需)  
├── css/
│   ├── base.css           # 基础样式
│   ├── components.css     # 组件样式
│   └── theme.css         # 主题样式
├── js/
│   ├── common.js         # 通用脚本
│   ├── dashboard.js      # 控制面板脚本
│   └── manage.js         # 管理页面脚本
├── images/
│   ├── logo.png          # Logo图片
│   └── icons/            # 图标文件
└── partials/
    ├── header.html       # 页面头部
    ├── footer.html       # 页面尾部
    └── sidebar.html      # 侧边栏
```

### 模板示例

**dashboard.html:**
```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="utf-8">
    <title>{{CompanyName}} - 飞书控制面板</title>
    <link rel="stylesheet" href="{{PathPrefix}}/css/base.css">
</head>
<body>
    <div class="container">
        <h1>欢迎使用 {{CompanyName}} 飞书管理系统</h1>
        <div class="version">版本: {{Version}}</div>
        
        <div class="actions">
            <a href="{{ManagePath}}" class="btn">进入管理</a>
            <a href="{{ApiPrefix}}/docs" class="btn">API文档</a>
        </div>
    </div>
    
    <script src="{{PathPrefix}}/js/dashboard.js"></script>
</body>
</html>
```

## 🔧 开发和调试

### 启用热重载

```csharp
if (app.Environment.IsDevelopment())
{
    builder.Services.AddFeishuDashboardTemplates(options =>
    {
        options.UseFileSystemTemplates("./templates")
               .EnableDevelopmentMode(); // 自动启用热重载和禁用缓存
    });
}
```

### 调试模板加载

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug); // 查看详细日志
});
```

### 清除模板缓存

```csharp
// 在需要时清除缓存
var templateService = app.Services.GetRequiredService<IFeishuDashboardTemplateService>();
await templateService.ClearCacheAsync();
```

## 📊 性能优化

### 生产环境配置

```csharp
builder.Services.AddFeishuDashboardTemplates(options =>
{
    options.UseEmbeddedTemplates()           // 使用嵌入资源
           .CacheEnabled = true              // 启用缓存
           .CacheExpirationMinutes = 120;    // 缓存2小时
});
```

### 自定义缓存策略

```csharp
public class RediTemplateCache : ITemplateCache
{
    // 实现Redis缓存
}

builder.Services.AddSingleton<ITemplateCache, RedisTemplateCache>();
```

## 🔄 迁移指南

### 从V1迁移到V2

1. **保持API兼容性**：所有API端点保持不变
2. **模板替换**：将硬编码HTML提取到模板文件
3. **渐进升级**：可以并行运行V1和V2

```csharp
// 旧版本
app.MapFeishuDashboard();

// 新版本
app.MapFeishuDashboardV2();

// 或同时支持（不同路径）
app.MapFeishuDashboard(); // /feishu
app.MapFeishuDashboardV2(options => {
    options.PathPrefix = "/feishu-v2";
});
```

## ❓ 常见问题

### Q: 如何添加自定义CSS样式？
A: 在模板文件中直接添加`<style>`标签，或者引用外部CSS文件。

### Q: 模板变量不生效？
A: 确保使用正确的格式 `{{变量名}}`，并且变量已通过配置添加。

### Q: 如何调试模板加载问题？
A: 启用Debug级别日志，查看模板加载的详细信息。

### Q: 生产环境推荐哪种模板提供者？
A: 推荐使用 `EmbeddedResourceTemplateProvider`，性能最佳且无外部依赖。

---

## 🎉 总结

Dashboard V2 提供了强大而灵活的模板系统，让您可以：

✅ **完全自定义UI界面**  
✅ **保持API向后兼容**  
✅ **支持多种模板来源**  
✅ **优化性能和缓存**  
✅ **简化开发和部署**  

立即升级到V2，体验更强大的自定义能力！