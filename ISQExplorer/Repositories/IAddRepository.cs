using ISQExplorer.Models;

#nullable enable

namespace ISQExplorer.Repositories
{
    public interface IAddRepository
    {
        void AddClass(string? description = null, string? name = null, (Term, int)? nameSince = null, 
            string? courseCode = null, (Term, int)? courseCodeSince = null);

        void AddProfessor(string nNumber, string firstName, string lastName);
    }
}