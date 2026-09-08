namespace IvanProject.Common;

/// <summary>
/// 业务异常：BLL 中校验失败时抛出，由 ExceptionMiddleware 统一转换为 ApiResult
/// </summary>
public class BusinessException : Exception
{
    public int Code { get; }

    public BusinessException(string message, int code = 400) : base(message)
    {
        Code = code;
    }
}
