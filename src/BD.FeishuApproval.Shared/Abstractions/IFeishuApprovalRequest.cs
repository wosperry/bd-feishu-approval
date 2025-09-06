using System;
using System.Linq;

namespace BD.FeishuApproval.Shared.Abstractions;

/// <summary>
/// 飞书审批请求接口
/// 所有审批实体类都必须实现此接口，以确保类型安全
/// </summary>
public interface IFeishuApprovalRequest
{
    /// <summary>
    /// 获取关联的审批代码
    /// 通过反射从ApprovalCodeAttribute中获取
    /// </summary>
    string GetApprovalCode(); 
}

/// <summary>
/// ApprovalCode特性定义
/// 用于标记审批代码和类型的特性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ApprovalCodeAttribute : Attribute
{
    public string Code { get; }
    
    /// <summary>
    /// 审批类型，用于策略模式匹配
    /// 如果未指定，将使用Code作为类型标识
    /// </summary>
    public string? Type { get; set; }

    public ApprovalCodeAttribute(string code)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }
}

/// <summary>
/// IFeishuApprovalRequest接口的默认实现基类
/// 提供了GetApprovalCode的默认实现
/// </summary>
public abstract class FeishuApprovalRequestBase : IFeishuApprovalRequest
{
    /// <summary>
    /// 获取关联的审批代码
    /// 通过反射从ApprovalCodeAttribute中获取
    /// </summary>
    public virtual string GetApprovalCode()
    {
        var attribute = GetType().GetCustomAttributes(typeof(ApprovalCodeAttribute), false)
                              .FirstOrDefault() as ApprovalCodeAttribute;
        
        return attribute?.Code ?? throw new InvalidOperationException(
            $"类 {GetType().Name} 缺少 ApprovalCodeAttribute 特性");
    } 
}