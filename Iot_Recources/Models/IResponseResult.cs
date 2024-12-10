namespace Iot_Recources.Models;

public interface IResponseResult
{
    string? Error { get; set; }
    //object? Result { get; set; }
    bool Succeeded { get; set; }
}
public interface IResponseResult<T> : IResponseResult
{
    T? Result { get; set; }
}