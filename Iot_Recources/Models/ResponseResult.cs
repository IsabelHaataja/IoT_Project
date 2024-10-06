
namespace Iot_Recources.Models;

public class ResponseResult<T> : ResponseResult, IResponseResult<T>
{
    public new T? Result
    {
        get => (T?)Result;
        set => base.Result = value;
    }

}
public class ResponseResult : IResponseResult
{
    public bool Succeeded { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
}
