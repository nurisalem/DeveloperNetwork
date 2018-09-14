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
        /// <summary>
        ///     Number of commits per release
        /// </summary>
        /// <param name="releases"></param>
        /// <param name="commits"></param>
        public static void NoC(List<BsonDocument> releases, List<Commit> commits)
        {
            // sort releases by ascending created date
            releases = releases
                .OrderBy(o => DateTime.Parse(o["created_at"].AsString))
                .ToList();

            // build release commit count collection
            var releaseCounts = new List<NoCOutput>();

            // for each release...
            for (int i = 0; i < releases.Count; i++)
            {
                // get release info
                var current = releases[i];
                var name = current["name"].AsString;
                var date = DateTime.Parse(current["created_at"].AsString);
                int commitCount;

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "";

                    // this is the first release, get all commits prior to created date
                    commitCount = commits.Count(o => o.Date <= date);
                }
                else
                {
                    // all other releases, get in-between commit counts
                    var previous = releases[i - 1];
                    var fromDate = DateTime.Parse(previous["created_at"].AsString);

                    // get commit count
                    commitCount = commits.Count(o => o.Date > fromDate && o.Date <= date);
                }

                // add to collection
                releaseCounts.Add(new NoCOutput
                {
                    Release = name,
                    Commits = commitCount
                });
            }

            // output to file
            using (var streamWriter = new StreamWriter("Metrics/NoC.csv"))
            using (var csvWriter = new CsvWriter(streamWriter))
                csvWriter.WriteRecords(releaseCounts);
        }
    }
}
