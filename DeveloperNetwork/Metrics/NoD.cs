using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using DeveloperNetwork.Models;

namespace DeveloperNetwork
{
    public partial class Metrics
    {
        /// <summary>
        ///     Number of developers who modified the file
        /// </summary>
        /// <param name="commits"></param>
        public static void NoD(List<Commit> commits)
        {
            // for all commits...
            var filesByAuthor = commits
                .AsParallel()
                // get unique authors
                .GroupBy(o => o.Author)
                .ToDictionary(o => o.Key, o =>
                {
                    // extract all files author has worked on
                    var authorCommits = o
                        .Select(c => c.Files.Select(f => f["filename"].AsString))
                        .SelectMany(c => c)
                        .Distinct()
                        .ToList();

                    return authorCommits;
                });

            // for each commit...
            var authorsByFile = new Dictionary<string, int>();
            foreach (var commit in commits)
            {
                // for each file...
                foreach (var file in commit.Files)
                {
                    // extract url to use as ID
                    var url = file["filename"].AsString;

                    // add file to collection if not present
                    if (!authorsByFile.ContainsKey(url))
                        authorsByFile.Add(url, 0);

                    // get all files the author of this commit has worked on
                    var byAuthor = filesByAuthor[commit.Author];

                    // has he worked on this one? +1
                    if (byAuthor.Contains(url))
                        authorsByFile[url]++;
                }
            }

            // transform into friendly CSV format
            var nod = authorsByFile.Select(o => new NoDOutput
            {
                File = o.Key,
                AuthorCount = o.Value
            });

            // output to file
            using (var streamWriter = new StreamWriter("Metrics/NoD.csv"))
            using (var csvWriter = new CsvWriter(streamWriter))
                csvWriter.WriteRecords(nod);
        }
    }
}
