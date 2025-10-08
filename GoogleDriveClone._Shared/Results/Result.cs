using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDriveClone.SharedModels.Results;

public record Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        // Перевірка, щоб не можна було створити суперечливий результат
        if (isSuccess && error is not null || !isSuccess && error is null)
        {
            throw new InvalidOperationException();
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    // Дозволяє неявно перетворювати Error в Result
    public static implicit operator Result(Error error) => Failure(error);
}

public record Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) => Value = value;
    private Result(Error error) : base(false, error) { }

    // Дозволяє неявно перетворювати значення T в успішний Result<T>
    public static implicit operator Result<T>(T value) => new(value);

    // Дозволяє неявно перетворювати Error в невдалий Result<T>
    public static implicit operator Result<T>(Error error) => new(error);
}