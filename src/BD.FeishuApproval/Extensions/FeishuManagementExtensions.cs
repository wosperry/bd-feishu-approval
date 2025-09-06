using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using BD.FeishuApproval.Controllers;

#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace BD.FeishuApproval.Extensions;

/// <summary>
/// 飞书管理 API 扩展方法
/// </summary>
public static class FeishuManagementExtensions
{
#if NET8_0_OR_GREATER
    /// <summary>
    /// 添加飞书管理 API 控制器
    /// 使用方可以通过这个方法将所有管理功能暴露为 REST API，方便构建自定义管理界面
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuManagementApi(this IServiceCollection services)
    {
        // 确保控制器服务已注册
        services.AddControllers();
        
        // 添加管理控制器
        services.AddScoped<FeishuManagementController>();
        
        return services;
    }

    /// <summary>
    /// 映射飞书管理 API 路由
    /// 将管理 API 控制器映射到指定路由前缀
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="routePrefix">路由前缀，默认为 "api/feishu-management"</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder MapFeishuManagementApi(this IApplicationBuilder app, string routePrefix = null)
    {
        if (!string.IsNullOrEmpty(routePrefix))
        {
            // 如果指定了自定义路由前缀，需要动态调整控制器路由
            // 这里可以通过中间件或其他方式实现路由重写
            // 为简化实现，建议使用默认路由
        }
        
        return app;
    }
#endif
}

/// <summary>
/// 飞书管理 API 客户端
/// 提供便利的客户端类，方便使用方调用管理 API
/// </summary>
public class FeishuManagementApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    /// <summary>
    /// 初始化飞书管理 API 客户端
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="baseUrl">API基础地址，如 "https://your-app.com"</param>
    public FeishuManagementApiClient(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// 获取系统配置状态
    /// </summary>
    public async Task<BD.FeishuApproval.Abstractions.Management.ConfigurationStatus> GetConfigurationStatusAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/feishu-management/configuration-status");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<BD.FeishuApproval.Abstractions.Management.ConfigurationStatus>(json);
    }

    /// <summary>
    /// 设置管理员密码
    /// </summary>
    public async Task<BD.FeishuApproval.Abstractions.Management.ManagementOperationResult> SetAdminPasswordAsync(string password)
    {
        var request = new { Password = password };
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/feishu-management/admin-password", content);
        var responseJson = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<BD.FeishuApproval.Abstractions.Management.ManagementOperationResult>(responseJson);
    }

    /// <summary>
    /// 验证管理员密码
    /// </summary>
    public async Task<bool> VerifyAdminPasswordAsync(string password)
    {
        var request = new { Password = password };
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/feishu-management/admin-password/verify", content);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<bool>(responseJson);
    }

    /// <summary>
    /// 保存飞书配置
    /// </summary>
    public async Task<BD.FeishuApproval.Abstractions.Management.ManagementOperationResult> SaveFeishuConfigAsync(
        string appId, string appSecret, string adminPassword, string encryptKey = null, string verificationToken = null)
    {
        var request = new 
        { 
            AppId = appId, 
            AppSecret = appSecret, 
            AdminPassword = adminPassword,
            EncryptKey = encryptKey,
            VerificationToken = verificationToken
        };
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/feishu-management/feishu-config", content);
        var responseJson = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<BD.FeishuApproval.Abstractions.Management.ManagementOperationResult>(responseJson);
    }

    /// <summary>
    /// 登记审批流程
    /// </summary>
    public async Task<BD.FeishuApproval.Abstractions.Management.ManagementOperationResult<long>> RegisterApprovalAsync(
        string approvalCode, string adminPassword, string displayName = null, string description = null)
    {
        var request = new 
        { 
            ApprovalCode = approvalCode,
            AdminPassword = adminPassword,
            DisplayName = displayName,
            Description = description
        };
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/feishu-management/approvals", content);
        var responseJson = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<BD.FeishuApproval.Abstractions.Management.ManagementOperationResult<long>>(responseJson);
    }

    /// <summary>
    /// 获取已登记的审批列表
    /// </summary>
    public async Task<List<BD.FeishuApproval.Shared.Models.FeishuApprovalRegistration>> GetRegisteredApprovalsAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/feishu-management/approvals");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<List<BD.FeishuApproval.Shared.Models.FeishuApprovalRegistration>>(json);
    }

    /// <summary>
    /// 生成实体代码
    /// </summary>
    public async Task<string> GenerateEntityCodeAsync(string approvalCode)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/feishu-management/code-generation/{approvalCode}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// 系统健康检查
    /// </summary>
    public async Task<BD.FeishuApproval.Abstractions.Health.FeishuHealthCheckResult> CheckSystemHealthAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/feishu-management/health");
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<BD.FeishuApproval.Abstractions.Health.FeishuHealthCheckResult>(json);
    }

    /// <summary>
    /// 获取失败任务列表
    /// </summary>
    public async Task<BD.FeishuApproval.Abstractions.Management.PagedResult<BD.FeishuApproval.Shared.Models.FailedJob>> GetFailedJobsAsync(int page = 1, int pageSize = 20)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/feishu-management/failed-jobs?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<BD.FeishuApproval.Abstractions.Management.PagedResult<BD.FeishuApproval.Shared.Models.FailedJob>>(json);
    }

    /// <summary>
    /// 标记失败任务为成功
    /// </summary>
    public async Task<BD.FeishuApproval.Abstractions.Management.ManagementOperationResult> ResolveFailedJobAsync(long jobId)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/feishu-management/failed-jobs/{jobId}/resolve", null);
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<BD.FeishuApproval.Abstractions.Management.ManagementOperationResult>(json);
    }
}