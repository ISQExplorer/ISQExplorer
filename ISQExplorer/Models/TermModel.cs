using System;
using System.ComponentModel.DataAnnotations;

namespace ISQExplorer.Models
{
    public class TermModel : IEquatable<TermModel>, IComparable<TermModel>
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public bool Equals(TermModel? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TermModel) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
        
        public int CompareTo(TermModel other) => this.Id - other.Id;

        public static bool operator >(TermModel? t1, TermModel? t2) =>
            !ReferenceEquals(t1, null) && !ReferenceEquals(t2, null) && t1.CompareTo(t2) > 0;

        public static bool operator <(TermModel? t1, TermModel? t2) =>
            !ReferenceEquals(t1, null) && !ReferenceEquals(t2, null) && t1.CompareTo(t2) < 0;

        public static bool operator >=(TermModel? t1, TermModel? t2) =>
            t1 == t2 || t1 > t2;

        public static bool operator <=(TermModel? t1, TermModel? t2) =>
            t1 == t2 || t1 < t2;

        public static bool operator ==(TermModel? t1, TermModel? t2) =>
            ReferenceEquals(t1, t2) || (!ReferenceEquals(t1, null) && t1.Equals(t2));

        public static bool operator !=(TermModel? t1, TermModel? t2) => !(t1 == t2);

        public override string ToString() => Name;
    }
}