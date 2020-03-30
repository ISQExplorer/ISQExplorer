using System;
using System.Security.Policy;
using ISQExplorer.Functional;

namespace ISQExplorer.Misc
{
    public static class EnumConversions
    {
        public static Try<TEnum, ArgumentException> FromString<TEnum>(string str) where TEnum : struct, System.Enum
        {
            if (!Enum.TryParse<TEnum>(str, true, out var res))
            {
                return new ArgumentException($"'{str}' is not convertible to an enum of type {typeof(TEnum).Name}.");
            }
            return res;
        }

        public static Try<TEnum, ArgumentException> FromInt<TEnum>(int num) where TEnum : System.Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), num))
            {
                return new ArgumentException($"'{num}' is not convertible to an enum of type {typeof(TEnum).Name}.");
            }

            return (TEnum)Convert.ChangeType(num, typeof(TEnum));
        }

        public static string AsString<TEnum>(this TEnum e)
            where TEnum : struct, System.Enum =>
            Enum.GetName(typeof(TEnum), e) ?? throw new ArgumentException("The argument cannot be converted to a string.");
    }
}