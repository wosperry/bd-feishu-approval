using BD.FeishuApproval.Shared.Contexts;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Strategies;

/// <summary>
/// 审批策略接口，定义特定类型审批的完整执行流程
/// </summary>
public interface IApprovalStrategy
{
    /// <summary>
    /// 执行审批流程
    /// </summary>
    /// <param name="context">审批上下文</param>
    Task ExecuteAsync(ApprovalContext context);

    /// <summary>
    /// 支持的审批类型（如"leave"、"purchase"）
    /// </summary>
    string ApprovalType { get; }
}

/// <summary>
/// 审批策略工厂接口，用于创建策略实例
/// </summary>
public interface IApprovalStrategyFactory
{
    /// <summary>
    /// 根据审批类型获取对应的策略
    /// </summary>
    /// <param name="approvalType">审批类型标识</param>
    /// <returns>策略实例</returns>
    IApprovalStrategy GetStrategy(string approvalType);
}
