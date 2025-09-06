using BD.FeishuApproval.Abstractions.Http;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Abstractions.Persistence;
using BD.FeishuApproval.Shared.Dtos.Instances;
using BD.FeishuApproval.Shared.Models;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Instances;

/// <summary>
/// 飞书审批实例服务实现
/// </summary>
public class FeishuApprovalInstanceService : IFeishuApprovalInstanceService
{
    private readonly IFeishuApiClient _client;
    private readonly IFeishuApprovalRepository _repository;

    /// <summary>
    /// 初始化飞书审批实例服务
    /// </summary>
    /// <param name="client">飞书API客户端</param>
    public FeishuApprovalInstanceService(IFeishuApiClient client, IFeishuApprovalRepository repository)
    {
        _client = client;
        _repository = repository;
    }

    // 响应模型类（根据飞书API实际响应结构定义）
    public class BatchGetIdResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; } // 0表示成功
        [JsonPropertyName("msg")]
        public string Msg { get; set; }
        [JsonPropertyName("data")]
        public BatchGetIdData Data { get; set; }
    }
    public class UserInfo
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } // 飞书用户ID
        [JsonPropertyName("mobile")]
        public string Mobile { get; set; } // 对应的手机号
    }

    public class BatchGetIdData
    {
        [JsonPropertyName("user_list")]
        public UserInfo[] UserList { get; set; }
    }

    /// <inheritdoc />
    public async Task<FeishuResponse<CreateInstanceResult>> CreateInstanceAsync(CreateInstanceRequest request)
    {
        var body = JsonSerializer.Serialize(request);
        var user = await _repository.GetUserAsync(1);
        if (string.IsNullOrEmpty(user.FeishuOpenId))
        {
            // 构建请求参数对象
            var requestBody = new
            {
                mobiles = new[] { user.Phone }, // 手机号数组
                emails = Array.Empty<string>() // 空邮箱数组
            };

            // 序列化请求体（确保中文正常处理）
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var jsonBody = JsonSerializer.Serialize(requestBody, jsonOptions);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://open.feishu.cn/open-apis/contact/v3/users/batch_get_id"),
                Content = content
            };

            try
            {
                // 发送请求并获取响应
                var response = await _client.SendAsync(req);
                response.EnsureSuccessStatusCode(); // 确保响应状态码为成功

                // 解析响应内容
                var responseBody = await response.Content.ReadAsStringAsync();
                var res = JsonSerializer.Deserialize<BatchGetIdResponse>(responseBody);

                // 提取OpenId（根据实际响应结构调整）
                if (res?.Data?.UserList != null && res.Data.UserList.Length > 0)
                {
                    var openId = res.Data.UserList[0].UserId;

                    await _repository.UpdateUserOpenIdAsync(user.Id, openId);

                    // TODO: 发起审批
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                // 处理请求异常
                Console.WriteLine($"请求飞书API失败: {ex.Message}");
                throw;
            }
        }


        //先在这里写了


        using var http = new HttpRequestMessage(HttpMethod.Post, "/open-apis/approval/v4/instances")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var resp = await _client.SendAsync(http);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FeishuResponse<CreateInstanceResult>>(json) ?? new FeishuResponse<CreateInstanceResult>();
        if (!result.IsSuccess)
        {
            throw new FeishuApiException($"Feishu API error: {result.Message}", result.Code, "/open-apis/approval/v4/instances");
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<FeishuResponse<InstanceDetail>> GetInstanceDetailAsync(string instanceCode)
    {
        using var http = new HttpRequestMessage(HttpMethod.Get, $"/open-apis/approval/v4/instances/{instanceCode}");
        var resp = await _client.SendAsync(http);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FeishuResponse<InstanceDetail>>(json) ?? new FeishuResponse<InstanceDetail>();
        if (!result.IsSuccess)
        {
            throw new FeishuApiException($"Feishu API error: {result.Message}", result.Code, $"/open-apis/approval/v4/instances/{instanceCode}");
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<FeishuResponse<InstanceStatus>> SyncInstanceStatusAsync(string instanceCode)
    {
        using var http = new HttpRequestMessage(HttpMethod.Get, $"/open-apis/approval/v4/instances/{instanceCode}/status");
        var resp = await _client.SendAsync(http);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FeishuResponse<InstanceStatus>>(json) ?? new FeishuResponse<InstanceStatus>();
        if (!result.IsSuccess)
        {
            throw new FeishuApiException($"Feishu API error: {result.Message}", result.Code, $"/open-apis/approval/v4/instances/{instanceCode}/status");
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<FeishuResponse> TerminateInstanceAsync(string instanceCode, string reason)
    {
        var body = JsonSerializer.Serialize(new { reason });
        using var http = new HttpRequestMessage(HttpMethod.Post, $"/open-apis/approval/v4/instances/{instanceCode}/terminate")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var resp = await _client.SendAsync(http);
        var json = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FeishuResponse>(json) ?? new FeishuResponse();
        if (!result.IsSuccess)
        {
            throw new FeishuApiException($"Feishu API error: {result.Message}", result.Code, $"/open-apis/approval/v4/instances/{instanceCode}/terminate");
        }
        return result;
    }
}


