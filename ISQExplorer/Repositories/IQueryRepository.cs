#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public enum QueryType
    {
        CourseCode = 0,
        CourseName = 1,
        ProfessorName = 2
    }

    public interface IQueryRepository
    {
        Task<IQueryable<ISQEntryModel>> QueryClass(string parameter, QueryType qt, Term? since = null, Term? until = null);
        Task<IQueryable<ProfessorModel>> NameToProfessors(string professorName);
    }
}