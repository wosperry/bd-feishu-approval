using BD.FeishuApproval.Shared.Options;

namespace BD.FeishuApproval.Abstractions.Configs;

/// <summary>
/// 飞书配置提供接口，负责提供API所需的配置信息
/// </summary>
public interface IFeishuConfigProvider
{
    /// <summary>
    /// 获取飞书API配置
    /// </summary>
    FeishuApiOptions GetApiOptions();

    /// <summary>
    /// 获取回调配置（如回调URL、签名密钥）
    /// </summary>
    FeishuCallbackOptions GetCallbackOptions();
}
