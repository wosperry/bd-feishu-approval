namespace BD.FeishuApproval.Shared.Options;

/// <summary>
/// 飞书仪表盘配置选项
/// </summary>
public class FeishuDashboardOptions
{
    /// <summary>
    /// 仪表盘路径前缀，默认为 "/feishu"
    /// </summary>
    public string PathPrefix { get; set; } = "/feishu";

    /// <summary>
    /// 管理页面路径，默认为 "/feishu/manage"
    /// </summary>
    public string ManagePath => $"{PathPrefix.TrimEnd('/')}/manage";

    /// <summary>
    /// API路径前缀，默认为 "/feishu/api"
    /// </summary>
    public string ApiPrefix => $"{PathPrefix.TrimEnd('/')}/api";
}