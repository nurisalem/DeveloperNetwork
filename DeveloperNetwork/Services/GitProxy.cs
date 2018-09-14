using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using DeveloperNetwork.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using RestSharp;

namespace DeveloperNetwork.Services
{
    public class GitProxy
    {
        private const string GitApiUrl = "https://api.github.com";
        private readonly string _token;
        private readonly string _repositoryUrl;
        private readonly ILogger<GitProxy> _logger;

        // TODO: Abstract request paging into a method to avoid code duplication

        public GitProxy(ProgramConfiguration configuration, ILogger<GitProxy> logger)
        {
            _logger = logger;
            _token = configuration.GitToken;
            _repositoryUrl = configuration.ProjectUriShorthand;
        }

        private static RestClient GitClient => new RestClient(GitApiUrl)
        {
            CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
        };

        private static string GetHeader(IRestResponse response, string name)
        {
            return response.Headers.FirstOrDefault(o => o.Name == name)?.Value.ToString();
        }

        private static string GetRateLimitReset(IRestResponse response)
        {
            return DateTimeOffset
                .FromUnixTimeSeconds(long.Parse(GetHeader(response, "X-RateLimit-Reset")))
                .ToLocalTime()
                .ToString("G");
        }

        public async Task GetCommits()
        {
            // retrieve all commits
            var pageCount = 1;
            var totalPages = 0;
            var nextPageUrl = $"repos/{_repositoryUrl}/commits?page=1";
            IRestResponse response;
            do
            {
                // retrieve page results
                response = await GitClient.ExecuteAsGitGetTaskAsync(_token, nextPageUrl);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError(response.Content);
                    throw new HttpRequestException("Couldn't retrieve Git data, check error message for details.");
                }

                var results = new List<BsonDocument>();

                using (var jsonReader = new MongoDB.Bson.IO.JsonReader(response.Content))
                {
                    var serializer = new BsonArraySerializer();
                    var bsonArray = serializer.Deserialize(BsonDeserializationContext.CreateRoot(jsonReader));

                    results.AddRange(bsonArray.Where(o => o.IsBsonDocument).Select(o => o.AsBsonDocument));
                }

                // the above request only provides partial commit information, go through all of them
                //  and retrieve their full details
                var fullCommitTasks = results
                    .Select(async commit =>
                    {
                        // build commit request with the SHA as commit ID
                        var sha = commit["sha"].AsString;
                        var fullResponse = await GitClient.ExecuteAsGitGetTaskAsync(_token, $"repos/{_repositoryUrl}/commits/{sha}");

                        // parse and return document
                        var document = BsonDocument.Parse(fullResponse.Content);
                        return new Commit(document);
                    })
                    .ToList();

                // wait for task completion
                var fullCommits = await Task.WhenAll(fullCommitTasks);
                OnRaiseCommitQueryNext(fullCommits);

                // make sure the response came with a header, otherwise we're done
                var linkHeader = GetHeader(response, "Link");
                if (linkHeader == null)
                    break;

                // make sure there's a next page, otherwise we're done
                var nextLinkMatch = Regex.Match(linkHeader, "<https:\\/\\/api\\.github\\.com\\/(.+?)>; rel=\"next\"");
                if (!nextLinkMatch.Success)
                    break;

                // get number of total pages
                if (totalPages == 0)
                {
                    var totalPagesMatch = Regex.Match(linkHeader, "page=(\\d+)>; rel=\"last");
                    if (totalPagesMatch.Success)
                        totalPages = int.Parse(totalPagesMatch.Groups[1].Value);
                }

                // log
                _logger.LogInformation(
                    "Got page {0} of {1}, " +
                    "X-RateLimit-Remaining: {2}, " +
                    "X-RateLimit-Reset: {3}",
                    pageCount,
                    totalPages,
                    GetHeader(response, "X-RateLimit-Remaining"),
                    GetRateLimitReset(response));

                // create new request and keep going
                nextPageUrl = nextLinkMatch.Groups[1].Value;
                pageCount++;
            } while (true);

