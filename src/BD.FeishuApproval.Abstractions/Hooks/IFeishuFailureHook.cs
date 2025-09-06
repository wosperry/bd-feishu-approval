using System.Threading;
using System.Threading.Tasks;
using BD.FeishuApproval.Shared.Models;

namespace BD.FeishuApproval.Abstractions.Hooks;

/// <summary>
/// 失败钩子。
/// 用户可实现以进行短信/钉钉/企业微信/自定义告警。
/// </summary>
public interface IFeishuFailureHook
{
    Task OnApiFailureAsync(FeishuRequestContext context, FeishuResponse response, string errorMessage, CancellationToken cancellationToken = default);
}


