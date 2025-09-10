using BD.FeishuApproval.Abstractions.Handlers;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Handlers;
using BD.FeishuApproval.Shared.Dtos.Instances;

namespace FeishuApproval.SampleWeb.FeishuApprovals.DemoApproval;

/// <summary>
/// Demo审批处理器
/// 这个例子项目里真正注入的Handler 另一个DemoApprovalHandler根本没有注入
/// </summary>
public class DemoApprovalHandlerImplement(
    IFeishuApprovalInstanceService instanceService,
    ILogger<DemoApprovalHandlerImplement> logger)
    : ApprovalHandlerBase<DemoApprovalDto>(instanceService, logger)
{
    /// <summary>
    /// 审批通过处理
    /// </summary>
    public override async Task HandleApprovalApprovedAsync(ApprovalContext<DemoApprovalDto> context)
    {
        var request = context.Data;
        var callback = context.Callback;

        _logger.LogInformation("🎉 Demo审批已通过! - 申请人: {Name}, 年龄: {Age}, 实例: {InstanceCode}",
            request.姓名, request.年龄_岁, callback.Event.ApprovalCode);

        try
        {
            // 记录审批通过事件
            await LogApprovalEventAsync("APPROVED", request, callback);

            // 发送通知（这里只是日志记录，实际可以发送邮件、短信等）
            _logger.LogInformation("✅ 已发送审批通过通知 - 申请人: {Name}", request.姓名);

            // 根据申请人年龄做不同的处理
            if (request.年龄_岁 >= 18)
            {
                _logger.LogInformation("💼 成年人申请，正常处理流程");
                await HandleAdultApprovalAsync(request, callback);
            }
            else
            {
                _logger.LogInformation("👶 未成年人申请，需要特殊关注");
                await HandleMinorApprovalAsync(request, callback);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理审批通过事件时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 审批拒绝处理
    /// </summary>
    public override async Task HandleApprovalRejectedAsync(ApprovalContext<DemoApprovalDto> context)
    {
        var request = context.Data;
        var callback = context.Callback;

        _logger.LogInformation("❌ Demo审批已拒绝 - 申请人: {Name}, 年龄: {Age}, 实例: {InstanceCode}",
            request.姓名, request.年龄_岁, callback.Event.ApprovalCode);

        try
        {
            // 记录审批拒绝事件
            await LogApprovalEventAsync("REJECTED", request, callback);

            // 发送拒绝通知
            _logger.LogInformation("📧 已发送审批拒绝通知 - 申请人: {Name}", request.姓名);

            // 处理拒绝后的清理工作
            await HandleRejectionCleanupAsync(request, callback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理审批拒绝事件时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 审批撤回处理
    /// </summary>
    public override async Task HandleApprovalCancelledAsync(ApprovalContext<DemoApprovalDto> context)
    {
        var request = context.Data;
        var callback = context.Callback;

        _logger.LogInformation("🔄 Demo审批已撤回 - 申请人: {Name}, 年龄: {Age}, 实例: {InstanceCode}",
            request.姓名, request.年龄_岁, callback.Event.ApprovalCode);

        try
        {
            // 记录审批撤回事件
            await LogApprovalEventAsync("CANCELLED", request, callback);

            // 处理撤回逻辑
            _logger.LogInformation("🔙 已处理审批撤回 - 申请人: {Name}", request.姓名);

            // 撤回可能需要回滚某些已经执行的操作
            await HandleCancellationAsync(request, callback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理审批撤回事件时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 处理未知状态
    /// </summary>
    public override async Task HandleUnknownStatusAsync(ApprovalContext<DemoApprovalDto> context)
    {
        var request = context.Data;
        var callback = context.Callback;

        _logger.LogWarning("⚠️ 收到未知的审批状态 - 申请人: {Name}, 状态: {Status}, 实例: {InstanceCode}",
            request.姓名, callback.Event.EventAction, callback.Event.ApprovalCode);

        try
        {
            // 记录未知状态事件，用于后续分析
            await LogApprovalEventAsync($"UNKNOWN_{callback.Type}", request, callback);

            // 可以选择是否需要人工介入处理
            _logger.LogWarning("🔍 未知状态事件已记录，可能需要人工处理");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理未知状态事件时发生错误");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 业务异常处理
    /// </summary>
    public override async Task HandleBusinessExceptionAsync(ApprovalContext<DemoApprovalDto> context, Exception exception)
    {
        var request = context.Data;
        var callback = context.Callback;

        _logger.LogError(exception, "💥 Demo审批业务处理异常 - 申请人: {Name}, 实例: {InstanceCode}",
            request.姓名, callback.Event.ApprovalCode);

        try
        {
            // 记录异常信息
            await LogApprovalEventAsync($"ERROR_{callback.Type}", request, callback, exception.Message);

            // 可以选择是否发送错误通知
            _logger.LogError("📝 审批处理异常，已记录错误信息 - 异常: {Error}", exception.Message);

            // 根据异常类型决定是否需要重试或人工干预
            if (ShouldRetry(exception))
            {
                _logger.LogInformation("🔄 异常类型支持重试，建议稍后重试处理");
            }
            else
            {
                _logger.LogError("🆘 严重异常，建议人工介入处理");
            }
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "❌ 记录业务异常时发生错误");
        }

        await Task.CompletedTask;
    }

    #region 可选重写的钩子方法

    /// <summary>
    /// 验证审批请求
    /// </summary>
    protected override async Task ValidateApprovalRequestAsync(DemoApprovalDto request)
    {
        // 自定义验证逻辑
        if (string.IsNullOrWhiteSpace(request.姓名))
            throw new ArgumentException("姓名不能为空");

        if (request.年龄_岁 <= 0)
            throw new ArgumentException("年龄必须大于0");

        if (request.年龄_岁 > 120)
            throw new ArgumentException("年龄不能超过120岁");

        _logger.LogDebug("✅ Demo审批请求验证通过 - 申请人: {Name}, 年龄: {Age}", 
            request.姓名, request.年龄_岁);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 预处理审批请求
    /// </summary>
    protected override async Task PreProcessApprovalAsync(DemoApprovalDto request)
    {
        // 预处理逻辑，例如数据清洗、格式化等
        request.姓名 = request.姓名?.Trim() ?? string.Empty;
        
        // 确保年龄在合理范围内
        if (request.年龄_岁 < 0)
        {
            request.年龄_岁 = 0;
        }

        _logger.LogDebug("🔧 Demo审批请求预处理完成 - 姓名: {Name}, 年龄: {Age}",
            request.姓名, request.年龄_岁);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 后处理审批结果
    /// </summary>
    protected override async Task PostProcessApprovalAsync(DemoApprovalDto request, CreateInstanceResult result)
    {
        if (!string.IsNullOrEmpty(result.InstanceCode))
        {
            _logger.LogInformation("🎯 Demo审批创建成功 - 实例代码: {InstanceCode}, 申请人: {Name}", 
                result.InstanceCode, request.姓名);

            // 可以在这里发送创建成功的通知
            // 或者更新相关业务数据
        }
        else
        {
            _logger.LogError("💔 Demo审批创建失败 - 申请人: {Name}", 
                request.姓名);
        }

        await Task.CompletedTask;
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 记录审批事件
    /// </summary>
    private async Task LogApprovalEventAsync(string eventType, DemoApprovalDto request, 
        FeishuCallbackEvent callback, string errorMessage = null)
    {
        // 这里可以将事件记录到数据库、文件或其他持久化存储中
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = eventType,
            InstanceCode = callback.Event.ApprovalCode,
            callback.Event.ApprovalCode,
            Name = request.姓名,
            Age = request.年龄_岁,
            ErrorMessage = errorMessage
        };

        _logger.LogInformation("📊 审批事件记录: {@LogEntry}", logEntry);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理成年人审批
    /// </summary>
    private async Task HandleAdultApprovalAsync(DemoApprovalDto request, 
        FeishuCallbackEvent callback)
    {
        // 成年人审批的特殊处理逻辑
        _logger.LogInformation("👨‍💼 执行成年人审批后处理逻辑 - 姓名: {Name}, 年龄: {Age}", 
            request.姓名, request.年龄_岁);
        
        // 例如：
        // 1. 发送正式通知
        // 2. 更新业务系统状态
        // 3. 触发后续流程
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理未成年人审批
    /// </summary>
    private async Task HandleMinorApprovalAsync(DemoApprovalDto request, 
        FeishuCallbackEvent callback)
    {
        // 未成年人审批的特殊处理逻辑
        _logger.LogInformation("👶 执行未成年人审批后处理逻辑 - 姓名: {Name}, 年龄: {Age}", 
            request.姓名, request.年龄_岁);
        
        // 例如：
        // 1. 发送特殊通知给监护人
        // 2. 记录到特殊审计日志
        // 3. 触发额外的合规检查
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理审批拒绝后的清理工作
    /// </summary>
    private async Task HandleRejectionCleanupAsync(DemoApprovalDto request, 
        FeishuCallbackEvent callback)
    {
        // 拒绝后的清理逻辑
        _logger.LogDebug("🧹 执行审批拒绝清理逻辑 - 申请人: {Name}", request.姓名);
        
        // 例如：
        // 1. 释放已预留的资源
        // 2. 回滚临时状态变更
        // 3. 清理缓存数据
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理审批撤回
    /// </summary>
    private async Task HandleCancellationAsync(DemoApprovalDto request, 
        FeishuCallbackEvent callback)
    {
        // 撤回处理逻辑
        _logger.LogDebug("↩️ 执行审批撤回处理逻辑 - 申请人: {Name}", request.姓名);
        
        // 例如：
        // 1. 回滚已执行的业务操作
        // 2. 恢复之前的状态
        // 3. 更新相关记录
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 判断异常是否可以重试
    /// </summary>
    private static bool ShouldRetry(Exception exception)
    {
        // 根据异常类型判断是否可以重试
        return exception is TimeoutException || 
               exception is HttpRequestException ||
               exception is InvalidOperationException && exception.Message.Contains("temporary");
    }

    #endregion
}