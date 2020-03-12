using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class ISQEntryModel : ITimedModel, IEquatable<ISQEntryModel>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public CourseModel Course { get; set; } = null!;
        public TermModel Term { get; set; } = null!;
        public ProfessorModel Professor { get; set; } = null!;
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

        public bool Equals(ISQEntryModel? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Course, other.Course) && Term == other.Term && Crn == other.Crn;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ISQEntryModel) obj);
        }

        public static bool operator ==(ISQEntryModel? p1, ISQEntryModel? p2) =>
            ReferenceEquals(p1, p2) || (!ReferenceEquals(p1, null) && p1.Equals(p2));

        public static bool operator !=(ISQEntryModel? p1, ISQEntryModel? p2) => !(p1 == p2);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Course != null ? Course.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Term != null ? Term.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Crn;
                return hashCode;
            }
        }

        public override string ToString() => $"{Term} {Crn} - {Course} {Professor}";
    }
}