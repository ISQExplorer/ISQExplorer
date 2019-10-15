using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Models
{
    public class IsqEntryModel
    {
        public int Id { get; set; }
        public string Term { get; set; }
        public ProfessorModel Professor { get; set; }
        public int Crn { get; set; }
        public int NResponded { get; set; }
        public int NTotal { get; set; }
        public double Pct5 { get; set; }
        public double Pct4 { get; set; }
        public double Pct3 { get; set; }
        public double Pct2 { get; set; }
        public double Pct1 { get; set; }
        public int NEnrolled { get; set; }
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