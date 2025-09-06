using BD.FeishuApproval.Abstractions.Auth;
using BD.FeishuApproval.Abstractions.Configs;
using BD.FeishuApproval.Shared.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Auth;

/// <summary>
/// 飞书认证服务实现
/// </summary>
public class FeishuAuthService : IFeishuAuthService
{
    private readonly IFeishuConfigProvider _configProvider;
    private readonly HttpClient _httpClient;
    private FeishuToken _cachedToken;

    /// <summary>
    /// 初始化飞书认证服务
    /// </summary>
    /// <param name="configProvider">飞书配置提供器</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    public FeishuAuthService(IFeishuConfigProvider configProvider, IHttpClientFactory httpClientFactory)
    {
        _configProvider = configProvider;
        _httpClient = httpClientFactory.CreateClient(nameof(FeishuAuthService));
        var baseUrl = _configProvider.GetApiOptions().BaseUrl?.TrimEnd('/') ?? "https://open.feishu.cn";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    /// <inheritdoc />
    public async Task<FeishuToken> GetTenantAccessTokenAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedToken != null)
        {
            var notExpired = (DateTime.UtcNow - _cachedToken.FetchTime).TotalSeconds < _cachedToken.ExpireSeconds - 60;
            if (notExpired)
            {
                return _cachedToken;
            }
        }

        var apiOptions = _configProvider.GetApiOptions();
        
        // 使用查询参数方式，与你提供的成功示例保持一致
        var requestUrl = $"/open-apis/auth/v3/tenant_access_token/internal?app_id={apiOptions.AppId}&app_secret={apiOptions.AppSecret}";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        using var response = await _httpClient.SendAsync(request, CancellationToken.None);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var code = root.TryGetProperty("code", out var codeProp) ? codeProp.GetInt32() : 0;
        if (code != 0)
        {
            var msg = root.TryGetProperty("msg", out var msgProp) ? msgProp.GetString() : "unknown error";
            throw new InvalidOperationException($"Feishu token error: {code} - {msg}");
        }
        var token = new FeishuToken
        {
            TenantAccessToken = root.GetProperty("tenant_access_token").GetString(),
            ExpireSeconds = root.GetProperty("expire").GetInt32(),
            FetchTime = DateTime.UtcNow
        };
        _cachedToken = token;
        return token;
    }
}
