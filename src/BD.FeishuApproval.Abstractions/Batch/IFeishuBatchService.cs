using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BD.FeishuApproval.Shared.Dtos.Instances;

namespace BD.FeishuApproval.Abstractions.Batch;

/// <summary>
/// 飞书批量操作服务接口
/// </summary>
public interface IFeishuBatchService
{
    /// <summary>
    /// 批量创建审批实例
    /// </summary>
    /// <param name="requests">批量创建请求</param>
    /// <returns>批量操作结果</returns>
    Task<BatchOperationResult<CreateInstanceResult>> BatchCreateInstancesAsync(IEnumerable<CreateInstanceRequest> requests);
    
    /// <summary>
    /// 批量查询审批状态
    /// </summary>
    /// <param name="instanceCodes">审批实例代码列表</param>
    /// <returns>批量查询结果</returns>
    Task<BatchOperationResult<BatchInstanceStatus>> BatchGetInstanceStatusAsync(IEnumerable<string> instanceCodes);
    
    /// <summary>
    /// 批量撤销审批
    /// </summary>
    /// <param name="instanceCodes">审批实例代码列表</param>
    /// <param name="reason">撤销原因</param>
    /// <returns>批量操作结果</returns>
    Task<BatchOperationResult<bool>> BatchCancelInstancesAsync(IEnumerable<string> instanceCodes, string reason = "");
}

/// <summary>
/// 批量操作结果
/// </summary>
/// <typeparam name="T">结果数据类型</typeparam>
public class BatchOperationResult<T>
{
    /// <summary>
    /// 成功项目
    /// </summary>
    public List<BatchOperationItem<T>> SuccessItems { get; set; } = new();
    
    /// <summary>
    /// 失败项目
    /// </summary>
    public List<BatchOperationError> FailedItems { get; set; } = new();
    
    /// <summary>
    /// 总数
    /// </summary>
    public int TotalCount => SuccessItems.Count + FailedItems.Count;
    
    /// <summary>
    /// 成功数
    /// </summary>
    public int SuccessCount => SuccessItems.Count;
    
    /// <summary>
    /// 失败数
    /// </summary>
    public int FailedCount => FailedItems.Count;
    
    /// <summary>
    /// 是否全部成功
    /// </summary>
    public bool IsAllSuccess => FailedItems.Count == 0;
}

/// <summary>
/// 批量操作项目
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class BatchOperationItem<T>
{
    /// <summary>
    /// 索引
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// 数据
    /// </summary>
    public T Data { get; set; }
    
    /// <summary>
    /// 额外信息
    /// </summary>
    public string ExtraInfo { get; set; }
}

/// <summary>
/// 批量操作错误
/// </summary>
public class BatchOperationError
{
    /// <summary>
    /// 索引
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    /// 错误代码
    /// </summary>
    public string ErrorCode { get; set; }
}

/// <summary>
/// 审批实例状态
/// </summary>
public class BatchInstanceStatus
{
    /// <summary>
    /// 实例代码
    /// </summary>
    public string InstanceCode { get; set; }
    
    /// <summary>
    /// 审批状态
    /// </summary>
    public string Status { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; }
}