using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Common.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public Codes Code { get; }

    protected AppException(string message, int statusCode, Codes code) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message, statusCode: 401, code: Codes.UNAUTHORIZED)
    {
    }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message, statusCode: 403, code: Codes.FORBIDDEN)
    {
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, statusCode: 404, code: Codes.NOT_FOUND)
    {
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, statusCode: 409, code: Codes.CONFLICT)
    {
    }
}