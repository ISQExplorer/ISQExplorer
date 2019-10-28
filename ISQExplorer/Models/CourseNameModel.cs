#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class CourseNameModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public CourseModel Course { get; set; }
        
        [StringLength(255)]
        public string Name { get; set; }
        public TermSeason? SinceTerm { get; set; }
        public int? SinceYear { get; set; }
    }
}