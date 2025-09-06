using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BD.FeishuApproval.Abstractions.Batch;
using BD.FeishuApproval.Abstractions.Instances;
using BD.FeishuApproval.Shared.Dtos.Instances;
using Microsoft.Extensions.Logging;

namespace BD.FeishuApproval.Batch;

/// <summary>
/// 飞书批量操作服务实现
/// </summary>
public class FeishuBatchService : IFeishuBatchService
{
    private readonly IFeishuApprovalInstanceService _instanceService;
    private readonly ILogger<FeishuBatchService> _logger;

    public FeishuBatchService(
        IFeishuApprovalInstanceService instanceService,
        ILogger<FeishuBatchService> logger)
    {
        _instanceService = instanceService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BatchOperationResult<CreateInstanceResult>> BatchCreateInstancesAsync(IEnumerable<CreateInstanceRequest> requests)
    {
        var result = new BatchOperationResult<CreateInstanceResult>();
        var requestList = requests.ToList();
        
        _logger.LogInformation("Starting batch create for {Count} instances", requestList.Count);
        
        for (int i = 0; i < requestList.Count; i++)
        {
            try
            {
                var response = await _instanceService.CreateInstanceAsync(requestList[i]);
                
                if (response.IsSuccess)
                {
                    result.SuccessItems.Add(new BatchOperationItem<CreateInstanceResult>
                    {
                        Index = i,
                        Data = response.Data,
                        ExtraInfo = $"Created instance: {response.Data?.InstanceCode}"
                    });
                    _logger.LogDebug("Successfully created instance at index {Index}", i);
                }
                else
                {
                    result.FailedItems.Add(new BatchOperationError
                    {
                        Index = i,
                        ErrorMessage = response.Message,
                        ErrorCode = response.Code.ToString()
                    });
                    _logger.LogWarning("Failed to create instance at index {Index}: {Message}", i, response.Message);
                }
            }
            catch (Exception ex)
            {
                result.FailedItems.Add(new BatchOperationError
                {
                    Index = i,
                    ErrorMessage = ex.Message,
                    ErrorCode = "EXCEPTION"
                });
                _logger.LogError(ex, "Exception occurred while creating instance at index {Index}", i);
            }
        }
        
        _logger.LogInformation("Batch create completed: {Success}/{Total} successful", result.SuccessCount, result.TotalCount);
        return result;
    }

    /// <inheritdoc />
    public async Task<BatchOperationResult<BatchInstanceStatus>> BatchGetInstanceStatusAsync(IEnumerable<string> instanceCodes)
    {
        var result = new BatchOperationResult<BatchInstanceStatus>();
        var codeList = instanceCodes.ToList();
        
        _logger.LogInformation("Starting batch status check for {Count} instances", codeList.Count);
        
        for (int i = 0; i < codeList.Count; i++)
        {
            try
            {
                var response = await _instanceService.GetInstanceDetailAsync(codeList[i]);
                
                if (response.IsSuccess)
                {
                    var status = new BatchInstanceStatus
                    {
                        InstanceCode = codeList[i],
                        Status = response.Data?.Status ?? "Unknown",
                        CreateTime = response.Data?.CreateTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(response.Data.CreateTime).DateTime : DateTime.MinValue,
                        UpdateTime = (response.Data?.EndTime.HasValue == true && response.Data.EndTime > 0) ? DateTimeOffset.FromUnixTimeSeconds(response.Data.EndTime.Value).DateTime : DateTime.MinValue
                    };
                    
                    result.SuccessItems.Add(new BatchOperationItem<BatchInstanceStatus>
                    {
                        Index = i,
                        Data = status,
                        ExtraInfo = $"Status: {status.Status}"
                    });
                }
                else
                {
                    result.FailedItems.Add(new BatchOperationError
                    {
                        Index = i,
                        ErrorMessage = response.Message,
                        ErrorCode = response.Code.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                result.FailedItems.Add(new BatchOperationError
                {
                    Index = i,
                    ErrorMessage = ex.Message,
                    ErrorCode = "EXCEPTION"
                });
                _logger.LogError(ex, "Exception occurred while checking status for instance {Code} at index {Index}", codeList[i], i);
            }
        }
        
        _logger.LogInformation("Batch status check completed: {Success}/{Total} successful", result.SuccessCount, result.TotalCount);
        return result;
    }

    /// <inheritdoc />
    public async Task<BatchOperationResult<bool>> BatchCancelInstancesAsync(IEnumerable<string> instanceCodes, string reason = "")
    {
        var result = new BatchOperationResult<bool>();
        var codeList = instanceCodes.ToList();
        
        _logger.LogInformation("Starting batch cancel for {Count} instances", codeList.Count);
        
        for (int i = 0; i < codeList.Count; i++)
        {
            try
            {
                var response = await _instanceService.TerminateInstanceAsync(codeList[i], reason);
                
                if (response.IsSuccess)
                {
                    result.SuccessItems.Add(new BatchOperationItem<bool>
                    {
                        Index = i,
                        Data = true,
                        ExtraInfo = $"Cancelled instance: {codeList[i]}"
                    });
                }
                else
                {
                    result.FailedItems.Add(new BatchOperationError
                    {
                        Index = i,
                        ErrorMessage = response.Message,
                        ErrorCode = response.Code.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                result.FailedItems.Add(new BatchOperationError
                {
                    Index = i,
                    ErrorMessage = ex.Message,
                    ErrorCode = "EXCEPTION"
                });
                _logger.LogError(ex, "Exception occurred while cancelling instance {Code} at index {Index}", codeList[i], i);
            }
        }
        
        _logger.LogInformation("Batch cancel completed: {Success}/{Total} successful", result.SuccessCount, result.TotalCount);
        return result;
    }
}