#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class ProfessorModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string NNumber { get; set; } = null!;
        public DepartmentModel Department { get; set; } = null!;

        protected bool Equals(ProfessorModel? other)
        {
            return other != null && NNumber == other.NNumber;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProfessorModel) obj);
        }

        public static bool operator ==(ProfessorModel? p1, ProfessorModel? p2) =>
            ReferenceEquals(p1, p2) || (!ReferenceEquals(p1, null) && p1.Equals(p2));

        public static bool operator !=(ProfessorModel? p1, ProfessorModel? p2) => !(p1 == p2);

        public override int GetHashCode()
        {
            return NNumber != null ? NNumber.GetHashCode() : 0;
        }

        public override string ToString() => $"{NNumber} - {FirstName} {LastName} - {Department?.Name}";
    }
}