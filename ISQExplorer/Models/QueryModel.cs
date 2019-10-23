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

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? NNumber { get; set; }
        public DateTime LastUpdated { get; set; }

        public QueryModel()
        {
            LastUpdated = DateTime.UtcNow;
        }
    }
}