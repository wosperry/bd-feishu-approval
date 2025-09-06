# 飞书Dashboard重构计划

## 🎯 目标
将硬编码的HTML页面重构为基于文件流的模板系统，支持第三方自定义，类似Swagger UI的设计模式。

## 📋 主要任务

### Phase 1: 架构设计与基础设施 🏗️
- [x] 1.1 设计Dashboard模板配置系统
- [x] 1.2 创建HTML模板文件结构
- [x] 1.3 实现模板流加载机制
- [x] 1.4 设计第三方自定义接口

### Phase 2: 模板文件创建 📄
- [x] 2.1 创建主Dashboard HTML模板
- [x] 2.2 创建管理页面HTML模板  
- [ ] 2.3 提取CSS样式到独立文件
- [ ] 2.4 提取JavaScript到独立文件
- [x] 2.5 创建配置帮助指导模板

### Phase 3: 配置系统实现 ⚙️
- [x] 3.1 实现FeishuDashboardTemplateOptions配置类
- [x] 3.2 实现模板流提供者接口
- [x] 3.3 实现默认文件系统模板提供者
- [x] 3.4 实现内嵌资源模板提供者
- [x] 3.5 实现第三方自定义模板支持

### Phase 4: 核心重构 🔄
- [x] 4.1 重构FeishuDashboardEndpoint类
- [x] 4.2 实现模板渲染引擎
- [x] 4.3 实现模板变量替换系统
- [x] 4.4 保持API接口向后兼容

### Phase 5: 增强功能 ✨
- [ ] 5.1 添加模板缓存机制
- [ ] 5.2 实现热重载支持
- [ ] 5.3 添加模板验证
- [ ] 5.4 实现多语言支持基础

### Phase 6: 文档与测试 📚
- [ ] 6.1 创建使用文档
- [ ] 6.2 创建自定义示例
- [ ] 6.3 添加单元测试
- [ ] 6.4 更新README说明

## 🏛️ 架构设计

### 核心组件
```
IFeishuDashboardTemplateProvider (接口)
├── FileSystemTemplateProvider (文件系统)
├── EmbeddedResourceTemplateProvider (内嵌资源)  
└── CustomTemplateProvider (第三方自定义)

FeishuDashboardTemplateOptions (配置)
├── TemplateProvider (模板提供者)
├── CacheEnabled (缓存开关)
├── CustomVariables (自定义变量)
└── HotReloadEnabled (热重载)
```

### 模板文件结构
```
Templates/
├── dashboard.html (主控制面板)
├── manage.html (管理页面)
├── css/
│   ├── dashboard.css
│   └── help.css
├── js/
│   ├── dashboard.js
│   └── help.js
└── partials/
    ├── config-help.html
    ├── approval-help.html
    └── header.html
```

## 🚀 预期收益

1. **灵活性**: 第三方可以完全自定义UI
2. **可维护性**: HTML/CSS/JS分离，便于维护
3. **扩展性**: 支持模板变量和部分模板
4. **性能**: 模板缓存和热重载
5. **兼容性**: 保持现有API不变

## 🎨 使用场景

### 默认使用
```csharp
builder.Services.AddFeishuDashboard(); // 使用默认模板
```

### 文件系统自定义
```csharp
builder.Services.AddFeishuDashboard(options => {
    options.UseFileSystemTemplates("./custom-templates");
});
```

### 完全自定义
```csharp
builder.Services.AddFeishuDashboard(options => {
    options.UseCustomTemplateProvider<MyTemplateProvider>();
});
```

---
*Created: 2025-01-04*  
*Status: 🚧 In Progress*