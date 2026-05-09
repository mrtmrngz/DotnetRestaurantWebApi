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