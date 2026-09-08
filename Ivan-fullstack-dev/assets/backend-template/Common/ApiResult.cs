namespace IvanProject.Common;

/// <summary>
/// 统一 API 响应格式（前端 request.ts 依赖此结构）
/// code: 0 成功；401 未认证；其余为业务错误
/// </summary>
public class ApiResult<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = "ok";
    public T? Data { get; set; }

    public static ApiResult<T> Ok(T data, string message = "ok") =>
        new() { Code = 0, Message = message, Data = data };

    public static ApiResult<T> Fail(string message, int code = 500) =>
        new() { Code = code, Message = message };
}

/// <summary>
/// 无数据返回时使用的简写
/// </summary>
public class ApiResult
{
    public int Code { get; set; }
    public string Message { get; set; } = "ok";

    public static ApiResult Ok(string message = "ok") => new() { Code = 0, Message = message };
    public static ApiResult Fail(string message, int code = 500) => new() { Code = code, Message = message };
}
