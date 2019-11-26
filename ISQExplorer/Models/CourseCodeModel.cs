#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class CourseCodeModel : IRangedModel
    {
        protected bool Equals(CourseCodeModel other)
        {
            return Equals(Course, other.Course) && CourseCode == other.CourseCode && Season == other.Season &&
                   Year == other.Year;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CourseCodeModel) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Course != null ? Course.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CourseCode != null ? CourseCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Season.GetHashCode();
                hashCode = (hashCode * 397) ^ Year.GetHashCode();
                return hashCode;
            }
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public CourseModel Course { get; set; }

        [StringLength(12)] public string CourseCode { get; set; }
        public Season? Season { get; set; }
        public int? Year { get; set; }
    }
}