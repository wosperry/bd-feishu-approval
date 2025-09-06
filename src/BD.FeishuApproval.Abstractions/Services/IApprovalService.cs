using BD.FeishuApproval.Shared.Abstractions;
using BD.FeishuApproval.Shared.Dtos.Instances;
using BD.FeishuApproval.Shared.Events;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Services;

/// <summary>
/// 统一审批服务接口
/// 提供类型安全的审批操作入口，基于策略模式自动分发到对应Handler
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// 创建审批实例
    /// 自动根据请求类型分发到对应的Handler处理
    /// </summary>
    /// <typeparam name="T">审批请求DTO类型</typeparam>
    /// <param name="request">审批请求数据</param>
    /// <returns>创建结果</returns>
    /// <exception cref="InvalidOperationException">当找不到对应Handler时抛出</exception>
    Task<CreateInstanceResult>  CreateApprovalAsync<T, TId>(T request, TId userId)
        where T : class, IFeishuApprovalRequest, new()
        where TId : struct;
    
    /// <summary>
    /// 处理审批回调事件
    /// 自动根据审批类型分发到对应的Handler处理
    /// </summary>
    /// <param name="callbackEvent">回调事件数据</param>
    /// <returns>处理任务</returns>
    /// <exception cref="InvalidOperationException">当找不到对应Handler时抛出</exception>
    Task HandleApprovalCallbackAsync(FeishuCallbackEvent callbackEvent);
    
    /// <summary>
    /// 根据审批类型处理回调事件
    /// </summary>
    /// <param name="approvalCode">审批类型标识</param>
    /// <param name="callbackEvent">回调事件数据</param>
    /// <returns>处理任务</returns>
    Task HandleApprovalCallbackAsync(string approvalCode, FeishuCallbackEvent callbackEvent);
    
    /// <summary>
    /// 检查指定审批类型是否已注册Handler
    /// </summary>
    /// <param name="approvalCode">审批类型标识</param>
    /// <returns>是否已注册</returns>
    bool IsApprovalCodeSupported(string approvalCode);
    
    /// <summary>
    /// 检查指定审批请求类型是否已注册Handler
    /// </summary>
    /// <typeparam name="T">审批请求DTO类型</typeparam>
    /// <returns>是否已注册</returns>
    bool IsApprovalCodeSupported<T>() where T : class, IFeishuApprovalRequest, new();
    
    /// <summary>
    /// 获取所有支持的审批类型
    /// </summary>
    /// <returns>审批类型列表</returns>
    string[] GetSupportedApprovalCodes();
}