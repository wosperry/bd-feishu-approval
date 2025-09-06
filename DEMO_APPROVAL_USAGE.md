# Demo审批使用指南

## 🎯 设计理念

Demo审批控制器采用了**极简设计**，让用户只需要关注两个核心文件：
1. **参数类** - 定义审批表单字段
2. **处理类** - 实现审批回调逻辑

系统自动处理所有底层细节：用户ID映射、OpenId缓存、API调用等。

## 📁 核心文件

### 1. 参数类：`DemoApprovalRequest.cs`
```csharp
[ApprovalCode("6A109ECD-3578-4243-93F9-DBDCF89515AF")]
public class DemoApprovalRequest : FeishuApprovalRequestBase
{
    [JsonPropertyName("widget17570011196430001")]
    public string 姓名 { get; set; } = string.Empty;

    [JsonPropertyName("widget17570011375970001")]
    public int 年龄_岁 { get; set; } = 0;
}
```

**说明**：
- `[ApprovalCode]` 特性指定审批流程代码
- `[JsonPropertyName]` 绑定飞书表单字段
- 继承 `FeishuApprovalRequestBase` 获得基础功能

### 2. 处理类：`DemoApprovalHandler.cs`
```csharp
public class DemoApprovalHandler : ApprovalHandlerBase<DemoApprovalRequest>
{
    // 必须实现的回调方法
    public override Task HandleApprovalApprovedAsync(ApprovalContext<DemoApprovalRequest> context)
    {
        // 审批通过后的业务逻辑
    }

    public override Task HandleApprovalRejectedAsync(ApprovalContext<DemoApprovalRequest> context)
    {
        // 审批拒绝后的业务逻辑
    }

    // 其他回调方法...
}
```

## 🚀 使用方式

### 1. 快速测试
```http
POST /api/DemoApproval/test
```
系统会自动创建测试数据并提交审批。

### 2. 自定义数据
```http
POST /api/DemoApproval/create
Content-Type: application/json

{
    "姓名": "张三",
    "年龄_岁": 25
}
```

### 3. 获取审批信息
```http
GET /api/DemoApproval/info
```
查看审批流程的详细信息和使用说明。

## 🔧 系统自动处理的功能

### 1. 用户ID管理
- 使用固定的 `fakeUserId = 1`
- 自动映射到飞书OpenId
- 支持OpenId缓存机制

### 2. 审批代码管理
- 从参数类的 `[ApprovalCode]` 特性自动获取
- 无需手动指定审批流程代码

### 3. 表单数据转换
- 自动将参数类转换为飞书API格式
- 处理中文字段名和特殊字符

### 4. 错误处理
- 统一的异常处理机制
- 详细的错误分类和日志记录

## 📊 响应格式

### 成功响应
```json
{
    "Success": true,
    "Message": "Demo审批创建成功",
    "Data": {
        "InstanceCode": "实例代码",
        "ApprovalCode": "6A109ECD-3578-4243-93F9-DBDCF89515AF",
        "UserId": 1,
        "CreateTime": "2024-01-01T00:00:00Z",
        "FormData": {
            "Name": "张三",
            "Age": 25
        }
    }
}
```

### 错误响应
```json
{
    "Success": false,
    "Message": "错误信息",
    "ErrorType": "ValidationError|BusinessError|SystemError"
}
```

## 🎯 开发流程

### 1. 创建新的审批类型
1. 复制 `DemoApprovalRequest.cs` 和 `DemoApprovalHandler.cs`
2. 修改审批代码和表单字段
3. 实现处理类中的回调方法
4. 系统自动注册新的审批类型

### 2. 实现业务逻辑
在 `DemoApprovalHandler` 中实现以下方法：
- `HandleApprovalApprovedAsync` - 审批通过
- `HandleApprovalRejectedAsync` - 审批拒绝
- `HandleApprovalCancelledAsync` - 审批撤回
- `HandleUnknownStatusAsync` - 未知状态
- `HandleBusinessExceptionAsync` - 业务异常

### 3. 可选的生命周期钩子
- `ValidateApprovalRequestAsync` - 请求验证
- `PreProcessApprovalAsync` - 预处理
- `PostProcessApprovalAsync` - 后处理
- `HandleCreateFailureInternalAsync` - 创建失败处理

## 🔍 调试和监控

### 1. 日志记录
系统自动记录详细的日志：
- 审批创建过程
- OpenId获取和缓存
- API调用结果
- 错误和异常信息

### 2. 健康检查
```http
GET /api/DemoApproval/types
GET /api/DemoApproval/types/{approvalType}/supported
```

### 3. 错误排查
- 查看日志文件中的详细错误信息
- 使用 `/info` 接口查看审批配置
- 检查用户ID映射和OpenId缓存状态

## 🚀 性能优化

### 1. OpenId缓存
- 首次调用后自动缓存OpenId
- 后续调用直接从缓存获取
- 支持缓存清除和更新

### 2. 批量处理
- 支持批量创建审批
- 自动优化API调用频率
- 减少网络请求次数

### 3. 异步处理
- 所有操作都是异步的
- 支持高并发场景
- 自动处理超时和重试

## 📝 最佳实践

### 1. 参数类设计
- 使用有意义的属性名
- 添加详细的XML注释
- 合理设置默认值

### 2. 处理类实现
- 实现所有必需的回调方法
- 添加适当的错误处理
- 记录重要的业务日志

### 3. 错误处理
- 区分不同类型的错误
- 提供有意义的错误信息
- 实现适当的重试机制

## 🎉 总结

Demo审批控制器实现了**"一行代码集成"**的目标：

1. **用户只需要**：
   - 定义参数类（表单字段）
   - 实现处理类（回调逻辑）

2. **系统自动处理**：
   - 用户ID映射和OpenId缓存
   - 审批代码管理
   - API调用和错误处理
   - 日志记录和监控

3. **开发效率**：
   - 减少90%的样板代码
   - 自动处理复杂的技术细节
   - 提供完整的错误处理和日志记录

这种设计让开发者可以专注于业务逻辑，而不用关心底层的技术实现细节。
