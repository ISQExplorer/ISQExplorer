#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class CourseModel
    {
        public CourseModel()
        {
        }

        public CourseModel(string description, int? id = null)
        {
            Description = description;
            Id = id ?? 0;
        }

        protected bool Equals(CourseModel other)
        {
            return Description == other.Description;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CourseModel) obj);
        }

        public override int GetHashCode()
        {
            return (Description != null ? Description.GetHashCode() : 0);
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Description { get; set; }
    }
}