namespace BD.FeishuApproval.Shared.Models;

/// <summary>
/// 飞书请求上下文
/// </summary>
public class FeishuRequestContext
{
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
}
