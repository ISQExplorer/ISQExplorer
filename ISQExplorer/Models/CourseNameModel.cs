#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class CourseNameModel : IRangedModel
    {
        protected bool Equals(CourseNameModel other)
        {
            return Equals(Course, other.Course) && Name == other.Name && Season == other.Season && Year == other.Year;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CourseNameModel) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Course != null ? Course.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Season.GetHashCode();
                hashCode = (hashCode * 397) ^ Year.GetHashCode();
                return hashCode;
            }
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public CourseModel Course { get; set; }
        
        [StringLength(255)]
        public string Name { get; set; }
        public Season? Season { get; set; }
        public int? Year { get; set; }
    }
}