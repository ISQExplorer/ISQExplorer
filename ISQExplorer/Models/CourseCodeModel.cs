#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class CourseCodeModel : IRangedModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public CourseModel Course { get; set; }
        
        [StringLength(12)]
        public string CourseCode { get; set; }
        public Season? Season { get; set; }
        public int? Year { get; set; }
    }
}