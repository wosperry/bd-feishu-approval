using BD.FeishuApproval.Abstractions.Http;
using BD.FeishuApproval.Abstractions.Persistence;
using BD.FeishuApproval.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Services;

/// <summary>
/// 飞书用户服务
/// 负责用户OpenId的获取、缓存和管理
/// </summary>
public class FeishuUserService : IFeishuUserService
{
    private readonly IFeishuApiClient _client;
    private readonly IFeishuApprovalRepository _repository;
    private readonly ILogger<FeishuUserService> _logger;

    public FeishuUserService(
        IFeishuApiClient client,
        IFeishuApprovalRepository repository,
        ILogger<FeishuUserService> logger)
    {
        _client = client;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户的飞书OpenId（带缓存机制）
    /// 优先从数据库获取，如果不存在则调用飞书API获取并缓存
    /// </summary>
    /// <param name="userId">用户标识（手机号、邮箱、UnionId等）</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <returns>飞书OpenId</returns>
    public async Task<string> GetUserOpenIdAsync(string userId, string userIdType = "auto")
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("用户ID为空");
            return string.Empty;
        }

        try
        {
            // 1. 先尝试从数据库获取缓存的OpenId
            var cachedOpenId = await GetCachedOpenIdAsync(userId, userIdType);
            if (!string.IsNullOrEmpty(cachedOpenId))
            {
                _logger.LogDebug("从缓存获取OpenId成功 - 用户: {UserId}, OpenId: {OpenId}", userId, cachedOpenId);
                return cachedOpenId;
            }

            // 2. 缓存中没有，调用飞书API获取
            _logger.LogInformation("缓存中未找到OpenId，调用飞书API获取 - 用户: {UserId}, 类型: {UserIdType}", userId, userIdType);
            var openId = await FetchOpenIdFromFeishuAsync(userId, userIdType);
            
            if (string.IsNullOrEmpty(openId))
            {
                _logger.LogWarning("从飞书API获取OpenId失败 - 用户: {UserId}", userId);
                return string.Empty;
            }

            // 3. 将获取到的OpenId缓存到数据库
            await CacheOpenIdAsync(userId, userIdType, openId);
            _logger.LogInformation("OpenId获取并缓存成功 - 用户: {UserId}, OpenId: {OpenId}", userId, openId);
            
            return openId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户OpenId失败 - 用户: {UserId}, 类型: {UserIdType}", userId, userIdType);
            throw;
        }
    }

    /// <summary>
    /// 从数据库获取缓存的OpenId
    /// </summary>
    private async Task<string> GetCachedOpenIdAsync(string userId, string userIdType)
    {
        try
        {
            // 根据用户ID类型查询数据库
            switch (userIdType.ToLower())
            {
                case "mobile":
                    return await _repository.GetOpenIdByMobileAsync(userId);
                case "email":
                    return await _repository.GetOpenIdByEmailAsync(userId);
                case "union_id":
                    return await _repository.GetOpenIdByUnionIdAsync(userId);
                case "user_id":
                    return await _repository.GetOpenIdByUserIdAsync(userId);
                case "open_id":
                    return userId; // 如果已经是OpenId，直接返回
                case "auto":
                default:
                    // 自动识别类型并查询
                    return await GetCachedOpenIdByAutoDetectAsync(userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从数据库获取缓存OpenId失败 - 用户: {UserId}, 类型: {UserIdType}", userId, userIdType);
            return string.Empty;
        }
    }

    /// <summary>
    /// 自动识别用户ID类型并从数据库查询
    /// </summary>
    private async Task<string> GetCachedOpenIdByAutoDetectAsync(string userId)
    {
        // 检测手机号
        if (userId.Length == 11 && userId.All(char.IsDigit))
        {
            return await _repository.GetOpenIdByMobileAsync(userId);
        }

        // 检测邮箱
        if (userId.Contains("@"))
        {
            return await _repository.GetOpenIdByEmailAsync(userId);
        }

        // 检测UnionId
        if (userId.StartsWith("ou_"))
        {
            return await _repository.GetOpenIdByUnionIdAsync(userId);
        }

        // 检测UserId
        if (userId.All(char.IsDigit))
        {
            return await _repository.GetOpenIdByUserIdAsync(userId);
        }

        // 如果无法识别，尝试作为OpenId返回
        return userId;
    }

    /// <summary>
    /// 调用飞书API获取OpenId
    /// </summary>
    private async Task<string> FetchOpenIdFromFeishuAsync(string userId, string userIdType)
    {
        switch (userIdType.ToLower())
        {
            case "mobile":
                return await FetchOpenIdByMobileAsync(userId);
            case "email":
                return await FetchOpenIdByEmailAsync(userId);
            case "union_id":
                return await FetchOpenIdByUnionIdAsync(userId);
            case "user_id":
                return await FetchOpenIdByUserIdAsync(userId);
            case "open_id":
                return userId;
            case "auto":
            default:
                return await FetchOpenIdByAutoDetectAsync(userId);
        }
    }

    /// <summary>
    /// 自动识别用户ID类型并调用飞书API
    /// </summary>
    private async Task<string> FetchOpenIdByAutoDetectAsync(string userId)
    {
        // 检测手机号
        if (userId.Length == 11 && userId.All(char.IsDigit))
        {
            return await FetchOpenIdByMobileAsync(userId);
        }

        // 检测邮箱
        if (userId.Contains("@"))
        {
            return await FetchOpenIdByEmailAsync(userId);
        }

        // 检测UnionId
        if (userId.StartsWith("ou_"))
        {
            return await FetchOpenIdByUnionIdAsync(userId);
        }

        // 检测UserId
        if (userId.All(char.IsDigit))
        {
            return await FetchOpenIdByUserIdAsync(userId);
        }

        // 如果无法识别，尝试作为OpenId返回
        return userId;
    }

    /// <summary>
    /// 通过手机号获取OpenId
    /// </summary>
    private async Task<string> FetchOpenIdByMobileAsync(string mobile)
    {
        var requestBody = new
        {
            mobiles = new[] { mobile },
            emails = Array.Empty<string>()
        };

        return await CallFeishuBatchGetIdApiAsync(requestBody);
    }

    /// <summary>
    /// 通过邮箱获取OpenId
    /// </summary>
    private async Task<string> FetchOpenIdByEmailAsync(string email)
    {
        var requestBody = new
        {
            mobiles = Array.Empty<string>(),
            emails = new[] { email }
        };

        return await CallFeishuBatchGetIdApiAsync(requestBody);
    }

    /// <summary>
    /// 通过UnionId获取OpenId
    /// </summary>
    private async Task<string> FetchOpenIdByUnionIdAsync(string unionId)
    {
        var requestBody = new
        {
            union_ids = new[] { unionId }
        };

        return await CallFeishuBatchGetIdApiAsync(requestBody);
    }

    /// <summary>
    /// 通过UserId获取OpenId
    /// </summary>
    private async Task<string> FetchOpenIdByUserIdAsync(string userId)
    {
        var requestBody = new
        {
            user_ids = new[] { userId }
        };

        return await CallFeishuBatchGetIdApiAsync(requestBody);
    }

    /// <summary>
    /// 调用飞书批量获取ID API
    /// </summary>
    private async Task<string> CallFeishuBatchGetIdApiAsync(object requestBody)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var jsonBody = JsonSerializer.Serialize(requestBody, jsonOptions);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            
            var req = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://open.feishu.cn/open-apis/contact/v3/users/batch_get_id"),
                Content = content
            };

            var response = await _client.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BatchGetIdResponse>(responseBody);

            if (result?.Data?.UserList != null && result.Data.UserList.Length > 0)
            {
                return result.Data.UserList[0].UserId;
            }

            return string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "调用飞书批量获取ID API失败");
            throw new FeishuApiException($"调用飞书批量获取ID API失败: {ex.Message}", 0, "/open-apis/contact/v3/users/batch_get_id");
        }
    }

    /// <summary>
    /// 将OpenId缓存到数据库
    /// </summary>
    private async Task CacheOpenIdAsync(string userId, string userIdType, string openId)
    {
        try
        {
            switch (userIdType.ToLower())
            {
                case "mobile":
                    await _repository.CacheOpenIdByMobileAsync(userId, openId);
                    break;
                case "email":
                    await _repository.CacheOpenIdByEmailAsync(userId, openId);
                    break;
                case "union_id":
                    await _repository.CacheOpenIdByUnionIdAsync(userId, openId);
                    break;
                case "user_id":
                    await _repository.CacheOpenIdByUserIdAsync(userId, openId);
                    break;
                case "auto":
                default:
                    await CacheOpenIdByAutoDetectAsync(userId, openId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "缓存OpenId到数据库失败 - 用户: {UserId}, OpenId: {OpenId}", userId, openId);
            // 缓存失败不影响主流程，只记录警告
        }
    }

    /// <summary>
    /// 自动识别用户ID类型并缓存OpenId
    /// </summary>
    private async Task CacheOpenIdByAutoDetectAsync(string userId, string openId)
    {
        // 检测手机号
        if (userId.Length == 11 && userId.All(char.IsDigit))
        {
            await _repository.CacheOpenIdByMobileAsync(userId, openId);
            return;
        }

        // 检测邮箱
        if (userId.Contains("@"))
        {
            await _repository.CacheOpenIdByEmailAsync(userId, openId);
            return;
        }

        // 检测UnionId
        if (userId.StartsWith("ou_"))
        {
            await _repository.CacheOpenIdByUnionIdAsync(userId, openId);
            return;
        }

        // 检测UserId
        if (userId.All(char.IsDigit))
        {
            await _repository.CacheOpenIdByUserIdAsync(userId, openId);
            return;
        }

        // 如果无法识别，记录警告
        _logger.LogWarning("无法识别用户ID类型，跳过缓存 - 用户: {UserId}, OpenId: {OpenId}", userId, openId);
    }

    /// <summary>
    /// 清除用户OpenId缓存
    /// </summary>
    public async Task ClearUserOpenIdCacheAsync(string userId, string userIdType = "auto")
    {
        try
        {
            switch (userIdType.ToLower())
            {
                case "mobile":
                    await _repository.ClearOpenIdCacheByMobileAsync(userId);
                    break;
                case "email":
                    await _repository.ClearOpenIdCacheByEmailAsync(userId);
                    break;
                case "union_id":
                    await _repository.ClearOpenIdCacheByUnionIdAsync(userId);
                    break;
                case "user_id":
                    await _repository.ClearOpenIdCacheByUserIdAsync(userId);
                    break;
                case "auto":
                default:
                    await ClearOpenIdCacheByAutoDetectAsync(userId);
                    break;
            }
            
            _logger.LogInformation("清除用户OpenId缓存成功 - 用户: {UserId}, 类型: {UserIdType}", userId, userIdType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除用户OpenId缓存失败 - 用户: {UserId}, 类型: {UserIdType}", userId, userIdType);
            throw;
        }
    }

    /// <summary>
    /// 自动识别用户ID类型并清除缓存
    /// </summary>
    private async Task ClearOpenIdCacheByAutoDetectAsync(string userId)
    {
        // 检测手机号
        if (userId.Length == 11 && userId.All(char.IsDigit))
        {
            await _repository.ClearOpenIdCacheByMobileAsync(userId);
            return;
        }

        // 检测邮箱
        if (userId.Contains("@"))
        {
            await _repository.ClearOpenIdCacheByEmailAsync(userId);
            return;
        }

        // 检测UnionId
        if (userId.StartsWith("ou_"))
        {
            await _repository.ClearOpenIdCacheByUnionIdAsync(userId);
            return;
        }

        // 检测UserId
        if (userId.All(char.IsDigit))
        {
            await _repository.ClearOpenIdCacheByUserIdAsync(userId);
            return;
        }

        _logger.LogWarning("无法识别用户ID类型，跳过清除缓存 - 用户: {UserId}", userId);
    }
}

/// <summary>
/// 飞书用户服务接口
/// </summary>
public interface IFeishuUserService
{
    /// <summary>
    /// 获取用户的飞书OpenId（带缓存机制）
    /// </summary>
    Task<string> GetUserOpenIdAsync(string userId, string userIdType = "auto");

    /// <summary>
    /// 清除用户OpenId缓存
    /// </summary>
    Task ClearUserOpenIdCacheAsync(string userId, string userIdType = "auto");
}

/// <summary>
/// 飞书批量获取ID响应模型
/// </summary>
public class BatchGetIdResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string Msg { get; set; }

    [JsonPropertyName("data")]
    public BatchGetIdData Data { get; set; }
}

public class BatchGetIdData
{
    [JsonPropertyName("user_list")]
    public UserInfo[] UserList { get; set; }
}

public class UserInfo
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("mobile")]
    public string Mobile { get; set; }
}
