using System.Text.Json.Serialization;

namespace BD.FeishuApproval.Shared.Dtos.Definitions;

/// <summary>
/// 表单组件
/// </summary>
public class FormWidget
{
    /// <summary>
    /// 默认值类型
    /// </summary>
    [JsonPropertyName("default_value_type")]
    public string DefaultValueType { get; set; }

    /// <summary>
    /// 显示条件
    /// </summary>
    [JsonPropertyName("display_condition")]
    public object DisplayCondition { get; set; }

    /// <summary>
    /// 是否启用默认值
    /// </summary>
    [JsonPropertyName("enable_default_value")]
    public bool EnableDefaultValue { get; set; }

    /// <summary>
    /// 组件ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// 组件名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// 是否可打印
    /// </summary>
    [JsonPropertyName("printable")]
    public bool Printable { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; }

    /// <summary>
    /// 组件类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; set; }

    /// <summary>
    /// 组件默认值
    /// </summary>
    [JsonPropertyName("widget_default_value")]
    public string WidgetDefaultValue { get; set; }
}