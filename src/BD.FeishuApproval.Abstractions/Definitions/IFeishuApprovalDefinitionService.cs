using BD.FeishuApproval.Shared.Abstractions;
using BD.FeishuApproval.Shared.Dtos.Definitions;
using BD.FeishuApproval.Shared.Models;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Definitions;

/// <summary>
/// 飞书审批定义服务接口，负责审批模板的管理
/// </summary>
public interface IFeishuApprovalDefinitionService
{
    /// <summary>
    /// 创建飞书第三方审批定义（模板）
    /// </summary>
    /// <param name="request">审批定义创建请求</param>
    /// <returns>创建结果（含approval_code）</returns>
    Task<FeishuResponse<CreateDefinitionResult>> CreateDefinitionAsync(CreateDefinitionRequest request);

    /// <summary>
    /// 查询审批定义详情
    /// </summary>
    /// <param name="approvalCode">审批定义标识</param>
    /// <returns>审批定义详情</returns>
    Task<FeishuResponse<ApprovalDefinitionDetail>> GetDefinitionDetailAsync(string approvalCode);

    /// <summary>
    /// 启用/停用审批定义
    /// </summary>
    /// <param name="approvalCode">审批定义标识</param>
    /// <param name="isEnabled">是否启用</param>
    /// <returns>操作结果</returns>
    Task<FeishuResponse> SetDefinitionStatusAsync(string approvalCode, bool isEnabled);

    /// <summary>
    /// 订阅审批回调通知
    /// </summary>
    /// <param name="approvalCode">审批定义标识</param>
    /// <returns>订阅结果</returns>
    Task<FeishuResponse> SubscribeApprovalAsync(string approvalCode);

    /// <summary>
    /// 取消订阅审批回调通知
    /// </summary>
    /// <param name="approvalCode">审批定义标识</param>
    /// <returns>取消订阅结果</returns>
    Task<FeishuResponse> UnsubscribeApprovalAsync(string approvalCode);

    /// <summary>
    /// 查询审批订阅状态
    /// </summary>
    /// <param name="approvalCode">审批定义标识</param>
    /// <returns>审批定义详情（含订阅状态）</returns>
    Task<FeishuResponse<ApprovalDefinitionDetail>> GetApprovalSubscriptionStatusAsync(string approvalCode);
    Task<FeishuCreateApprovalBody> CreateFeishuApprovalRequestBody<T>(string userOpenId, T request) where T : class, IFeishuApprovalRequest, new();
}
