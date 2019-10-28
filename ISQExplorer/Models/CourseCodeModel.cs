#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class CourseCodeModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public CourseModel Course { get; set; }
        
        [StringLength(12)]
        public string CourseCode { get; set; }
        public TermSeason? SinceTerm { get; set; }
        public int? SinceYear { get; set; }
    }
}