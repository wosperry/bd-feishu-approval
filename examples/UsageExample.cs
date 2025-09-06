using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Shared.Dtos.Instances;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Examples;

/// <summary>
/// 飞书审批服务使用示例
/// 演示如何使用不同类型的UserId创建审批实例
/// </summary>
public class ApprovalServiceUsageExample
{
    private readonly IFeishuApprovalInstanceService _approvalService;
    private readonly ILogger<ApprovalServiceUsageExample> _logger;

    public ApprovalServiceUsageExample(
        IFeishuApprovalInstanceService approvalService,
        ILogger<ApprovalServiceUsageExample> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    /// <summary>
    /// 演示使用手机号创建审批实例
    /// </summary>
    public async Task<string> CreateApprovalWithMobileAsync()
    {
        try
        {
            var request = new CreateInstanceRequest
            {
                ApprovalCode = "approval_001",
                ApplicantUserId = "13800138000", // 手机号
                UserIdType = "mobile", // 明确指定类型
                FormData = """
                {
                    "申请人": "张三",
                    "申请理由": "请假一天",
                    "申请日期": "2024-01-15"
                }
                """
            };

            _logger.LogInformation("创建审批实例 - 使用手机号: {Mobile}", request.ApplicantUserId);

            var result = await _approvalService.CreateInstanceAsync(request);

            _logger.LogInformation("审批实例创建成功 - 实例ID: {InstanceCode}", result.Data?.InstanceCode);
            return result.Data?.InstanceCode ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用手机号创建审批实例失败");
            throw;
        }
    }

    /// <summary>
    /// 演示使用邮箱创建审批实例
    /// </summary>
    public async Task<string> CreateApprovalWithEmailAsync()
    {
        try
        {
            var request = new CreateInstanceRequest
            {
                ApprovalCode = "approval_002",
                ApplicantUserId = "zhangsan@company.com", // 邮箱
                UserIdType = "email", // 明确指定类型
                FormData = """
                {
                    "申请人": "张三",
                    "申请内容": "报销费用",
                    "金额": 1000
                }
                """
            };

            _logger.LogInformation("创建审批实例 - 使用邮箱: {Email}", request.ApplicantUserId);

            var result = await _approvalService.CreateInstanceAsync(request);

            _logger.LogInformation("审批实例创建成功 - 实例ID: {InstanceCode}", result.Data?.InstanceCode);
            return result.Data?.InstanceCode ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用邮箱创建审批实例失败");
            throw;
        }
    }

    /// <summary>
    /// 演示使用飞书UserId创建审批实例
    /// </summary>
    public async Task<string> CreateApprovalWithUserIdAsync()
    {
        try
        {
            var request = new CreateInstanceRequest
            {
                ApprovalCode = "approval_003",
                ApplicantUserId = "1234567890", // 飞书用户ID
                UserIdType = "user_id", // 明确指定类型
                FormData = """
                {
                    "申请类型": "差旅申请",
                    "出差地点": "北京",
                    "出差时间": "2024-01-20 至 2024-01-22"
                }
                """
            };

            _logger.LogInformation("创建审批实例 - 使用飞书用户ID: {UserId}", request.ApplicantUserId);

            var result = await _approvalService.CreateInstanceAsync(request);

            _logger.LogInformation("审批实例创建成功 - 实例ID: {InstanceCode}", result.Data?.InstanceCode);
            return result.Data?.InstanceCode ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用飞书用户ID创建审批实例失败");
            throw;
        }
    }

    /// <summary>
    /// 演示使用自动识别创建审批实例（推荐方式）
    /// </summary>
    public async Task<string> CreateApprovalWithAutoDetectAsync(string userId)
    {
        try
        {
            var request = new CreateInstanceRequest
            {
                ApprovalCode = "approval_004",
                ApplicantUserId = userId, // 可以是手机号、邮箱、用户ID等任意格式
                // UserIdType 不设置或设置为 "auto"，系统会自动识别
                FormData = """
                {
                    "申请人": "自动识别用户",
                    "申请内容": "测试自动识别功能"
                }
                """
            };

            _logger.LogInformation("创建审批实例 - 自动识别用户ID类型: {UserId}", request.ApplicantUserId);

            var result = await _approvalService.CreateInstanceAsync(request);

            _logger.LogInformation("审批实例创建成功 - 实例ID: {InstanceCode}", result.Data?.InstanceCode);
            return result.Data?.InstanceCode ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用自动识别创建审批实例失败");
            throw;
        }
    }

    /// <summary>
    /// 演示批量创建审批实例
    /// </summary>
    public async Task<string[]> CreateMultipleApprovalsAsync()
    {
        var results = new List<string>();

        try
        {
            // 不同类型用户ID的审批请求
            var requests = new[]
            {
                new CreateInstanceRequest
                {
                    ApprovalCode = "batch_001",
                    ApplicantUserId = "13800138001", // 手机号
                    FormData = """{"申请人": "用户1", "类型": "请假"}"""
                },
                new CreateInstanceRequest
                {
                    ApprovalCode = "batch_002", 
                    ApplicantUserId = "user2@company.com", // 邮箱
                    FormData = """{"申请人": "用户2", "类型": "报销"}"""
                },
                new CreateInstanceRequest
                {
                    ApprovalCode = "batch_003",
                    ApplicantUserId = "ou_12345678", // UnionId
                    UserIdType = "union_id",
                    FormData = """{"申请人": "用户3", "类型": "差旅"}"""
                }
            };

            _logger.LogInformation("开始批量创建 {Count} 个审批实例", requests.Length);

            foreach (var request in requests)
            {
                try
                {
                    var result = await _approvalService.CreateInstanceAsync(request);
                    if (!string.IsNullOrEmpty(result.Data?.InstanceCode))
                    {
                        results.Add(result.Data.InstanceCode);
                        _logger.LogInformation("审批实例创建成功 - 用户: {UserId}, 实例ID: {InstanceCode}", 
                            request.ApplicantUserId, result.Data.InstanceCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "创建审批实例失败 - 用户: {UserId}", request.ApplicantUserId);
                    // 继续处理下一个请求
                }

                // 添加延迟避免API调用过于频繁
                await Task.Delay(100);
            }

            _logger.LogInformation("批量创建完成 - 成功: {SuccessCount}/{TotalCount}", results.Count, requests.Length);
            return results.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量创建审批实例失败");
            throw;
        }
    }

    /// <summary>
    /// 演示查询审批实例详情
    /// </summary>
    public async Task<InstanceDetail?> GetApprovalDetailAsync(string instanceCode)
    {
        try
        {
            _logger.LogInformation("查询审批实例详情 - 实例ID: {InstanceCode}", instanceCode);

            var result = await _approvalService.GetInstanceDetailAsync(instanceCode);
            
            _logger.LogInformation("审批实例详情查询成功 - 状态: {Status}", result.Data?.Status);
            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询审批实例详情失败 - 实例ID: {InstanceCode}", instanceCode);
            throw;
        }
    }

    /// <summary>
    /// 演示完整的审批流程：创建 -> 查询 -> 同步状态
    /// </summary>
    public async Task<bool> CompleteApprovalWorkflowAsync(string userId, string approvalCode, string formData)
    {
        try
        {
            _logger.LogInformation("开始完整审批流程 - 用户: {UserId}, 审批代码: {ApprovalCode}", userId, approvalCode);

            // 1. 创建审批实例
            var createRequest = new CreateInstanceRequest
            {
                ApprovalCode = approvalCode,
                ApplicantUserId = userId, // 自动识别用户ID类型
                FormData = formData
            };

            var createResult = await _approvalService.CreateInstanceAsync(createRequest);
            var instanceCode = createResult.Data?.InstanceCode;

            if (string.IsNullOrEmpty(instanceCode))
            {
                _logger.LogError("创建审批实例失败 - 未返回实例ID");
                return false;
            }

            _logger.LogInformation("审批实例创建成功 - 实例ID: {InstanceCode}", instanceCode);

            // 2. 查询实例详情
            var detailResult = await _approvalService.GetInstanceDetailAsync(instanceCode);
            _logger.LogInformation("实例详情查询成功 - 当前状态: {Status}", detailResult.Data?.Status);

            // 3. 同步实例状态
            var statusResult = await _approvalService.SyncInstanceStatusAsync(instanceCode);
            _logger.LogInformation("实例状态同步成功 - 最新状态: {Status}", statusResult.Data?.Status);

            _logger.LogInformation("完整审批流程执行成功 - 实例ID: {InstanceCode}", instanceCode);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完整审批流程执行失败 - 用户: {UserId}", userId);
            return false;
        }
    }
}