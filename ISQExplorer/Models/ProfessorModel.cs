using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Models
{
    public class ProfessorModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NNumber { get; set; }
    }
}