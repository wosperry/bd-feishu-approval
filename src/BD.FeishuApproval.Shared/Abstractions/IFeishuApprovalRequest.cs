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
    
    /// <summary>
    /// 获取审批类型，用于策略模式匹配
    /// 通过反射从ApprovalCodeAttribute.Type或ApprovalTypeAttribute中获取
    /// </summary>
    string GetApprovalType();
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
/// 审批类型特性定义
/// 专门用于标记审批处理策略类型
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ApprovalTypeAttribute : Attribute
{
    public string Type { get; }
    
    /// <summary>
    /// 策略描述
    /// </summary>
    public string? Description { get; set; }

    public ApprovalTypeAttribute(string type)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
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
    
    /// <summary>
    /// 获取审批类型，用于策略模式匹配
    /// 优先使用ApprovalTypeAttribute，其次使用ApprovalCodeAttribute.Type，最后使用审批代码作为类型
    /// </summary>
    public virtual string GetApprovalType()
    {
        // 优先使用专门的ApprovalTypeAttribute
        var typeAttribute = GetType().GetCustomAttributes(typeof(ApprovalTypeAttribute), false)
                                   .FirstOrDefault() as ApprovalTypeAttribute;
        if (typeAttribute != null)
        {
            return typeAttribute.Type;
        }
        
        // 其次使用ApprovalCodeAttribute中的Type属性
        var codeAttribute = GetType().GetCustomAttributes(typeof(ApprovalCodeAttribute), false)
                                   .FirstOrDefault() as ApprovalCodeAttribute;
        if (codeAttribute?.Type != null)
        {
            return codeAttribute.Type;
        }
        
        // 最后使用审批代码作为类型标识
        return GetApprovalCode();
    }
}