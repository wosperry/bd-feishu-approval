using BD.FeishuApproval.Abstractions.Definitions;
using BD.FeishuApproval.Abstractions.Http;
using BD.FeishuApproval.Shared.Abstractions;
using BD.FeishuApproval.Shared.Dtos.Definitions;
using BD.FeishuApproval.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

    public Task<FeishuCreateApprovalBody> CreateFeishuApprovalRequestBody<T>(string userOpenId, T request)
        where T : class, IFeishuApprovalRequest, new()
    {
        string approvalCode = string.Empty;
        string openId = userOpenId;
        string form = string.Empty;

        var t = typeof(T);
        var props = t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        var attribute = t.GetCustomAttributes(typeof(ApprovalCodeAttribute), false)
                  .FirstOrDefault() as ApprovalCodeAttribute;

        approvalCode = attribute?.Code ?? throw new InvalidOperationException($"类 {GetType().Name} 缺少 ApprovalCodeAttribute 特性");

        StringBuilder sb = new StringBuilder();


        sb.Append('[');

        foreach (var p in props)
        {
            sb.Append('{');

            var jsonPropertyNameAttribute = p.GetCustomAttribute<JsonPropertyNameAttribute>();

            if(jsonPropertyNameAttribute is null)
            {
                throw new Exception($@"字段未定义JsonPropertyNameAttribute：{p.Name}");
            }

            sb.Append($@"""id"":""{jsonPropertyNameAttribute.Name}""");
            sb.Append(',');

            sb.Append($@"""type"":""{GetFeishuControlType(p)}""");
            sb.Append(',');

            sb.Append($@"""value"":""{p.GetValue(request)}""");

            sb.Append('}');
            sb.Append(',');
        }

        sb.Remove(sb.Length - 1, 1);

        sb.Append(']');
            

        return Task.FromResult(new FeishuCreateApprovalBody
        {
            ApprovalCode = approvalCode,
            Form  = sb.ToString(),
            OpenId = openId
        }) ;
    }

    private static string GetFeishuControlType(PropertyInfo property)
    {
        if (property == null)
            return "input"; // 默认值

        var propertyType = property.PropertyType;

        // 处理数组/集合类型
        if (propertyType.IsArray)
        {
            // 数组元素类型
            var elementType = propertyType.GetElementType();
            if (elementType == typeof(string))
                return "multiselect"; // string[] 对应多选控件
        }
        // 处理泛型集合（如List<string>）
        else if (propertyType.IsGenericType &&
                 propertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var genericArguments = propertyType.GetGenericArguments();
            if (genericArguments.Length > 0 && genericArguments[0] == typeof(string))
                return "multiselect";
        }
        // 处理基础类型
        else
        {
            switch (propertyType)
            {
                case Type t when t == typeof(int) || t == typeof(long) || t == typeof(short):
                    return "number"; // 数字类型对应number控件
                case Type t when t == typeof(bool):
                    return "checkbox"; // 布尔类型对应checkbox控件
                case Type t when t == typeof(string):
                    // 字符串类型可能对应多个控件，这里返回最常用的input
                    // 如需更精确，可结合属性名称或自定义特性进一步判断
                    return "input";
                case Type t when t == typeof(DateTime) || t == typeof(DateTimeOffset):
                    return "datetime"; // 日期时间类型对应datetime控件
                default:
                    return "input"; // 默认控件类型
            }
        }

        return "input";
    }
}


