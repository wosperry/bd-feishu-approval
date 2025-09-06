using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Dtos.Instances;

/// <summary>
/// 创建审批实例的返回结果
/// </summary>
public class CreateInstanceResult
{
    /// <summary>
    /// 飞书审批实例唯一标识（instance_code）
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string InstanceCode { get; set; }
}
