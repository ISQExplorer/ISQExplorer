#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ISQExplorer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ISQExplorer.Misc
{
    public class CourseEntry
    {
        public string Description;
        public NameEntry[] Names;
        public CodeEntry[] CourseCodes;
    }

    public class NameEntry
    {
        public string Name;
        public string? Since;
    }

    public class CodeEntry
    {
        public string Code;
        public string? Since;
    }

    public class ProfessorEntry
    {
        public string NNumber;
        public string FirstName;
        public string LastName;
    }

    public static class DatabaseImporter
    {
        public static void ImportProfessors(IServiceProvider serviceProvider, string professorJson)
        {
            using var context = new ISQExplorerContext(
                serviceProvider.GetRequiredService<
                    DbContextOptions<ISQExplorerContext>>());

            var existingNNumber = context.Professors.Select(x => x.NNumber).ToHashSet();
            
            var fileEntries = JsonConvert.DeserializeObject<List<ProfessorEntry>>(File.ReadAllText(professorJson));
            if (fileEntries.Any(x => x.NNumber == null || x.LastName == null))
            {
                throw new ArgumentException("Professor N-Number/last name cannot be null.");
            }
            
            context.Professors.AddRange(fileEntries
                .Select(x => new ProfessorModel
                {FirstName = x.FirstName, LastName = x.LastName, NNumber = x.NNumber})
                .Where(x => !existingNNumber.Contains(x.NNumber)));

            context.SaveChanges();
        }

        public static void ImportClasses(IServiceProvider serviceProvider, string classJson)
        {
            using var context = new ISQExplorerContext(
                serviceProvider.GetRequiredService<
                    DbContextOptions<ISQExplorerContext>>());

            var entries = JsonConvert.DeserializeObject<CourseEntry[]>(File.ReadAllText(classJson));
            var existingDescriptions = context.Courses.Select(x => x.Description).ToHashSet();

            foreach (var entry in entries)
            {
                if (existingDescriptions.Contains(entry.Description))
                {
                    continue;
                }

                var existingNames = context.CourseNames.Select(x => new {x.Name, SinceTerm = x.Season, SinceYear = x.Year}).ToHashSet();
                var existingCodes = context.CourseCodes.Select(x => new {x.CourseCode, SinceTerm = x.Season, SinceYear = x.Year})
                    .ToHashSet();

                var model = new CourseModel {Description = entry.Description};
                context.Courses.Add(model);

                foreach (var name in entry.Names)
                {
                    var (SinceTerm, SinceYear) = Term.FromNullableString(name.Since);
                    var candidate = new {name.Name, SinceTerm, SinceYear};
                    if (!existingNames.Contains(candidate))
                    {
                        context.CourseNames.Add(new CourseNameModel
                        {
                            Course = model, Season = candidate.SinceTerm, Name = name.Name,
                            Year = candidate.SinceYear
                        });
                    }
                }

                foreach (var code in entry.CourseCodes)
                {
                    var (SinceTerm, SinceYear) = Term.FromNullableString(code.Since);
                    var candidate = new {CourseCode = code.Code, SinceTerm, SinceYear};
                    if (!existingCodes.Contains(candidate))
                    {
                        context.CourseCodes.Add(new CourseCodeModel
                        {
                            Course = model, Season = candidate.SinceTerm, CourseCode =  candidate.CourseCode,
                            Year = candidate.SinceYear
                        });
                    }
                }
            }

            context.SaveChanges();
        }
    }
}