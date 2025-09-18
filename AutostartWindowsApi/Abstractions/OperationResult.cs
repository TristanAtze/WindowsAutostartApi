using System;

namespace WindowsAutostartApi.Abstractions;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
public sealed class OperationResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }

    private OperationResult(bool isSuccess, string? errorMessage, Exception? exception)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    public static OperationResult Success() => new(true, null, null);

    public static OperationResult Failure(string errorMessage) => new(false, errorMessage, null);

    public static OperationResult Failure(string errorMessage, Exception exception) => new(false, errorMessage, exception);

    public static OperationResult Failure(Exception exception) => new(false, exception.Message, exception);
}

/// <summary>
/// Represents the result of an operation that can succeed with a value or fail.
/// </summary>
public sealed class OperationResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }

    private OperationResult(bool isSuccess, T? value, string? errorMessage, Exception? exception)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    public static OperationResult<T> Success(T value) => new(true, value, null, null);

    public static OperationResult<T> Failure(string errorMessage) => new(false, default, errorMessage, null);

    public static OperationResult<T> Failure(string errorMessage, Exception exception) => new(false, default, errorMessage, exception);

    public static OperationResult<T> Failure(Exception exception) => new(false, default, exception.Message, exception);

    public static implicit operator OperationResult<T>(T value) => Success(value);
}