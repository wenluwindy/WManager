using System;

namespace WManager
{
    /// <summary>
    /// 统一的结果类型。用于显式表达成功/失败，避免异常滥用或魔法 null。
    /// </summary>
    /// <typeparam name="T">成功时的值类型</typeparam>
    public readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string Error { get; }

        private Result(bool isSuccess, T value, string error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value, null);
        public static Result<T> Failure(string error) => new Result<T>(false, default, error ?? "未知错误");

        public override string ToString()
        {
            return IsSuccess ? $"Success({Value})" : $"Failure({Error})";
        }

        /// <summary>
        /// 匹配成功/失败分支。函数式风格，避免到处写 if/else。
        /// </summary>
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(Value) : onFailure(Error);
        }
    }

    /// <summary>
    /// 无返回值的 Result 变体
    /// </summary>
    public readonly struct Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }

        private Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new Result(true, null);
        public static Result Failure(string error) => new Result(false, error ?? "未知错误");

        public override string ToString()
        {
            return IsSuccess ? "Success()" : $"Failure({Error})";
        }

        public Result<T> ToTyped<T>(T value)
        {
            return IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(Error);
        }
    }
}