using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeveloperNetwork.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DeveloperNetwork.Services
{
    public class MongoProxy
    {
        private readonly IMongoCollection<BsonDocument> _commitCollection;
        private readonly IMongoCollection<BsonDocument> _releaseCollection;
        private readonly IMongoCollection<BsonDocument> _statisticsCollection;
        private readonly IMongoCollection<BsonDocument> _usersCollection;

        // TODO: Create interface for collection objects to simplify manipulation and avoid code duplication

        public MongoProxy(ProgramConfiguration configuration, ILogger<MongoProxy> logger)
        {
            // connect to mongo
            var mongoClient = new MongoClient(configuration.MongoConnectionString);
            var mongoDb = mongoClient.GetDatabase(configuration.CleanProjectName);
            
            // get collection refs
            _commitCollection = mongoDb.GetCollection<BsonDocument>("commits");
            _releaseCollection = mongoDb.GetCollection<BsonDocument>("releases");
            _statisticsCollection = mongoDb.GetCollection<BsonDocument>("statistics");
            _usersCollection = mongoDb.GetCollection<BsonDocument>("users");
        }

        public async Task TruncateCommits()
        {
            await _commitCollection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        }

        public async Task InsertCommits(IEnumerable<Commit> commits)
        {
            var docs = commits.Select(o =>
            {
                var doc = o.BsonDocument;
                doc["_id"] = Guid.NewGuid();

                return o.BsonDocument;
            });
            await _commitCollection.InsertManyAsync(docs);
        }

        public async Task<List<Commit>> GetCommits(FilterDefinition<BsonDocument> filter = null)
        {
            filter = filter ?? FilterDefinition<BsonDocument>.Empty;
            var docs = await _commitCollection.Find(filter).ToListAsync();

            return docs.Select(o => new Commit(o)).ToList();
        }

        public async Task RecacheReleases(IEnumerable<BsonDocument> releases)
        {
            await _releaseCollection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
            await _releaseCollection.InsertManyAsync(releases);
        }

        public async Task<List<BsonDocument>> GetReleases(FilterDefinition<BsonDocument> filter = null)
        {
            filter = filter ?? FilterDefinition<BsonDocument>.Empty;
            return await _releaseCollection.Find(filter).ToListAsync();
        }

        public async Task RecacheStatistics(IEnumerable<BsonDocument> statistics)
        {
            await _statisticsCollection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
            await _statisticsCollection.InsertManyAsync(statistics);
        }

        public async Task<List<BsonDocument>> GetStatistics(FilterDefinition<BsonDocument> filter = null)
        {
            filter = filter ?? FilterDefinition<BsonDocument>.Empty;
            return await _statisticsCollection.Find(filter).ToListAsync();
        }

        public async Task RecacheUsers(IEnumerable<BsonDocument> statistics)
        {
            await _usersCollection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
            await _usersCollection.InsertManyAsync(statistics);
        }

        public async Task<List<BsonDocument>> GetUsers(FilterDefinition<BsonDocument> filter = null)
        {
            filter = filter ?? FilterDefinition<BsonDocument>.Empty;
            return await _usersCollection.Find(filter).ToListAsync();
        }
    }
}
