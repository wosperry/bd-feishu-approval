using BD.FeishuApproval.Shared.Dtos.Instances;
using BD.FeishuApproval.Shared.Models;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Abstractions.Instances;

/// <summary>
/// 飞书审批实例服务接口，负责审批流程实例的全生命周期管理
/// </summary>
public interface IFeishuApprovalInstanceService
{
    /// <summary>
    /// 创建审批实例（发起审批）
    /// </summary>
    /// <param name="request">实例创建请求</param>
    /// <returns>创建结果（含instance_code）</returns>
    Task<FeishuResponse<CreateInstanceResult>> CreateInstanceAsync(CreateInstanceRequest request);

    /// <summary>
    /// 查询审批实例详情
    /// </summary>
    /// <param name="instanceCode">实例标识</param>
    /// <returns>实例详情</returns>
    Task<FeishuResponse<InstanceDetail>> GetInstanceDetailAsync(string instanceCode);

    /// <summary>
    /// 同步审批实例状态
    /// </summary>
    /// <param name="instanceCode">实例标识</param>
    /// <returns>同步后的状态信息</returns>
    Task<FeishuResponse<InstanceStatus>> SyncInstanceStatusAsync(string instanceCode);

    /// <summary>
    /// 终止审批实例
    /// </summary>
    /// <param name="instanceCode">实例标识</param>
    /// <param name="reason">终止原因</param>
    /// <returns>操作结果</returns>
    Task<FeishuResponse> TerminateInstanceAsync(string instanceCode, string reason);
}
