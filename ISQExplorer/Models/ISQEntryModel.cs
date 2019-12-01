using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Models
{
    public class ISQEntryModel
    {
        protected bool Equals(ISQEntryModel other)
        {
            return Season == other.Season && Year == other.Year && Crn == other.Crn;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((ISQEntryModel) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Season.GetHashCode();
                hashCode = (hashCode * 397) ^ Year.GetHashCode();
                hashCode = (hashCode * 397) ^ Crn;
                return hashCode;
            }
        }

        public static bool operator ==(ISQEntryModel i1, ISQEntryModel i2) => i1 != null && i1.Equals(i2);
        public static bool operator !=(ISQEntryModel i1, ISQEntryModel i2) => i1 == null || i1.Equals(i2);

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public CourseModel Course { get; set; }
        public Season Season { get; set; }
        public int Year { get; set; }
        public ProfessorModel Professor { get; set; }
        public int Crn { get; set; }
        public int NResponded { get; set; }
        public int NEnrolled { get; set; }
        public double Pct5 { get; set; }
        public double Pct4 { get; set; }
        public double Pct3 { get; set; }
        public double Pct2 { get; set; }
        public double Pct1 { get; set; }
        public double PctNa { get; set; }
        public double PctA { get; set; }
        public double PctAMinus { get; set; }
        public double PctBPlus { get; set; }
        public double PctB { get; set; }
        public double PctBMinus { get; set; }
        public double PctCPlus { get; set; }
        public double PctC { get; set; }
        public double PctD { get; set; }
        public double PctF { get; set; }
        public double PctWithdraw { get; set; }
        public double MeanGpa { get; set; }
    }
}