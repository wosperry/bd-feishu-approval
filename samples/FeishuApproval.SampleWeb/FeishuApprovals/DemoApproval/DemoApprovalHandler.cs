using BD.FeishuApproval.Abstractions.Handlers;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Handlers;

namespace FeishuApproval.SampleWeb.FeishuApprovals.DemoApproval;

/// <summary>
/// Demo 审批处理器
/// 
/// 正确的职责划分：
/// - Handler 负责回调处理 + 提供校验能力（给 ApprovalService 调用）
/// - ApprovalService 负责实际的创建审批操作
/// - 第三方开发者继承此基类，实现回调方法 + 可选的校验逻辑
/// </summary>
public class DemoApprovalHandler(
    IFeishuApprovalInstanceService instanceService,
    ILogger<DemoApprovalHandler> logger)
    : ApprovalHandlerBase<DemoApprovalDto>(instanceService, logger)
{
    #region ===== 必须实现的回调处理方法 =====

    /// <summary>
    /// 审批通过后的处理逻辑
    /// 
    /// 实现建议：
    /// 1. 更新业务数据状态为"已通过"
    /// 2. 发送通知给申请人和相关人员
    /// 3. 调用其他系统API（如资源分配、权限开通等）
    /// 4. 记录审计日志
    /// 5. 触发后续业务流程
    /// 
    /// 示例代码：
    /// await _businessService.UpdateStatusAsync(context.Data, "Approved");
    /// await _notificationService.SendApprovedNotificationAsync(context.Data);
    /// await _auditService.LogAsync("ApprovalApproved", context.Data);
    /// </summary>
    public override Task HandleApprovalApprovedAsync(ApprovalContext<DemoApprovalDto> context)
    {
        throw new NotImplementedException("请实现审批通过后的业务逻辑处理");
    }

    /// <summary>
    /// 审批拒绝后的处理逻辑
    /// 
    /// 实现建议：
    /// 1. 更新业务数据状态为"已拒绝"
    /// 2. 记录拒绝原因（context.Callback.Comment）
    /// 3. 回滚相关预处理操作
    /// 4. 发送拒绝通知给申请人
    /// 5. 清理临时分配的资源
    /// 
    /// 示例代码：
    /// await _businessService.UpdateStatusAsync(context.Data, "Rejected", context.Callback.Comment);
    /// await _businessService.RollbackAsync(context.Data);
    /// await _notificationService.SendRejectedNotificationAsync(context.Data, context.Callback.Comment);
    /// </summary>
    public override Task HandleApprovalRejectedAsync(ApprovalContext<DemoApprovalDto> context)
    {
        throw new NotImplementedException("请实现审批拒绝后的业务逻辑处理");
    }

    /// <summary>
    /// 审批撤回后的处理逻辑
    /// 
    /// 实现建议：
    /// 1. 更新业务数据状态为"已撤回"
    /// 2. 清理临时数据和预分配的资源
    /// 3. 通知申请人撤回成功
    /// 4. 记录撤回操作的审计日志
    /// 5. 如有必要，触发补偿机制
    /// 
    /// 示例代码：
    /// await _businessService.UpdateStatusAsync(context.Data, "Cancelled");
    /// await _resourceService.ReleasePreAllocatedAsync(context.Data);
    /// await _notificationService.SendCancelledNotificationAsync(context.Data);
    /// </summary>
    public override Task HandleApprovalCancelledAsync(ApprovalContext<DemoApprovalDto> context)
    {
        throw new NotImplementedException("请实现审批撤回后的业务逻辑处理");
    }

    /// <summary>
    /// 审批状态未知时的处理逻辑
    /// 
    /// 实现建议：
    /// 1. 记录详细的告警日志，包含所有回调信息
    /// 2. 发送告警通知给系统管理员
    /// 3. 将事件记录到失败任务表，等待人工处理
    /// 4. 避免执行不可逆的业务操作
    /// 5. 考虑实现重试机制或状态查询
    /// 
    /// 示例代码：
    /// _logger.LogWarning("收到未知审批状态: {Status}, 实例: {InstanceCode}", context.Callback.Type, context.Callback.InstanceCode);
    /// await _alertService.SendUnknownStatusAlertAsync(context);
    /// await _failedJobService.AddAsync(context, "Unknown approval status received");
    /// </summary>
    public override Task HandleUnknownStatusAsync(ApprovalContext<DemoApprovalDto> context)
    {
        throw new NotImplementedException("请实现未知状态的处理逻辑");
    }

    /// <summary>
    /// 业务异常处理逻辑
    /// 
    /// 实现建议：
    /// 1. 详细记录异常信息和上下文数据
    /// 2. 发送异常告警给开发和运维团队
    /// 3. 将失败的任务记录到补偿队列
    /// 4. 考虑是否需要回滚已执行的操作
    /// 5. 提供人工介入的补偿机制
    /// 
    /// 示例代码：
    /// _logger.LogError(exception, "审批业务处理异常: {Data}", JsonSerializer.Serialize(context.Data));
    /// await _alertService.SendExceptionAlertAsync(context, exception);
    /// await _failedJobService.AddAsync(context, exception);
    /// </summary>
    public override Task HandleBusinessExceptionAsync(ApprovalContext<DemoApprovalDto> context, Exception exception)
    {
        throw new NotImplementedException("请实现业务异常处理逻辑");
    }

    #endregion

    #region ===== 可选的校验和生命周期钩子 =====

    /// <summary>
    /// 审批请求验证逻辑（可选重写）
    /// 
    /// 实现建议：
    /// 1. 验证必填字段
    /// 2. 验证数据格式和范围
    /// 3. 执行业务规则校验
    /// 4. 检查用户权限和状态
    /// 5. 验证关联数据的有效性
    /// 
    /// 示例代码：
    /// if (string.IsNullOrEmpty(request.姓名))
    ///     throw new ArgumentException("姓名不能为空");
    /// if (await _userService.IsBlacklistedAsync(request.UserId))
    ///     throw new InvalidOperationException("用户已被列入黑名单");
    /// </summary>
    protected override async Task ValidateApprovalRequestAsync(DemoApprovalDto request)
    {
        // 在这里实现自定义的验证逻辑
        // 如果验证失败，抛出相应的异常
        await Task.CompletedTask;
    }

    /// <summary>
    /// 审批创建前预处理逻辑（可选重写）
    /// 
    /// 实现建议：
    /// 1. 预分配必要的资源
    /// 2. 通知相关人员审批开始
    /// 3. 准备审批所需的补充数据
    /// 4. 执行前置业务逻辑
    /// 5. 更新关联数据状态
    /// 
    /// 示例代码：
    /// await _resourceService.PreAllocateAsync(request);
    /// await _notificationService.NotifyApproversAsync(request);
    /// request.PrepareAdditionalData();
    /// </summary>
    protected override async Task PreProcessApprovalAsync(DemoApprovalDto request)
    {
        // 在这里实现创建审批前的预处理逻辑
        await Task.CompletedTask;
    }

    /// <summary>
    /// 审批创建成功后处理逻辑（可选重写）
    /// 
    /// 实现建议：
    /// 1. 更新业务数据状态为"审批中"
    /// 2. 保存审批实例ID到业务数据
    /// 3. 发送提交成功通知
    /// 4. 记录创建操作的审计日志
    /// 5. 触发相关的业务流程
    /// 
    /// 示例代码：
    /// await _businessService.UpdateStatusAsync(request, "InApproval", result.InstanceCode);
    /// await _notificationService.SendSubmissionSuccessAsync(request, result);
    /// await _auditService.LogCreationAsync(request, result);
    /// </summary>
    protected override async Task PostProcessApprovalAsync(DemoApprovalDto request, BD.FeishuApproval.Shared.Dtos.Instances.CreateInstanceResult result)
    {
        // 在这里实现审批创建成功后的处理逻辑
        await Task.CompletedTask;
    }

    /// <summary>
    /// 审批创建失败处理逻辑（可选重写）
    /// 
    /// 实现建议：
    /// 1. 回滚预处理阶段的操作
    /// 2. 释放预分配的资源
    /// 3. 发送创建失败通知
    /// 4. 记录失败日志和异常信息
    /// 5. 考虑是否需要重试机制
    /// 
    /// 示例代码：
    /// await _resourceService.RollbackPreAllocationAsync(request);
    /// await _notificationService.SendCreationFailedAsync(request, exception);
    /// _logger.LogError(exception, "审批创建失败: {Request}", JsonSerializer.Serialize(request));
    /// </summary>
    protected override async Task HandleCreateFailureInternalAsync(DemoApprovalDto request, Exception exception)
    {
        // 在这里实现审批创建失败时的处理逻辑
        await Task.CompletedTask;
    }

    #endregion
}

