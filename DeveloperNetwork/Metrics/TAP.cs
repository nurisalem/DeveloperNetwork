using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using DeveloperNetwork.Models;

namespace DeveloperNetwork
{
    public partial class Metrics
    {
        public static void TAP(IEnumerable<Commit> commits)
        {
            // build commit ranges
            var authorCommitRanges = new Dictionary<string, (DateTime, DateTime)>();

            // for each commit...
            foreach (var o in commits)
            {
                // have we worked with this author before?
                authorCommitRanges.TryGetValue(o.Author, out var range);
                if (range.Equals(default((DateTime, DateTime))))
                {
                    // no, save author
                    authorCommitRanges.Add(o.Author, (o.Date, o.Date));
                }
                else
                {
                    // yes, update range
                    if (o.Date < range.Item1)
                        range.Item1 = o.Date;

                    if (o.Date > range.Item2)
                        range.Item2 = o.Date;

                    authorCommitRanges[o.Author] = range;
                }
            }

            // calculate day difference per author
            //  always add 1 for a minimum value
            var daysPerAuthor = authorCommitRanges
                .Select(o => new TAPOutput
                {
                    Author = o.Key,
                    Days = (o.Value.Item2 - o.Value.Item1).Days + 1
                });

            // output to file
            using (var streamWriter = new StreamWriter("Metrics/TAP.csv"))
            using (var csvWriter = new CsvWriter(streamWriter))
                csvWriter.WriteRecords(daysPerAuthor);
        }
    }
}
