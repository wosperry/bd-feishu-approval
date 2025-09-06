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
}


