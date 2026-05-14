namespace Cis.Contracts;

public sealed record ApiResponse<T>(T Data, ApiResponseMeta Meta)
{
    public static ApiResponse<T> Success(T data, string? correlationId = null)
    {
        return new ApiResponse<T>(data, new ApiResponseMeta(correlationId, DateTime.UtcNow));
    }
}

public sealed record ApiResponseMeta(string? CorrelationId, DateTime TimestampUtc);
