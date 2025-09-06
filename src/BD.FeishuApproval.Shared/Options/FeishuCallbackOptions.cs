namespace BD.FeishuApproval.Shared.Options;

/// <summary>
/// 飞书回调配置
/// </summary>
public class FeishuCallbackOptions
{
    /// <summary>
    /// 回调接收URL（需在飞书后台配置）
    /// </summary>
    public string CallbackUrl { get; set; }

    /// <summary>
    /// 回调签名密钥（用于验证请求真实性）
    /// </summary>
    public string SigningSecret { get; set; }

    /// <summary>
    /// 是否启用回调验证（默认true）
    /// </summary>
    public bool EnableVerification { get; set; } = true;

    /// <summary>
    /// 回调请求超时时间（毫秒，默认5000）
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 5000;
}
