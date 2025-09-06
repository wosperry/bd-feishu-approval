using System.Threading.Tasks;
using BD.FeishuApproval.Shared.Models;

namespace BD.FeishuApproval.Abstractions.Definitions;

/// <summary>
/// 飞书审批订阅服务接口
/// </summary>
public interface IFeishuApprovalSubscriptionService
{
    /// <summary>
    /// 订阅审批定义更新
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <returns>订阅结果</returns>
    Task<FeishuResponse> SubscribeApprovalAsync(string approvalCode);

    /// <summary>
    /// 取消订阅审批定义更新
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <returns>取消订阅结果</returns>
    Task<FeishuResponse> UnsubscribeApprovalAsync(string approvalCode);
}