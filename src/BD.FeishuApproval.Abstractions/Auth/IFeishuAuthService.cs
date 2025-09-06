using BD.FeishuApproval.Shared.Models;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Auth;

/// <summary>
/// 飞书认证服务接口，负责获取和管理访问令牌
/// </summary>
public interface IFeishuAuthService
{
    /// <summary>
    /// 获取租户级访问令牌（tenant_access_token）
    /// </summary>
    /// <param name="forceRefresh">是否强制刷新令牌（忽略缓存）</param>
    /// <returns>令牌信息（含令牌字符串和过期时间）</returns>
    Task<FeishuToken> GetTenantAccessTokenAsync(bool forceRefresh = false);
}
