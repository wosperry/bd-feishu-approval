using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BD.FeishuApproval.Abstractions.Auth;
using BD.FeishuApproval.Abstractions.Http;
using BD.FeishuApproval.Abstractions.Persistence;
using BD.FeishuApproval.Shared.Models;
using System.IO;

namespace BD.FeishuApproval.Http;

/// <summary>
/// 飞书API客户端实现
/// </summary>
public class FeishuApiClient : IFeishuApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IFeishuAuthService _authService;
    private readonly IFeishuApprovalRepository _repository;

    /// <summary>
    /// 初始化飞书API客户端
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="authService">飞书认证服务</param>
    /// <param name="repository">飞书审批存储库</param>
    public FeishuApiClient(HttpClient httpClient, IFeishuAuthService authService, IFeishuApprovalRepository repository)
    {
        _httpClient = httpClient;
        _authService = authService;
        _repository = repository;
    }

    /// <inheritdoc />
    public Uri BaseAddress => _httpClient.BaseAddress;

    /// <inheritdoc />
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var token = await _authService.GetTenantAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.TenantAccessToken);

        var traceId = Guid.NewGuid().ToString("N");
        
        // 读取请求体内容
        var requestBody = string.Empty;
        if (request.Content != null)
        {
            requestBody = await request.Content.ReadAsStringAsync();
            // 重新创建请求内容，因为已经读取过了
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(requestBody);
            request.Content = new ByteArrayContent(contentBytes);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        }
        
        // 记录请求日志
        await _repository.SaveRequestLogAsync(new FeishuRequestLog
        {
            Method = request.Method.Method,
            Url = request.RequestUri?.ToString() ?? string.Empty,
            Headers = request.Headers.ToString(),
            Body = requestBody,
            TraceId = traceId
        }, cancellationToken);

        HttpResponseMessage response = null;
        string responseBody = string.Empty;
        string errorMessage = string.Empty;
        bool isSuccess = false;
        
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
            responseBody = response.Content != null ? await response.Content.ReadAsStringAsync() : string.Empty;
            isSuccess = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            isSuccess = false;
        }

        // 记录响应日志
        await _repository.SaveResponseLogAsync(new FeishuResponseLog
        {
            StatusCode = response?.StatusCode != null ? (int)response.StatusCode : 0,
            Headers = response?.Headers?.ToString() ?? string.Empty,
            Body = responseBody,
            Success = isSuccess,
            TraceId = traceId,
            ErrorMessage = errorMessage
        }, cancellationToken);
        
        if (response != null && !response.IsSuccessStatusCode)
        {
            throw new FeishuApiException($"Http error: {(int)response.StatusCode}", (int)response.StatusCode, request.RequestUri?.AbsolutePath);
        }
        else if (response == null)
        {
            throw new FeishuApiException($"Request failed: {errorMessage}", 0, request.RequestUri?.AbsolutePath);
        }
        
        return response;
    }
}


