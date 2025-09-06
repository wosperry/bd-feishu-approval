using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Http;

/// <summary>
/// 飞书开放平台统一请求客户端抽象。
/// 负责：
/// - 统一携带 tenant_access_token
/// - 重试与限流处理（实现层）
/// - 请求与响应日志的钩子调用
/// </summary>
public interface IFeishuApiClient
{
    /// <summary>
    /// 发送 HTTP 请求（已自动注入鉴权）。
    /// </summary>
    /// <param name="request">HttpRequestMessage</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>HttpResponseMessage</returns>
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 基础地址（形如 https://open.feishu.cn ）。
    /// </summary>
    Uri BaseAddress { get; }
}


