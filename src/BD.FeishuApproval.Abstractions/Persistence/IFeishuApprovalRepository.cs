using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BD.FeishuApproval.Shared.Events;
using BD.FeishuApproval.Shared.Models;

namespace BD.FeishuApproval.Abstractions.Persistence;

/// <summary>
/// 审批相关的持久化仓储抽象。
/// 包含：
/// - 请求/响应日志
/// - 失败任务（可重试）
/// - 关键状态事件
/// - 初始化建表
/// </summary>
public interface IFeishuApprovalRepository
{
    /// <summary>
    /// 初始化数据库表结构
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化任务</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除并重新创建所有数据库表（用于修复ID自增问题）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重建任务</returns>
    Task DropAndRecreateTablesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存请求日志
    /// </summary>
    /// <param name="log">请求日志对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存任务</returns>
    Task SaveRequestLogAsync(FeishuRequestLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存响应日志
    /// </summary>
    /// <param name="log">响应日志对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存任务</returns>
    Task SaveResponseLogAsync(FeishuResponseLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加失败任务记录
    /// </summary>
    /// <param name="job">失败任务对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>失败任务ID</returns>
    Task<int> AddFailedJobAsync(FailedJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记失败任务为成功
    /// </summary>
    /// <param name="jobId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>标记任务</returns>
    Task MarkFailedJobSucceededAsync(int jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询失败任务列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>失败任务列表</returns>
    Task<IEnumerable<FailedJob>> QueryFailedJobsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询请求日志
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>请求日志和总数</returns>
    Task<(IEnumerable<FeishuRequestLog> Items, int Total)> QueryRequestLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询响应日志
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应日志和总数</returns>
    Task<(IEnumerable<FeishuResponseLog> Items, int Total)> QueryResponseLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存审批状态变更事件
    /// </summary>
    /// <param name="evt">事件对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存任务</returns>
    Task SaveEventAsync(ApprovalStatusChangedEvent evt, CancellationToken cancellationToken = default);

    // 配置管理
    /// <summary>
    /// 获取飞书访问配置
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问配置对象</returns>
    Task<FeishuAccessConfig> GetAccessConfigAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// 插入或更新访问配置
    /// </summary>
    /// <param name="config">配置对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新任务</returns>
    Task UpsertAccessConfigAsync(FeishuAccessConfig config, CancellationToken cancellationToken = default);

    // 仪表盘访问口令（仅哈希）
    /// <summary>
    /// 设置管理员密码
    /// </summary>
    /// <param name="password">密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设置任务</returns>
    Task SetAdminPasswordAsync(string password, CancellationToken cancellationToken = default);
    /// <summary>
    /// 验证管理员密码
    /// </summary>
    /// <param name="password">密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<bool> VerifyAdminPasswordAsync(string password, CancellationToken cancellationToken = default);

    // 审批登记
    /// <summary>
    /// 登记审批流程
    /// </summary>
    /// <param name="reg">审批登记对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登记ID</returns>
    Task<int> RegisterApprovalAsync(FeishuApprovalRegistration reg, CancellationToken cancellationToken = default);
    /// <summary>
    /// 查询已登记的审批流程列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审批登记列表</returns>
    Task<IEnumerable<FeishuApprovalRegistration>> ListApprovalsAsync(CancellationToken cancellationToken = default);

    // 管理日志
    /// <summary>
    /// 保存管理操作日志
    /// </summary>
    /// <param name="log">管理日志对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存任务</returns>
    Task SaveManageLogAsync(FeishuManageLog log, CancellationToken cancellationToken = default);
    /// <summary>
    /// 查询管理操作日志
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>管理日志列表</returns>
    Task<IEnumerable<FeishuManageLog>> QueryManageLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<FeishuUser> GetUserAsync(int userId);
    Task UpdateUserOpenIdAsync(int id, string openId);

    // OpenId缓存管理
    /// <summary>
    /// 通过手机号获取缓存的OpenId
    /// </summary>
    Task<string> GetOpenIdByMobileAsync(string mobile);
    
    /// <summary>
    /// 通过邮箱获取缓存的OpenId
    /// </summary>
    Task<string> GetOpenIdByEmailAsync(string email);
    
    /// <summary>
    /// 通过UnionId获取缓存的OpenId
    /// </summary>
    Task<string> GetOpenIdByUnionIdAsync(string unionId);
    
    /// <summary>
    /// 通过UserId获取缓存的OpenId
    /// </summary>
    Task<string> GetOpenIdByUserIdAsync(string userId);

    /// <summary>
    /// 缓存OpenId（通过手机号）
    /// </summary>
    Task CacheOpenIdByMobileAsync(string mobile, string openId);
    
    /// <summary>
    /// 缓存OpenId（通过邮箱）
    /// </summary>
    Task CacheOpenIdByEmailAsync(string email, string openId);
    
    /// <summary>
    /// 缓存OpenId（通过UnionId）
    /// </summary>
    Task CacheOpenIdByUnionIdAsync(string unionId, string openId);
    
    /// <summary>
    /// 缓存OpenId（通过UserId）
    /// </summary>
    Task CacheOpenIdByUserIdAsync(string userId, string openId);

    /// <summary>
    /// 清除OpenId缓存（通过手机号）
    /// </summary>
    Task ClearOpenIdCacheByMobileAsync(string mobile);
    
    /// <summary>
    /// 清除OpenId缓存（通过邮箱）
    /// </summary>
    Task ClearOpenIdCacheByEmailAsync(string email);
    
    /// <summary>
    /// 清除OpenId缓存（通过UnionId）
    /// </summary>
    Task ClearOpenIdCacheByUnionIdAsync(string unionId);
    
    /// <summary>
    /// 清除OpenId缓存（通过UserId）
    /// </summary>
    Task ClearOpenIdCacheByUserIdAsync(string userId);

    // 回调记录管理
    /// <summary>
    /// 保存回调记录
    /// </summary>
    /// <param name="record">回调记录对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>记录ID</returns>
    Task<int> SaveCallbackRecordAsync(FeishuCallbackRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新回调记录状态
    /// </summary>
    /// <param name="recordId">记录ID</param>
    /// <param name="status">状态</param>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新任务</returns>
    Task UpdateCallbackRecordStatusAsync(int recordId, string status, string message = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过事件ID获取回调记录
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回调记录</returns>
    Task<FeishuCallbackRecord> GetCallbackRecordByEventIdAsync(string eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待处理的回调记录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回调记录列表</returns>
    Task<IEnumerable<FeishuCallbackRecord>> GetPendingCallbackRecordsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询回调记录
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="status">状态过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回调记录列表</returns>
    Task<IEnumerable<FeishuCallbackRecord>> QueryCallbackRecordsAsync(int page, int pageSize, string status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加回调重试次数
    /// </summary>
    /// <param name="recordId">记录ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新任务</returns>
    Task IncrementCallbackRetryCountAsync(int recordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过实例代码获取审批类型
    /// </summary>
    /// <param name="instanceCode">实例代码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审批类型</returns>
    Task<string> GetApprovalTypeByInstanceCodeAsync(string instanceCode, CancellationToken cancellationToken = default);
}


