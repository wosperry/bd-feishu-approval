using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BD.FeishuApproval.Abstractions.Definitions;
using BD.FeishuApproval.Abstractions.Http;
using BD.FeishuApproval.Shared.Models;

namespace BD.FeishuApproval.Definitions;

/// <summary>
/// 飞书审批订阅服务实现
/// </summary>
public class FeishuApprovalSubscriptionService : IFeishuApprovalSubscriptionService
{
    private readonly IFeishuApiClient _client;

    /// <summary>
    /// 初始化飞书审批订阅服务
    /// </summary>
    /// <param name="client">飞书API客户端</param>
    public FeishuApprovalSubscriptionService(IFeishuApiClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<FeishuResponse> SubscribeApprovalAsync(string approvalCode)
    {
        using var http = new HttpRequestMessage(HttpMethod.Post, $"/open-apis/approval/v4/approvals/{approvalCode}/subscribe");
        var resp = await _client.SendAsync(http);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FeishuResponse>(json) ?? new FeishuResponse();
        if (!result.IsSuccess)
        {
            throw new FeishuApiException($"Feishu API error: {result.Message}", result.Code, $"/open-apis/approval/v4/approvals/{approvalCode}/subscribe");
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<FeishuResponse> UnsubscribeApprovalAsync(string approvalCode)
    {
        using var http = new HttpRequestMessage(HttpMethod.Post, $"/open-apis/approval/v4/approvals/{approvalCode}/unsubscribe");
        var resp = await _client.SendAsync(http);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FeishuResponse>(json) ?? new FeishuResponse();
        if (!result.IsSuccess)
        {
            throw new FeishuApiException($"Feishu API error: {result.Message}", result.Code, $"/open-apis/approval/v4/approvals/{approvalCode}/unsubscribe");
        }
        return result;
    }
}