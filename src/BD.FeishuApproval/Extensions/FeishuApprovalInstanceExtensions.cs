using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Shared.Abstractions;
using BD.FeishuApproval.Shared.Dtos.Instances;
using BD.FeishuApproval.Shared.Models;

namespace BD.FeishuApproval.Extensions;

/// <summary>
/// 飞书审批实例服务扩展方法
/// 提供类型安全的审批创建方法
/// </summary>
public static class FeishuApprovalInstanceExtensions
{
    /// <summary>
    /// 创建类型安全的审批实例
    /// 使用强类型的审批请求，自动从特性获取审批代码
    /// </summary>
    /// <typeparam name="T">实现了IFeishuApprovalRequest的审批请求类型</typeparam>
    /// <param name="instanceService">审批实例服务</param>
    /// <param name="request">审批请求数据</param>
    /// <param name="jsonOptions">JSON序列化选项</param>
    /// <returns>创建结果</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当类型缺少ApprovalCodeAttribute时抛出</exception>
    public static async Task<FeishuResponse<CreateInstanceResult>> CreateTypedInstanceAsync<T>(
        this IFeishuApprovalInstanceService instanceService,
        T request,
        JsonSerializerOptions? jsonOptions = null)
        where T : class, IFeishuApprovalRequest
    {
        if (instanceService == null)
            throw new ArgumentNullException(nameof(instanceService));
        
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // 从类型特性获取审批代码
        var approvalCode = request.GetApprovalCode();
        
        // 序列化请求数据
        var formData = JsonSerializer.Serialize(request, jsonOptions);
        
        // 调用原始的创建方法
        return await instanceService.CreateInstanceAsync(new CreateInstanceRequest
        {
            ApprovalCode = approvalCode,
            FormData = formData
        });
    }

    /// <summary>
    /// 批量创建类型安全的审批实例
    /// 使用强类型的审批请求列表
    /// </summary>
    /// <typeparam name="T">实现了IFeishuApprovalRequest的审批请求类型</typeparam>
    /// <param name="instanceService">审批实例服务</param>
    /// <param name="requests">审批请求数据列表</param>
    /// <param name="jsonOptions">JSON序列化选项</param>
    /// <returns>创建结果列表</returns>
    public static async Task<List<FeishuResponse<CreateInstanceResult>>> CreateTypedInstancesAsync<T>(
        this IFeishuApprovalInstanceService instanceService,
        IEnumerable<T> requests,
        JsonSerializerOptions? jsonOptions = null)
        where T : class, IFeishuApprovalRequest
    {
        if (instanceService == null)
            throw new ArgumentNullException(nameof(instanceService));
        
        if (requests == null)
            throw new ArgumentNullException(nameof(requests));

        var results = new List<FeishuResponse<CreateInstanceResult>>();
        
        foreach (var request in requests)
        {
            var result = await CreateTypedInstanceAsync(instanceService, request, jsonOptions);
            results.Add(result);
        }
        
        return results;
    }
}