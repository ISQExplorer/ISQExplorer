#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class QueryModel
    {
        protected bool Equals(QueryModel other)
        {
            return CourseCode == other.CourseCode && CourseName == other.CourseName &&
                   ProfessorName == other.ProfessorName && SeasonSince == other.SeasonSince &&
                   YearSince == other.YearSince && SeasonUntil == other.SeasonUntil && YearUntil == other.YearUntil;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((QueryModel) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (CourseCode != null ? CourseCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CourseName != null ? CourseName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProfessorName != null ? ProfessorName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SeasonSince.GetHashCode();
                hashCode = (hashCode * 397) ^ YearSince.GetHashCode();
                hashCode = (hashCode * 397) ^ SeasonUntil.GetHashCode();
                hashCode = (hashCode * 397) ^ YearUntil.GetHashCode();
                return hashCode;
            }
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? CourseCode { get; set; }
        public string? CourseName { get; set; }
        public string? ProfessorName { get; set; }
        public Season? SeasonSince { get; set; }
        public int? YearSince { get; set; }
        public Season? SeasonUntil { get; set; }
        public int? YearUntil { get; set; }
        public DateTime LastUpdated { get; set; }

        public QueryModel()
        {
            LastUpdated = DateTime.UtcNow;
        }
    }
}