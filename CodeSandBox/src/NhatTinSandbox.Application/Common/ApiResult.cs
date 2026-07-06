namespace NhatTinSandbox.Application.Common;

public static class ApiResult
{
    public static object Ok(object? data, string message = "")
        => new { success = true, message, data };

    public static object Fail(string message, object? data = null)
        => new { success = false, message, data = data ?? new { } };
}
