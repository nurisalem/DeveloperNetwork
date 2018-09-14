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
        public static void LoC(List<BsonDocument> statistics)
        {
            // build collection
            var authorCommits = statistics
                .Select(o =>
                {
                    var author = o["author"]["login"].AsString;
                    var commits = o["total"].AsInt32;

                    return new LoCOutput
                    {
                        Author = author,
                        Commits = commits
                    };
                });

            // output to file
            using (var streamWriter = new StreamWriter("Metrics/LoC.csv"))
            using (var csvWriter = new CsvWriter(streamWriter))
                csvWriter.WriteRecords(authorCommits);
        }
    }
}
