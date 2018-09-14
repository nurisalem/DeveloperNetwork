using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DeveloperNetwork.Models;
using MongoDB.Bson;

namespace DeveloperNetwork
{
    public partial class Metrics
    {
        /// <summary>
        ///     Degree centrality (DC)
        ///     Betweens centrality (BC)
        ///     Closeness centrality (CC)
        /// </summary>
        /// <param name="commits"></param>
        public static async Task Centrality(List<Commit> commits)
        {
            // for all commits...
            var relatedAuthors = commits
                .AsParallel()
                .Select(o =>
                {
                    // define related commits range (ex: -1 +1 months = 2 months range)
                    var earliestDate = o.Date.AddMonths(-1);
                    var latestDate = o.Date.AddMonths(1);

                    // for all commits by other authors...
                    var relatedCommits = commits
                        .Where(c => c.Author != o.Author)
                        .Where(c =>
                        {
                            // validate date range
                            var destDate = c.Date;

                            var isWithinEarlyDate = destDate >= earliestDate && destDate <= o.Date;
                            var isWithinLateRange = destDate >= o.Date && destDate <= latestDate;

                            return isWithinEarlyDate || isWithinLateRange;
                        })
                        // extract author
                        .Select(c => c.Author)
                        .ToList();

                    // extract edge
                    return new KeyValuePair<string, List<string>>(o.Author, relatedCommits);
                })
                // group authors to extract distinct edges
                .GroupBy(o => o.Key)
                .Select(o =>
                {
                    // merge for clear relation visibility
                    var mergedRelatedAuthors = new List<string>();
                    foreach (var commitRelatedAuthors in o)
                        mergedRelatedAuthors.AddRange(commitRelatedAuthors.Value);

                    var distinctRelatedAuthors = mergedRelatedAuthors
                        .GroupBy(c => c)
                        .Select(c => c.Key)
                        .ToList();

                    return new KeyValuePair<string, List<string>>(o.Key, distinctRelatedAuthors);
                })
                .ToList();

            // output related authors as nodes and edges for python processing
            using (var nodesWriter = new StreamWriter("Metrics/CentralityNodes.txt"))
            using (var edgesWriter = new StreamWriter("Metrics/CentralityEdges.txt"))
            {
                foreach (var author in relatedAuthors)
                {
                    await nodesWriter.WriteLineAsync(author.Key);
                    
                    foreach (var relatedAuthor in author.Value)
                        await edgesWriter.WriteLineAsync($"{author.Key}, {relatedAuthor}");
                }
            }

            // call Python script for analysis output
            var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = "/C python CentralityAnalysis.py",
                UseShellExecute = false,
                WorkingDirectory = Directory.GetCurrentDirectory() + "/Metrics"
            };
            process.StartInfo = startInfo;
            process.Start();
        }
    }
}
