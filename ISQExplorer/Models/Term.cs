using System;

namespace ISQExplorer.Models
{
    public enum Season
    {
        Spring = 0,
        Summer = 1,
        Fall = 2
    }

    public class Term : IComparable<Term>
    {
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

        public int CompareSql(Season? season, int? year)
        {
            if (season == null || year == null)
            {
                return 1;
            }

            if (Year != year)
            {
                return Year - (int)year;
            }

            return Season - (Season)season;
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