using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BD.FeishuApproval.Abstractions.Definitions;
using BD.FeishuApproval.Abstractions.Http;
using BD.FeishuApproval.Shared.Dtos.Definitions;
using BD.FeishuApproval.Shared.Models;

namespace BD.FeishuApproval.Definitions;

/// <summary>
/// 飞书审批定义服务实现
/// </summary>
public class FeishuApprovalDefinitionService : IFeishuApprovalDefinitionService
{
    private readonly IFeishuApiClient _client;

    /// <summary>
    /// 初始化飞书审批定义服务
    /// </summary>
    /// <param name="client">飞书API客户端</param>
    public FeishuApprovalDefinitionService(IFeishuApiClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<FeishuResponse<CreateDefinitionResult>> CreateDefinitionAsync(CreateDefinitionRequest request)
    {
        var body = JsonSerializer.Serialize(request);
        using var http = new HttpRequestMessage(HttpMethod.Post, "/open-apis/approval/v4/approvals")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var resp = await _client.SendAsync(http);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FeishuResponse<CreateDefinitionResult>>(json) ?? new FeishuResponse<CreateDefinitionResult>();
        if (!result.IsSuccess)
        {
            throw new FeishuApiException($"Feishu API error: {result.Message}", result.Code, "/open-apis/approval/v4/approvals");
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<FeishuResponse<ApprovalDefinitionDetail>> GetDefinitionDetailAsync(string approvalCode)
    {
        using var http = new HttpRequestMessage(HttpMethod.Get, $"/open-apis/approval/v4/approvals/{approvalCode}");
        var resp = await _client.SendAsync(http);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FeishuResponse<ApprovalDefinitionDetail>>(json) ?? new FeishuResponse<ApprovalDefinitionDetail>();
        if (!result.IsSuccess)
        {
            throw new FeishuApiException($"Feishu API error: {result.Message}", result.Code, $"/open-apis/approval/v4/approvals/{approvalCode}");
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<FeishuResponse> SetDefinitionStatusAsync(string approvalCode, bool isEnabled)
    {
        var body = JsonSerializer.Serialize(new { is_enable = isEnabled });
        using var http = new HttpRequestMessage(HttpMethod.Post, $"/open-apis/approval/v4/approvals/{approvalCode}/status")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var resp = await _client.SendAsync(http);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FeishuResponse>(json) ?? new FeishuResponse();
        if (!result.IsSuccess)
        {
            throw new FeishuApiException($"Feishu API error: {result.Message}", result.Code, $"/open-apis/approval/v4/approvals/{approvalCode}/status");
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<FeishuResponse> SubscribeApprovalAsync(string approvalCode)
    {
        using var http = new HttpRequestMessage(HttpMethod.Post, $"/open-apis/approval/v4/approvals/{approvalCode}/subscribe");
        var resp = await _client.SendAsync(http);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FeishuResponse>(json) ?? new FeishuResponse();
        
        // 如果是已经订阅的错误，认为是成功的
        if (!result.IsSuccess && result.Code != 1390007)
        {
            throw new FeishuApiException($"Feishu API error: {result.Message}", result.Code, $"/open-apis/approval/v4/approvals/{approvalCode}/subscribe");
        }
        
        // 对于已存在订阅的情况，返回成功状态
        if (result.Code == 1390007)
        {
            result = new FeishuResponse { Code = 0, Message = "订阅成功（已存在）" };
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

    /// <inheritdoc />
    public async Task<FeishuResponse<ApprovalDefinitionDetail>> GetApprovalSubscriptionStatusAsync(string approvalCode)
    {
        // 复用查询定义详情的接口，飞书API返回的数据中包含订阅状态
        return await GetDefinitionDetailAsync(approvalCode);
    }
}


