namespace BD.FeishuApproval.Shared.Options;

/// <summary>
/// 飞书API核心配置
/// </summary>
public class FeishuApiOptions
{
    /// <summary>
    /// 飞书应用ID（从飞书开发者后台获取）
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// 飞书应用密钥（需保密）
    /// </summary>
    public string AppSecret { get; set; }

    /// <summary>
    /// 飞书租户ID（可选）
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// 飞书API基础地址（默认：https://open.feishu.cn）
    /// </summary>
    public string BaseUrl { get; set; } = "https://open.feishu.cn";

    /// <summary>
    /// 令牌缓存过期时间（秒，默认7000）
    /// </summary>
    public int TokenExpireSeconds { get; set; } = 7000;
}
