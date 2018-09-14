using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using DeveloperNetwork.Models;
using DeveloperNetwork.Services;
using MongoDB.Bson;

namespace DeveloperNetwork
{
    public partial class Metrics
    {
        public static async Task GL(GoogleProxy googleProxy, List<BsonDocument> users)
        {
            // get user locations
            var authorLocationTasks = users
                .Select(o =>
                {
                    // TODO: Move this logic to a user class

                    // parse bson
                    var login = o["login"];
                    var location = o["location"];

                    return new GLOutput
                    {
                        Login = login != BsonNull.Value ? login.AsString : string.Empty,
                        GitLocation = location != BsonNull.Value ? location.AsString : string.Empty,
                    };
                })
                // treat only users who provided their locations
                .Where(o => !string.IsNullOrWhiteSpace(o.GitLocation) &&
                            !string.IsNullOrWhiteSpace(o.Login))
                .Select(async o =>
                {
                    // send query to google geocode API
                    var location = o.GitLocation;
                    var geocode = await googleProxy.LookupGeocode(location);

                    return new GLOutput(o, geocode);
                })
                .ToList();

            // wait for completion
            await Task.WhenAll(authorLocationTasks);
            var authorLocations = authorLocationTasks.Select(o => o.Result);

            // output to file
            using (var streamWriter = new StreamWriter("Metrics/GL.csv"))
            using (var csvWriter = new CsvWriter(streamWriter))
                csvWriter.WriteRecords(authorLocations);
        }
    }
}
