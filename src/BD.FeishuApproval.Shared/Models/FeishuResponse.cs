using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 飞书API通用响应模型
/// </summary>
/// <typeparam name="T">业务数据类型</typeparam>
public class FeishuResponse<T>
{
    /// <summary>
    /// 错误码（0表示成功）
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 错误消息（成功时为空）
    /// </summary>
    [JsonPropertyName("msg")]
    public string Message { get; set; }

    /// <summary>
    /// 业务数据（成功时返回）
    /// </summary>
    [JsonPropertyName("data")]
    public T Data { get; set; }

    /// <summary>
    /// 是否请求成功
    /// </summary>
    public bool IsSuccess => Code == 0;
}

/// <summary>
/// 无业务数据的飞书响应（如删除、更新操作）
/// </summary>
public class FeishuResponse : FeishuResponse<object> { }
