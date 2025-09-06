using BD.FeishuApproval.Shared.Events;
using BD.FeishuApproval.Shared.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Callbacks;

/// <summary>
/// 飞书回调处理接口，负责解析和处理飞书推送的审批事件
/// </summary>
public interface IFeishuCallbackHandler
{
    /// <summary>
    /// 验证并解析飞书回调请求
    /// </summary>
    /// <param name="httpContext">HTTP请求上下文</param>
    /// <returns>解析后的回调事件</returns>
    Task<FeishuCallbackEvent> ParseCallbackAsync(HttpContext httpContext);

    /// <summary>
    /// 处理审批状态变更事件
    /// </summary>
    /// <param name="event">状态变更事件</param>
    /// <returns>处理结果</returns>
    Task<FeishuResponse> HandleApprovalStatusChangedAsync(ApprovalStatusChangedEvent @event);
}
