using BD.FeishuApproval.Abstractions.Configs;
using BD.FeishuApproval.Abstractions.Persistence;
using BD.FeishuApproval.Shared.Options;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Auth;

/// <summary>
/// 从数据库读取的默认配置提供者。
/// 注意：GetApiOptions/GetCallbackOptions 为同步签名，这里以阻塞方式读取一次并缓存，
/// 若需热更新可在 Dashboard API 更新后刷新缓存。
/// </summary>
public class DbFeishuConfigProvider : IFeishuConfigProvider
{
    private readonly IFeishuApprovalRepository _repo;
    private FeishuApiOptions _apiOptions;
    private FeishuCallbackOptions _callbackOptions;

    /// <summary>
    /// 初始化数据库配置提供器
    /// </summary>
    /// <param name="repo">飞书审批存储库</param>
    public DbFeishuConfigProvider(IFeishuApprovalRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// 获取API配置选项
    /// </summary>
    /// <returns>API配置选项</returns>
    public FeishuApiOptions GetApiOptions()
    {
        if (_apiOptions != null) return _apiOptions;
        var cfg = Task.Run(() => _repo.GetAccessConfigAsync()).GetAwaiter().GetResult();
        _apiOptions = new FeishuApiOptions
        {
            AppId = cfg.AppId,
            AppSecret = cfg.AppSecret,
        };
        return _apiOptions;
    }

    /// <summary>
    /// 获取回调配置选项
    /// </summary>
    /// <returns>回调配置选项</returns>
    public FeishuCallbackOptions GetCallbackOptions()
    {
        if (_callbackOptions != null) return _callbackOptions;
        var cfg = Task.Run(() => _repo.GetAccessConfigAsync()).GetAwaiter().GetResult();
        _callbackOptions = new FeishuCallbackOptions
        {
            SigningSecret = cfg.EncryptKey,
            // CallbackUrl 由宿主应用决定是否暴露，这里不强制
        };
        return _callbackOptions;
    }
}


