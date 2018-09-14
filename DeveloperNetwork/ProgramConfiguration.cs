using System;
using System.Configuration;
using System.Text.RegularExpressions;
using DeveloperNetwork.Models;

namespace DeveloperNetwork
{
    public class ProgramConfiguration
    {
        public ProgramConfiguration(string[] args)
        {
            // parse args
            GitToken = args[0];
            GoogleKey = args[1];

            Enum.TryParse<DataFlags>(args[2], out var dataFlags);
            DataFlags = dataFlags;

            // get configuration options
            ProjectUriShorthand = ConfigurationManager.AppSettings["ProjectUriShorthand"];
            MongoConnectionString = ConfigurationManager.AppSettings["MongoConnectionString"];
            
            // clean project uri
            var rgx = new Regex("[^a-zA-Z0-9]");
            CleanProjectName = rgx.Replace(ProjectUriShorthand, "");
        }

        public string GoogleKey { get; }

        public string CleanProjectName { get; }

        public string MongoConnectionString { get; }

        public string ProjectUriShorthand { get; }

        public string GitToken { get; }

        public DataFlags DataFlags { get; set; }
    }
}
