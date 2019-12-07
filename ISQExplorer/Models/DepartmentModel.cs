#nullable enable
using System;
using System.ComponentModel.DataAnnotations;

namespace ISQExplorer.Models
{
    public class DepartmentModel : IEquatable<DepartmentModel>
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime LastUpdated { get; set; }

        public DepartmentModel()
        {
            LastUpdated = DateTime.UtcNow;
        }

        public bool Equals(DepartmentModel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DepartmentModel) obj);
        }
        
        public static bool operator ==(DepartmentModel? p1, DepartmentModel? p2) =>
            ReferenceEquals(p1, p2) || (!ReferenceEquals(p1, null) && p1.Equals(p2));

        public static bool operator !=(DepartmentModel? p1, DepartmentModel? p2) => !(p1 == p2);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }
}