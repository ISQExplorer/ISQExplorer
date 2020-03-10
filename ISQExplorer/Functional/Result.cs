using System;
using System.Threading.Tasks;

namespace ISQExplorer.Functional
{
    public struct Result
    {
        private readonly Exception _ex;
        public readonly bool IsError;

        public static Result Of(Action func)
        {
            try
            {
                func();
                return new Result();
            }
            catch (Exception e)
            {
                return new Result(e);
            }
        }

        public static Result Of(Func<Result> func)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                return new Result(e);
            }
        }

        public static async Task<Result> OfAsync(Func<Task> func)
        {
            try
            {
                await func();
                return new Result();
            }
            catch (Exception e)
            {
                return new Result(e);
            }
        }

        public static async Task<Result> OfAsync(Func<Task<Result>> func)
        {
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                return new Result(e);
            }
        }

        public Result(Exception e)
        {
            (IsError, _ex) = (e != null, e);
        }

        public Result(Action func)
        {
            try
            {
                func();
                (IsError, _ex) = (false, default);
            }
            catch (Exception e)
            {
                (IsError, _ex) = (true, e);
            }
        }

        public Exception Error =>
            IsError ? _ex : throw new InvalidOperationException("This Result does not contain an error.");

        public static implicit operator bool(Result r) => !r.IsError;

        public static implicit operator Result(Exception ex) => new Result(ex);

        public static implicit operator Result(bool b) => b ? new Result() : new Result(new Exception());
    }

    public struct Result<TException> where TException : Exception
    {
        private readonly TException _ex;
        public readonly bool IsError;

        public static Result<TException> Of(Action func)
        {
            try
            {
                func();
                return new Result<TException>();
            }
            catch (TException e)
            {
                return new Result<TException>(e);
            }
        }

        public static async Task<Result<TException>> OfAsync(Func<Task> func)
        {
            try
            {
                await func();
                return new Result<TException>();
            }
            catch (TException e)
            {
                return new Result<TException>(e);
            }
        }

        public Result(TException e)
        {
            (IsError, _ex) = (true, e);
        }

        public Result(Action func)
        {
            try
            {
                func();
                (IsError, _ex) = (false, default);
            }
            catch (TException e)
            {
                (IsError, _ex) = (true, e);
            }
        }

        public TException Error =>
            IsError ? _ex : throw new InvalidOperationException("This Result does not contain an error.");

        public static implicit operator bool(Result<TException> r) => !r.IsError;

        public static implicit operator Result(Result<TException> r) => r ? new Result() : new Result(r.Error);

        public static implicit operator Result<TException>(TException ex) => new Result<TException>(ex);
    }
}