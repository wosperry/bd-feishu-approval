using BD.FeishuApproval.Abstractions.Http;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Abstractions.Persistence;
using BD.FeishuApproval.Services;
using BD.FeishuApproval.Shared.Dtos.Definitions;
using BD.FeishuApproval.Shared.Dtos.Instances;
using BD.FeishuApproval.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Instances;

/// <summary>
/// 飞书审批实例服务实现
/// </summary>
public class FeishuApprovalInstanceService : IFeishuApprovalInstanceService
{
    private readonly IFeishuApiClient _client;
    private readonly IFeishuApprovalRepository _repository;
    private readonly IFeishuUserService _userService;
    private readonly ILogger<FeishuApprovalInstanceService> _logger;

    /// <summary>
    /// 初始化飞书审批实例服务
    /// </summary>
    /// <param name="client">飞书API客户端</param>
    /// <param name="repository">数据仓储</param>
    /// <param name="userService">用户服务</param>
    /// <param name="logger">日志记录器</param>
    public FeishuApprovalInstanceService(
        IFeishuApiClient client, 
        IFeishuApprovalRepository repository,
        IFeishuUserService userService,
        ILogger<FeishuApprovalInstanceService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    } 

    /// <summary>
    /// 构建飞书创建审批实例请求
    /// </summary>
    private object BuildFeishuCreateInstanceRequest(CreateInstanceRequest request, string openId)
    {
        var feishuRequest = new
        {
            approval_code = request.ApprovalCode,
            open_id = openId,
            form = request.FormData
        };

        _logger.LogDebug("构建飞书API请求 - 审批代码: {ApprovalCode}, OpenId: {OpenId}, 表单数据长度: {FormDataLength}", 
            request.ApprovalCode, openId, request.FormData.Length);

        return feishuRequest;
    }

    /// <summary>
    /// 调用飞书API创建审批实例
    /// </summary>
    public async Task<FeishuResponse<CreateInstanceResult>> CallFeishuCreateInstanceApiAsync(FeishuCreateApprovalBody requestBody)
    {
        // 1. 配置支持中文的JSON序列化选项
        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false // 紧凑格式
        };

        // 2. 手动将对象序列化为JSON字符串（确保中文正确处理）
        string jsonBody = JsonSerializer.Serialize(requestBody, jsonOptions);

        // 3. 创建StringContent，指定UTF8编码和application/json类型
        var content = new StringContent(
            jsonBody,
            Encoding.UTF8, // 明确指定UTF8编码，避免中文乱码
            "application/json"
        );

