#if NET8_0_OR_GREATER
namespace BD.FeishuApproval.Dashboard;

/// <summary>
/// 飞书Dashboard选项配置
/// </summary>
public class FeishuDashboardOptions
{
    /// <summary>
    /// API路径前缀
    /// </summary>
    public string ApiPrefix { get; set; } = "/feishu/api";
    
    /// <summary>
    /// Dashboard路径前缀
    /// </summary>
    public string PathPrefix { get; set; } = "/feishu";
    
    /// <summary>
    /// 管理页面路径
    /// </summary>
    public string ManagePath { get; set; } = "/feishu/manage";
}
#endif