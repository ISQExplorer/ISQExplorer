using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ISQExplorer.Functional
{
    public class Either<TLeft, TRight> : IEquatable<Either<TLeft, TRight>>
    {
        public readonly bool HasLeft;
        public readonly bool HasRight;
        private readonly TLeft _left;
        private readonly TRight _right;

        public Either(TLeft val)
        {
            (HasLeft, HasRight, _left, _right) = (true, false, val, default!);
        }

        public Either(TRight val)
        {
            (HasLeft, HasRight, _left, _right) = (false, true, default!, val);
        }

        public TLeft Left =>
            HasLeft
                ? _left
                : throw new InvalidOperationException(
                    $"This Either has a {typeof(TRight).Name} value, and not a {typeof(TLeft).Name} value.");


        public TRight Right =>
            HasRight
                ? _right
                : throw new InvalidOperationException(
                    $"This Either has a {typeof(TLeft).Name} value, and not a {typeof(TRight).Name} value.");

        public void Match(Action<TLeft> left, Action<TRight> right)
        {
            if (HasLeft)
            {
                left(Left);
            }
            else
            {
                right(Right);
            }
        }

        public TRes Match<TRes>(Func<TLeft, TRes> left, Func<TRight, TRes> right) =>
            HasLeft ? left(Left) : right(Right);

        public Task MatchAsync(Func<TLeft, Task> left, Func<TRight, Task> right) =>
            HasLeft ? left(Left) : right(Right);

        public Task<TRes> MatchAsync<TRes>(Func<TLeft, Task<TRes>> left, Func<TRight, Task<TRes>> right) =>
            HasLeft ? left(Left) : right(Right);

        public TLeft Unite(Func<TRight, TLeft> func) => HasLeft ? Left : func(Right);

        public TRight Unite(Func<TLeft, TRight> func) => HasRight ? Right : func(Left);

        public async Task<TLeft> UniteAsync(Func<TRight, Task<TLeft>> func) => HasLeft ? Left : await func(Right);

        public async Task<TRight> UniteAsync(Func<TLeft, Task<TRight>> func) => HasRight ? Right : await func(Left);

        public static implicit operator Either<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left);

        public static implicit operator Either<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(right);

        public static explicit operator TLeft(Either<TLeft, TRight> e) => e.Left;

        public static explicit operator TRight(Either<TLeft, TRight> e) => e.Right;

        public static implicit operator Either<TRight, TLeft>(Either<TLeft, TRight> e) =>
            e.HasLeft ? new Either<TRight, TLeft>(e.Left) : new Either<TRight, TLeft>(e.Right);

        public static implicit operator Optional<TLeft>(Either<TLeft, TRight> e) =>
            e.HasLeft ? new Optional<TLeft>(e.Left) : new Optional<TLeft>();

        public static implicit operator Optional<TRight>(Either<TLeft, TRight> e) =>
            e.HasRight ? new Optional<TRight>(e.Right) : new Optional<TRight>();

        public static bool operator ==(Either<TLeft, TRight> v1, Either<TLeft, TRight> v2) =>
            ReferenceEquals(v1, v2) || (!ReferenceEquals(v1, null) && v1.Equals(v2));

        public static bool operator !=(Either<TLeft, TRight> v1, Either<TLeft, TRight> v2) => !(v1 == v2);

        public bool Equals(Either<TLeft, TRight> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return HasLeft == other.HasLeft && HasRight == other.HasRight &&
                   EqualityComparer<TLeft>.Default.Equals(_left, other._left) &&
                   EqualityComparer<TRight>.Default.Equals(_right, other._right);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Either<TLeft, TRight>) obj);
        }

        public override int GetHashCode() => HasLeft ? Left.GetHashCode() : Right.GetHashCode();

        public override string ToString() => HasLeft ? Left!.ToString()! : Right!.ToString()!;
    }
}