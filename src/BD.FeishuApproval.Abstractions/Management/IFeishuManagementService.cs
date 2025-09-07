using BD.FeishuApproval.Abstractions.Health;
using BD.FeishuApproval.Shared.Dtos.Definitions;
using BD.FeishuApproval.Shared.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Management;

/// <summary>
/// 飞书管理服务接口，提供所有管理界面功能的编程接口
/// </summary>
public interface IFeishuManagementService
{
    /// <summary>
    /// 获取系统配置状态
    /// </summary>
    /// <returns>配置状态信息</returns>
    Task<ConfigurationStatus> GetConfigurationStatusAsync(bool needPassword = true);

    /// <summary>
    /// 设置管理员密码
    /// </summary>
    /// <param name="password">新密码</param>
    /// <returns>操作结果</returns>
    Task<ManagementOperationResult> SetAdminPasswordAsync(string password);

    /// <summary>
    /// 验证管理员密码
    /// </summary>
    /// <param name="password">密码</param>
    /// <returns>验证结果</returns>
    Task<bool> VerifyAdminPasswordAsync(string password);

    /// <summary>
    /// 保存飞书应用配置
    /// </summary>
    /// <param name="config">配置信息</param>
    /// <param name="adminPassword">管理员密码</param>
    /// <returns>操作结果</returns>
    Task<ManagementOperationResult> SaveFeishuConfigAsync(FeishuConfigRequest config, string adminPassword);

    /// <summary>
    /// 登记审批流程
    /// </summary>
    /// <param name="registration">审批登记信息</param>
    /// <param name="adminPassword">管理员密码</param>
    /// <returns>操作结果</returns>
    Task<ManagementOperationResult<int>> RegisterApprovalAsync(ApprovalRegistrationRequest registration, string adminPassword);

    /// <summary>
    /// 获取已登记的审批流程列表
    /// </summary>
    /// <returns>审批流程列表</returns>
    Task<List<FeishuApprovalRegistration>> GetRegisteredApprovalsAsync();

    /// <summary>
    /// 生成审批实体类代码
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <returns>生成的C#代码</returns>
    Task<string> GenerateEntityCodeAsync(string approvalCode);

    /// <summary>
    /// 生成TypeScript接口代码
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <param name="interfaceName">接口名称（可选）</param>
    /// <returns>生成的TypeScript代码</returns>
    Task<string> GenerateTypeScriptCodeAsync(string approvalCode, string interfaceName = null);

    /// <summary>
    /// 订阅审批定义更新
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <param name="adminPassword">管理员密码</param>
    /// <returns>操作结果</returns>
    Task<ManagementOperationResult> SubscribeApprovalAsync(string approvalCode, string adminPassword);

    /// <summary>
    /// 获取审批定义详情
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <returns>审批定义详情</returns>
    Task<ApprovalDefinitionDetail> GetApprovalDefinitionAsync(string approvalCode);

    /// <summary>
    /// 查询失败任务列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>失败任务列表</returns>
    Task<PagedResult<FailedJob>> GetFailedJobsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// 标记失败任务为成功
    /// </summary>
    /// <param name="jobId">任务ID</param>
    /// <returns>操作结果</returns>
    Task<ManagementOperationResult> ResolveFailedJobAsync(int jobId);

    /// <summary>
    /// 查询请求日志
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>请求日志列表</returns>
    Task<PagedResult<FeishuRequestLog>> GetRequestLogsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// 查询响应日志
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>响应日志列表</returns>
    Task<PagedResult<FeishuResponseLog>> GetResponseLogsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// 查询管理操作日志
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>管理操作日志列表</returns>
    Task<PagedResult<FeishuManageLog>> GetManageLogsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// 系统健康检查
    /// </summary>
    /// <returns>健康检查结果</returns>
    Task<FeishuHealthCheckResult> CheckSystemHealthAsync();

    string DecryptFeishuData(string encryptedData, string encryptKey);


    /// <summary>
    /// 处理解密后的飞书事件数据
    /// </summary>
    /// <param name="decryptedEventData">解密后的事件JSON字符串</param>
    /// <returns>事件处理结果</returns>
    Task<FeishuEventHandleResult> ProcessFeishuEventAsync(string decryptedEventData);

    /// <summary>
    /// 处理审批事件
    /// </summary>
    /// <param name="eventData">审批事件数据</param>
    /// <returns>处理结果</returns>
    Task<FeishuEventHandleResult> ProcessApprovalEventAsync(JObject eventData);

    /// <summary>
    /// 处理消息事件
    /// </summary>
    /// <param name="eventData">消息事件数据</param>
    /// <returns>处理结果</returns>
    Task<FeishuEventHandleResult> ProcessMessageEventAsync(JObject eventData);
}

/// <summary>
/// 飞书请求处理结果
/// </summary>
public class FeishuRequestHandleResult
{
    /// <summary>
    /// 是否处理成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息（处理失败时）
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// 飞书验证所需的challenge（URL验证时）
    /// </summary>
    public string Challenge { get; set; }

    /// <summary>
    /// 事件处理结果（事件推送时）
    /// </summary>
    public object EventResult { get; set; }
}

/// <summary>
/// 管理操作结果
/// </summary>
public class ManagementOperationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperationTime { get; set; } = DateTime.Now;

    public static ManagementOperationResult Success(string details = null)
    {
        return new ManagementOperationResult
        {
            IsSuccess = true,
            Details = details
        };
    }

    public static ManagementOperationResult Failure(string errorMessage, string details = null)
    {
        return new ManagementOperationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Details = details
        };
    }
}

/// <summary>
/// 管理操作结果（带返回数据）
/// </summary>
/// <typeparam name="T">返回数据类型</typeparam>
public class ManagementOperationResult<T> : ManagementOperationResult
{
    /// <summary>
    /// 返回数据
    /// </summary>
    public T Data { get; set; }

    public static ManagementOperationResult<T> Success(T data, string details = null)
    {
        return new ManagementOperationResult<T>
        {
            IsSuccess = true,
            Data = data,
            Details = details
        };
    }

    public static new ManagementOperationResult<T> Failure(string errorMessage, string details = null)
    {
        return new ManagementOperationResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Details = details
        };
    }
}

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;
}


/// <summary>
/// 飞书配置请求模型
/// </summary>
public class FeishuConfigRequest
{
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public string EncryptKey { get; set; }
    public string VerificationToken { get; set; }
}

/// <summary>
/// 审批登记请求模型
/// </summary>
public class ApprovalRegistrationRequest
{
    public string ApprovalCode { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// 系统配置状态
/// </summary>
public class ConfigurationStatus
{
    /// <summary>
    /// 是否已配置飞书应用
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// 是否设置了管理员密码
    /// </summary>
    public bool HasAdminPassword { get; set; }

    /// <summary>
    /// 掩码后的AppId
    /// </summary>
    public string AppIdMasked { get; set; }

    /// <summary>
    /// 配置完成度百分比
    /// </summary>
    public int CompletionPercentage { get; set; }

    /// <summary>
    /// 飞书完整配置（内部使用）
    /// </summary>
    public FeishuAccessConfig FeishuConfig { get; set; }
}
public class FeishuEventHandleResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string EventType { get; set; }
    public string EventId { get; set; }
    public object Data { get; set; }
}