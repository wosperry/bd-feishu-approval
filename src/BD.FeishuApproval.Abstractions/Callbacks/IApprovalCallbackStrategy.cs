using BD.FeishuApproval.Shared.Events;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Callbacks;

/// <summary>
/// 审批回调处理策略接口
/// 定义特定类型审批的回调处理逻辑
/// </summary>
public interface IApprovalCallbackStrategy
{
    /// <summary>
    /// 支持的审批类型标识
    /// </summary>
    string ApprovalType { get; }

    /// <summary>
    /// 处理审批状态变更回调
    /// </summary>
    /// <param name="callbackEvent">回调事件数据</param>
    /// <returns>处理任务</returns>
    Task HandleApprovalStatusChangedAsync(FeishuCallbackEvent callbackEvent);

    /// <summary>
    /// 验证回调数据是否适用于此策略
    /// </summary>
    /// <param name="callbackEvent">回调事件数据</param>
    /// <returns>是否适用</returns>
    bool CanHandle(FeishuCallbackEvent callbackEvent);
}

/// <summary>
/// 审批回调策略工厂接口
/// </summary>
public interface IApprovalCallbackStrategyFactory
{
    /// <summary>
    /// 根据审批类型获取对应的回调处理策略
    /// </summary>
    /// <param name="approvalType">审批类型标识</param>
    /// <returns>策略实例，如果未找到则返回null</returns>
    IApprovalCallbackStrategy? GetStrategy(string approvalType);

    /// <summary>
    /// 根据回调事件自动匹配最适合的策略
    /// </summary>
    /// <param name="callbackEvent">回调事件数据</param>
    /// <returns>策略实例，如果未找到则返回null</returns>
    IApprovalCallbackStrategy? GetStrategy(FeishuCallbackEvent callbackEvent);
}