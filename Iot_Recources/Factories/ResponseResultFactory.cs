
using Iot_Recources.Models;

namespace Iot_Recources.Factories;

public static class ResponseResultFactory
{
    public static ResponseResult Success(object result)
    {
        return new ResponseResult
        {
            Succeeded = true,
            Result = result
        };
    }
    public static ResponseResult Error(string error)
    {
        return new ResponseResult
        {
            Succeeded = false,
            Error = error
        };
    }
    public static ResponseResult<T> Success<T>(T result)
    {
        return new ResponseResult<T>
        {
            Succeeded = true,
            Result = result,
            Error = null
        };
    }
    public static ResponseResult<T> Error<T>(string error)
    {
        return new ResponseResult<T>
        {
            Succeeded = false,
            Result = default,
            Error = error
        };
    }
}

