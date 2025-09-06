using System.Text.Json.Serialization;
using BD.FeishuApproval.Shared.Abstractions;

namespace FeishuApproval.SampleWeb.FeishuApprovals.DemoApproval;

/// <summary>
/// Demo 审批请求 DTO
/// 
/// 说明：
/// - 这是根据审批流定义生成的参数类
/// - 属性名称与飞书表单字段绑定（通过 JsonPropertyName）
/// - 开发者不需要修改此类，除非审批流定义发生变化
/// </summary>
[ApprovalCode("6A109ECD-3578-4243-93F9-DBDCF89515AF")]
public class DemoApprovalDto : FeishuApprovalRequestBase
{
    /// <summary>
    /// 姓名 (飞书表单: input)
    /// </summary>
    [JsonPropertyName("widget17570011196430001")]
    public string 姓名 { get; set; } = string.Empty;

    /// <summary>
    /// 年龄 (岁) (飞书表单: number)
    /// </summary>
    [JsonPropertyName("widget17570011375970001")]
    public int 年龄_岁 { get; set; } = 0;
}