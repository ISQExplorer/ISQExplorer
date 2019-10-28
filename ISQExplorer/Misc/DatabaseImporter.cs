#nullable enable
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using ISQExplorer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

            var fileEntries = JsonSerializer.Deserialize<ProfessorEntry[]>(File.ReadAllText(professorJson));
            context.Professors.AddRange(fileEntries.Select(x => new ProfessorModel
                {FirstName = x.FirstName, LastName = x.LastName, NNumber = x.NNumber}));

            context.SaveChanges();
        }

        public static void ImportClasses(IServiceProvider serviceProvider, string classJson)
        {
            using var context = new ISQExplorerContext(
                serviceProvider.GetRequiredService<
                    DbContextOptions<ISQExplorerContext>>());

            var entries = JsonSerializer.Deserialize<CourseEntry[]>(File.ReadAllText(classJson));
            var existingDescriptions = context.Courses.Select(x => x.Description).ToHashSet();

            foreach (var entry in entries)
            {
                if (existingDescriptions.Contains(entry.Description))
                {
                    continue;
                }

                var existingNames = context.CourseNames.Select(x => new {x.Name, x.SinceTerm, x.SinceYear}).ToHashSet();
                var existingCodes = context.CourseCodes.Select(x => new {x.CourseCode, x.SinceTerm, x.SinceYear})
                    .ToHashSet();

                var model = new CourseModel {Description = entry.Description};
                context.Courses.Add(model);

                foreach (var name in entry.Names)
                {
                    var (SinceTerm, SinceYear) = TermMethods.ParseTermNullable(name.Since);
                    var candidate = new {name.Name, SinceTerm, SinceYear};
                    if (!existingNames.Contains(candidate))
                    {
                        context.CourseNames.Add(new CourseNameModel
                        {
                            Course = model, SinceTerm = candidate.SinceTerm, Name = name.Name,
                            SinceYear = candidate.SinceYear
                        });
                    }
                }

                foreach (var code in entry.CourseCodes)
                {
                    var (SinceTerm, SinceYear) = TermMethods.ParseTermNullable(code.Since);
                    var candidate = new {CourseCode = code.Code, SinceTerm, SinceYear};
                    if (!existingCodes.Contains(candidate))
                    {
                        context.CourseCodes.Add(new CourseCodeModel
                        {
                            Course = model, SinceTerm = candidate.SinceTerm, CourseCode =  candidate.CourseCode,
                            SinceYear = candidate.SinceYear
                        });
                    }
                }
            }

            context.SaveChanges();
        }
    }
}