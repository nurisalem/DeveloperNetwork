using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using DeveloperNetwork.Models;
using MongoDB.Bson;

namespace DeveloperNetwork
{
    public partial class Metrics
    {
        public static void NAC(List<BsonDocument> releases, List<Commit> commits)
        {
            // sort releases by ascending created date
            releases = releases
                .OrderBy(o => DateTime.Parse(o["created_at"].AsString))
                .ToList();

            // build release commit count collection
            var releaseAuthors = new List<NACOutput>();

            // for each release...
            for (int i = 0; i < releases.Count; i++)
            {
                // get release info
                var current = releases[i];
                var name = current["name"].AsString;
                var date = DateTime.Parse(current["created_at"].AsString);
                IEnumerable<Commit> releaseCommits;

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "";

                    // this is the first release, get all commits prior to created date
                    releaseCommits = commits.Where(o => o.Date <= date);
                }
                else
                {
                    // all other releases, get in-between commit counts
                    var previous = releases[i - 1];
                    var fromDate = DateTime.Parse(previous["created_at"].AsString);

                    // get commit count
                    releaseCommits = commits.Where(o => o.Date > fromDate && o.Date <= date);
                }

                // extract distinc authors
                var authorCount = releaseCommits.Select(o => o.Author).Distinct().Count();

                // add to collection
                releaseAuthors.Add(new NACOutput
                {
                    Release = name,
                    Authors = authorCount
                });
            }

            var a = new List<string>();
            foreach (var s in a)
            {
                // do something
            }

            a.ForEach(s =>
            {
                // do something
            });

            // output to file
            using (var streamWriter = new StreamWriter("Metrics/NAC.csv"))
            using (var csvWriter = new CsvWriter(streamWriter))
                csvWriter.WriteRecords(releaseAuthors);
        }
    }
}