            // log
            _logger.LogInformation(
                "Got page {0} of {1}\n" +
                "\tX-RateLimit-Remaining: {2}\n" +
                "\tX-RateLimit-Reset: {3}",
                totalPages,
                totalPages,
                GetHeader(response, "X-RateLimit-Remaining"),
                GetRateLimitReset(response));
        }

        public event EventHandler<Commit[]> OnCommitQueryNext;

        protected virtual void OnRaiseCommitQueryNext(Commit[] commits)
        {
            var handler = OnCommitQueryNext;
            handler?.Invoke(this, commits);
        }

        public async Task<List<BsonDocument>> GetReleases()
        {
            // retrieve all releases
            var results = new List<BsonDocument>();
            var nextPageUrl = $"repos/{_repositoryUrl}/releases?page=1";
            do
            {
                // retrieve pages results
                var response = await GitClient.ExecuteAsGitGetTaskAsync(_token, nextPageUrl);

                using (var jsonReader = new MongoDB.Bson.IO.JsonReader(response.Content))
                {
                    var serializer = new BsonArraySerializer();
                    var bsonArray = serializer.Deserialize(BsonDeserializationContext.CreateRoot(jsonReader));

                    results.AddRange(bsonArray.Where(o => o.IsBsonDocument).Select(o => o.AsBsonDocument));
                }

                // make sure the response came with a header, otherwise we're done
                var linkHeader = response.Headers.FirstOrDefault(o => o.Name == "Link")?.Value.ToString();
                if (linkHeader == null)
                    break;

                // make sure there's a next page, otherwise we're done
                var nextLinkMatch = Regex.Match(linkHeader, "<https:\\/\\/api\\.github\\.com\\/(.+?)>; rel=\"next\"");
                if (!nextLinkMatch.Success)
                    break;

                // create new request and keep going
                nextPageUrl = nextLinkMatch.Groups[1].Value;
            } while (true);

            return results;
        }

        public async Task<List<BsonDocument>> GetStatistics()
        {
            // retrieve stats
            var results = new List<BsonDocument>();
            var nextPageUrl = $"repos/{_repositoryUrl}/stats/contributors";

            // retrieve results
            var response = await GitClient.ExecuteAsGitGetTaskAsync(_token, nextPageUrl);

            // make sure the calculation has been completed
            // https://developer.github.com/v3/repos/statistics/#a-word-about-caching
            while (response.StatusCode == HttpStatusCode.Accepted)
            {
                Thread.Sleep(5000);
                response = await GitClient.ExecuteAsGitGetTaskAsync(_token, nextPageUrl);
            }

            using (var jsonReader = new MongoDB.Bson.IO.JsonReader(response.Content))
            {
                var serializer = new BsonArraySerializer();
                var bsonArray = serializer.Deserialize(BsonDeserializationContext.CreateRoot(jsonReader));

                results.AddRange(bsonArray.Where(o => o.IsBsonDocument).Select(o => o.AsBsonDocument));
            }

            return results;
        }

        public async Task<List<BsonDocument>> GetUsers(List<BsonDocument> statistics)
        {
            var users = await Task.WhenAll(statistics.Select(async o =>
            {
                // get user login
                var login = o["author"]["login"].AsString;

                // retrieve user
                var requestUrl = $"users/{login}";
                var response = await GitClient.ExecuteAsGitGetTaskAsync(_token, requestUrl);

                // parse and return
                using (var jsonReader = new MongoDB.Bson.IO.JsonReader(response.Content))
                {
                    var serializer = new BsonDocumentSerializer();
                    var bsonDocument = serializer.Deserialize(BsonDeserializationContext.CreateRoot(jsonReader));

                    return bsonDocument;
                }
            }));

            return new List<BsonDocument>(users);
        }
    }

    static class GitExtensions
    {
        public static async Task<IRestResponse> ExecuteAsGitGetTaskAsync(this RestClient client, string token, string resource)
        {
            var request = GitRequest(token, resource);
            return await client.ExecuteGetTaskAsync(request);
        }

        private static RestRequest GitRequest(string token, string resource)
        {
            var request = new RestRequest(resource);
            request.AddHeader("Authorization", $"token {token}");
            return request;
        }
    }
}