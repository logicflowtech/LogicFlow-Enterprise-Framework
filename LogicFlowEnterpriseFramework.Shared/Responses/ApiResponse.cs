namespace LogicFlowEnterpriseFramework.Shared.Responses;

public sealed record ApiResponse<T>(bool Succeeded, T? Data, string? Message, IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static ApiResponse<T> Success(T data, string? message = null) => new(true, data, message, null);
    public static ApiResponse<T> Failure(string message, IReadOnlyDictionary<string, string[]>? errors = null) => new(false, default, message, errors);
}
