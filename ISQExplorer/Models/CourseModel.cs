#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class CourseModel : IEquatable<CourseModel>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string CourseCode { get; set; }
        public string Name { get; set; }

        public bool Equals(CourseModel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && CourseCode == other.CourseCode && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CourseModel) obj);
        }

        public static bool operator ==(CourseModel? p1, CourseModel? p2) =>
            ReferenceEquals(p1, p2) || (!ReferenceEquals(p1, null) && p1.Equals(p2));

        public static bool operator !=(CourseModel? p1, CourseModel? p2) => !(p1 == p2);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (CourseCode != null ? CourseCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}