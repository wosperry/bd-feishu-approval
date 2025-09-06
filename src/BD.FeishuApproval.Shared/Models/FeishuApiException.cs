using System;

namespace BD.FeishuApproval.Shared.Models;

public class FeishuApiException : Exception
{
    public int Code { get; }
    public string ApiPath { get; }

    public FeishuApiException(string message, int code = -1, string apiPath = null, Exception inner = null)
        : base(message, inner)
    {
        Code = code;
        ApiPath = apiPath;
    }
}


