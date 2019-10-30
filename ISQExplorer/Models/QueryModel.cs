#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISQExplorer.Models
{
    public class QueryModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? CourseCode { get; set; }
        public string? CourseName { get; set; }
        public string? ProfessorName { get; set; }
        public Season? SeasonSince { get; set; }
        public int? YearSince { get; set; }
        public Season? SeasonUntil { get; set; }
        public int? YearUntil { get; set; }
        public DateTime LastUpdated { get; set; }

        public QueryModel()
        {
            LastUpdated = DateTime.UtcNow;
        }
    }
}