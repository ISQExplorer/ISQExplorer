using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Models
{
    public class ProfessorModel
    {
        public ProfessorModel()
        {
        }

        public ProfessorModel(string nNumber, string firstName, string lastName)
        {
            (NNumber, FirstName, LastName) = (nNumber, firstName, lastName);
        }

        protected bool Equals(ProfessorModel other)
        {
            return NNumber == other.NNumber;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProfessorModel) obj);
        }

        public override int GetHashCode()
        {
            return NNumber != null ? NNumber.GetHashCode() : 0;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NNumber { get; set; }
    }
}