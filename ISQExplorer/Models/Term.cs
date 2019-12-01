using System;
using System.Security.Cryptography.Xml;
using Microsoft.VisualBasic.CompilerServices;

namespace ISQExplorer.Models
{
    public enum Season
    {
        Spring = 0,
        Summer = 1,
        Fall = 2
    }

    public class Term : IComparable<Term>, IEquatable<Term>
    {
        public bool Equals(Term other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Season == other.Season && Year == other.Year;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Term) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Season * 397) ^ Year;
            }
        }

        public readonly Season Season;
        public readonly int Year;

        public static (Season?, int?) FromNullableString(string? s)
        {
            if (s == null)
            {
                return (null, null);
            }

            var (season, year) = new Term(s);
            return (season, year);
        }

        public Term(string s)
        {
            var since = s.Split(" ");
            if (since.Length != 2)
            {
                throw new ArgumentException($"Invalid since field '{s}'");
            }

            var term = since[0] switch
            {
                "Spring" => Season.Spring,
                "Summer" => Season.Summer,
                "Fall" => Season.Fall,
                _ => throw new ArgumentException($"Invalid term '{since[0]}'")
            };

            if (!int.TryParse(since[1], out var year))
            {
                throw new ArgumentException($"Invalid year '{since[1]}'");
            }

            (Season, Year) = (term, year);
        }

        public Term(Season season, int year)
        {
            (Season, Year) = (season, year);
        }

        public Term(Term other)
        {
            (Season, Year) = other;
        }

        public void Deconstruct(out Season season, out int year)
        {
            (season, year) = (Season, Year);
        }

        public int CompareTo(Term other)
        {
            if (this.Year != other.Year)
            {
                return this.Year - other.Year;
            }

            return this.Season - other.Season;
        }

        // DO NOT USE `t1 != null` HERE. it causes infinite recursion
        public static bool operator ==(Term? t1, Term? t2) =>
            ReferenceEquals(t1, t2) || (!ReferenceEquals(t1, null) && t1.Equals(t2));

        public static bool operator !=(Term? t1, Term? t2) => !(t1 == t2);

        public static bool operator >(Term? t1, Term? t2)
        {
            if (t1 == t2 || t1 == null)
            {
                return false;
            }

            if (t2 == null)
            {
                return true;
            }

            return t1.Year > t2.Year || (t1.Year == t2.Year && t1.Season > t2.Season);
        }

        public static bool operator <(Term? t1, Term? t2)
        {
            if (t1 == t2 || t2 == null)
            {
                return false;
            }

            if (t1 == null)
            {
                return true;
            }
            
            return t1.Year < t2.Year || (t1.Year == t2.Year && t1.Season < t2.Season);
        }

        public static bool operator >=(Term? t1, Term? t2)
        {
            return t1 == t2 || t1 > t2;
        }

        public static bool operator <=(Term? t1, Term? t2)
        {
            return t1 == t2 || t1 < t2;
        }

        public (Season, int) ToTuple()
        {
            return (Season, Year);
        }

        public static (Season?, int?) ToTuple(Term? x)
        {
            if (x == null)
            {
                return (null, null);
            }

            return x.ToTuple();
        }

        public override string ToString() => $"{Season} {Year}";
    }
}