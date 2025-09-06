using System;
using System.Collections.Generic;
using System.Linq;
using BD.FeishuApproval.Abstractions.Strategies;

namespace BD.FeishuApproval.Strategies;

public class ApprovalStrategyFactory : IApprovalStrategyFactory
{
    private readonly IReadOnlyDictionary<string, IApprovalStrategy> _strategies;

    public ApprovalStrategyFactory(IEnumerable<IApprovalStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.ApprovalType, StringComparer.OrdinalIgnoreCase);
    }

    public IApprovalStrategy GetStrategy(string approvalType)
    {
        if (!_strategies.TryGetValue(approvalType, out var strategy))
        {
            throw new InvalidOperationException($"Approval strategy not found: {approvalType}");
        }
        return strategy;
    }
}


