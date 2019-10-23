using System;
using System.Data;
using System.IO;
using System.Linq;
using ISQExplorer.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ISQExplorer.Models
{
    public static class ProfessorSeeder
    {
        public static void Initialize(IServiceProvider serviceProvider, string professorCsv)
        {
            using var context = new ISQExplorerContext(
                serviceProvider.GetRequiredService<
                    DbContextOptions<ISQExplorerContext>>());

            var existingNNumbers = context.Professors.Select(x => x.NNumber).ToHashSet();
            var toInsert = File.OpenText(professorCsv).Lines().Select(x =>
            {
                var line = x.Split(",").Select(y => y.Trim()).ToList();
                if (line.Count != 3)
                {
                    throw new DataException(
                        $"Malformed line '{x}' needs to have 3 comma-separated entries corresponding to 'n-number, first name, last name'.");
                }

                var model = new ProfessorModel {FirstName = line[1], LastName = line[2], NNumber = line[0]};
                return model;
            });
            var withoutDuplicates = toInsert.Where(x => !existingNNumbers.Contains(x.NNumber));
            context.Professors.AddRange(withoutDuplicates);

            context.SaveChanges();
        }
    }
}