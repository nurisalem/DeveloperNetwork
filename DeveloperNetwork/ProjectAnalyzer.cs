using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeveloperNetwork.Aliases;
using DeveloperNetwork.Models;
using DeveloperNetwork.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace DeveloperNetwork
{
    public class ProjectAnalyzer
    {
        private readonly ProgramConfiguration _configuration;
        private readonly MongoProxy _mongoProxy;
        private readonly GitProxy _gitProxy;
        private readonly GoogleProxy _googleProxy;
        private readonly AliasReader _aliasReader;
        private readonly ILogger<ProjectAnalyzer> _logger;

        public ProjectAnalyzer(ProgramConfiguration configuration, MongoProxy mongoProxy, GitProxy gitProxy, GoogleProxy googleProxy,
            AliasReader aliasReader, ILogger<ProjectAnalyzer> logger)
        {
            _configuration = configuration;
            _mongoProxy = mongoProxy;
            _gitProxy = gitProxy;
            _googleProxy = googleProxy;
            _aliasReader = aliasReader;
            _logger = logger;
        }

        public async Task RunAnalysis()
        {
            // prepare commits
            if ((_configuration.DataFlags & DataFlags.Commits) == DataFlags.Commits)
            {
                // prepare db
                await _mongoProxy.TruncateCommits();

                // read all commit pages
                var commits = new List<Commit>();
                _gitProxy.OnCommitQueryNext += async (sender, pagedCommits) =>
                {
                    commits.AddRange(pagedCommits);
                    await _mongoProxy.InsertCommits(pagedCommits);
                };

                // run
                await _gitProxy.GetCommits();
                Commits = commits;
            }
            else
                Commits = await _mongoProxy.GetCommits();

            // prepare releases
            if ((_configuration.DataFlags & DataFlags.Releases) == DataFlags.Releases)
            {
                Releases = await _gitProxy.GetReleases();

                if (Releases.Any())
                    await _mongoProxy.RecacheReleases(Releases);
            }
            else
                Releases = await _mongoProxy.GetReleases();

            // prepare stats
            if ((_configuration.DataFlags & DataFlags.Statistics) == DataFlags.Statistics)
            {
                Statistics = await _gitProxy.GetStatistics();

                if (Statistics.Any())
                    await _mongoProxy.RecacheStatistics(Statistics);
            }
            else
                Statistics = await _mongoProxy.GetStatistics();

            // prepare users
            if ((_configuration.DataFlags & DataFlags.Users) == DataFlags.Users)
            {
                Users = await _gitProxy.GetUsers(Statistics);

                if (Users.Any())
                    await _mongoProxy.RecacheUsers(Users);
            }
            else
                Users = await _mongoProxy.GetUsers();
            
            // read aliases
            var aliases = _aliasReader.Aliases;

            // replace aliased authors
            if (aliases.Any())
            {
                foreach (var commit in Commits)
                foreach (var alias in aliases)
                {
                    if (alias.Value.Contains(commit.Author))
                        commit.UpdateAuthor(alias.Key);
                }
            }

            // run metric calculation tasks
            var tasks = new List<Task>
            {
                Metrics.Centrality(Commits),
                Task.Run(() => Metrics.NoD(Commits)),
                Task.Run(() => Metrics.TAP(Commits)),
                Task.Run(() => Metrics.NoC(Releases, Commits)),
                Task.Run(() => Metrics.LoC(Statistics)),
                Metrics.GL(_googleProxy, Users),
                Task.Run(() => Metrics.NAC(Releases, Commits)),
            };

            // wait for completion
            await Task.WhenAll(tasks);
        }

        public List<BsonDocument> Users { get; set; }

        public List<BsonDocument> Statistics { get; set; }

        public List<BsonDocument> Releases { get; set; }

        public List<Commit> Commits { get; set; }
    }
}