        // 4. 构建HTTP请求
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/open-apis/approval/v4/instances"
        )
        {
            Content = content
        };

        _logger.LogDebug("正在调用飞书API创建审批实例...");

        var response = await _client.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogDebug("飞书API响应 - StatusCode: {StatusCode}, 响应体长度: {ResponseLength}", 
            response.StatusCode, responseBody.Length);

        var result = JsonSerializer.Deserialize<FeishuResponse<CreateInstanceResult>>(responseBody) 
                    ?? new FeishuResponse<CreateInstanceResult>();

        if (!result.IsSuccess)
        {
            var errorMessage = $"飞书API调用失败 - 错误码: {result.Code}, 错误信息: {result.Message}";
            _logger.LogError(errorMessage);
            throw new FeishuApiException(errorMessage, result.Code, "/open-apis/approval/v4/instances");
        }

        return result;
    }


    /// <inheritdoc />
    public async Task<FeishuResponse<InstanceDetail>> GetInstanceDetailAsync(string instanceCode)
    {
        _logger.LogInformation("开始获取审批实例详情 - 实例代码: {InstanceCode}", instanceCode);

        try
        {
            if (string.IsNullOrWhiteSpace(instanceCode))
            {
                _logger.LogError("实例代码为空");
                throw new ArgumentException("实例代码不能为空", nameof(instanceCode));
            }

            using var http = new HttpRequestMessage(HttpMethod.Get, $"/open-apis/approval/v4/instances/{instanceCode}");
            
            _logger.LogDebug("正在调用飞书API获取实例详情...");
            var resp = await _client.SendAsync(http);
            var json = await resp.Content.ReadAsStringAsync();

            _logger.LogDebug("飞书API响应 - StatusCode: {StatusCode}, 响应体长度: {ResponseLength}", 
                resp.StatusCode, json.Length);

            var result = JsonSerializer.Deserialize<FeishuResponse<InstanceDetail>>(json) ?? new FeishuResponse<InstanceDetail>();
            
            if (!result.IsSuccess)
            {
                var errorMessage = $"获取审批实例详情失败 - 错误码: {result.Code}, 错误信息: {result.Message}";
                _logger.LogError(errorMessage);
                throw new FeishuApiException(errorMessage, result.Code, $"/open-apis/approval/v4/instances/{instanceCode}");
            }

            _logger.LogInformation("成功获取审批实例详情 - 实例代码: {InstanceCode}", instanceCode);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审批实例详情失败 - 实例代码: {InstanceCode}", instanceCode);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FeishuResponse<InstanceStatus>> SyncInstanceStatusAsync(string instanceCode)
    {
        _logger.LogInformation("开始同步审批实例状态 - 实例代码: {InstanceCode}", instanceCode);

        try
        {
            if (string.IsNullOrWhiteSpace(instanceCode))
            {
                _logger.LogError("实例代码为空");
                throw new ArgumentException("实例代码不能为空", nameof(instanceCode));
            }

            using var http = new HttpRequestMessage(HttpMethod.Get, $"/open-apis/approval/v4/instances/{instanceCode}/status");
            
            _logger.LogDebug("正在调用飞书API同步实例状态...");
            var resp = await _client.SendAsync(http);
            var json = await resp.Content.ReadAsStringAsync();

            _logger.LogDebug("飞书API响应 - StatusCode: {StatusCode}, 响应体长度: {ResponseLength}", 
                resp.StatusCode, json.Length);

            var result = JsonSerializer.Deserialize<FeishuResponse<InstanceStatus>>(json) ?? new FeishuResponse<InstanceStatus>();
            
            if (!result.IsSuccess)
            {
                var errorMessage = $"同步审批实例状态失败 - 错误码: {result.Code}, 错误信息: {result.Message}";
                _logger.LogError(errorMessage);
                throw new FeishuApiException(errorMessage, result.Code, $"/open-apis/approval/v4/instances/{instanceCode}/status");
            }

            _logger.LogInformation("成功同步审批实例状态 - 实例代码: {InstanceCode}, 状态: {Status}", 
                instanceCode, result.Data?.Status);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步审批实例状态失败 - 实例代码: {InstanceCode}", instanceCode);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FeishuResponse> TerminateInstanceAsync(string instanceCode, string reason)
    {
        _logger.LogInformation("开始终止审批实例 - 实例代码: {InstanceCode}, 原因: {Reason}", instanceCode, reason);

        try
        {
            if (string.IsNullOrWhiteSpace(instanceCode))
            {
                _logger.LogError("实例代码为空");
                throw new ArgumentException("实例代码不能为空", nameof(instanceCode));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                _logger.LogError("终止原因为空");
                throw new ArgumentException("终止原因不能为空", nameof(reason));
            }

            var body = JsonSerializer.Serialize(new { reason });
            using var http = new HttpRequestMessage(HttpMethod.Post, $"/open-apis/approval/v4/instances/{instanceCode}/terminate")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            _logger.LogDebug("正在调用飞书API终止实例...");
            var resp = await _client.SendAsync(http);
            var json = await resp.Content.ReadAsStringAsync();

            _logger.LogDebug("飞书API响应 - StatusCode: {StatusCode}, 响应体长度: {ResponseLength}", 
                resp.StatusCode, json.Length);

            var result = JsonSerializer.Deserialize<FeishuResponse>(json) ?? new FeishuResponse();
            
            if (!result.IsSuccess)
            {
                var errorMessage = $"终止审批实例失败 - 错误码: {result.Code}, 错误信息: {result.Message}";
                _logger.LogError(errorMessage);
                throw new FeishuApiException(errorMessage, result.Code, $"/open-apis/approval/v4/instances/{instanceCode}/terminate");
            }

            _logger.LogInformation("成功终止审批实例 - 实例代码: {InstanceCode}", instanceCode);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "终止审批实例失败 - 实例代码: {InstanceCode}", instanceCode);
            throw;
        }
    }
}


