using System.Net.Cache;
using System.Threading.Tasks;
using DeveloperNetwork.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using RestSharp;

namespace DeveloperNetwork.Services
{
    public class GoogleProxy
    {
        private const string ApiUrl = "https://maps.googleapis.com/maps/api";
        private readonly string _apiKey;

        public GoogleProxy(ProgramConfiguration configuration, ILogger<GoogleProxy> logger)
        {
            _apiKey = configuration.GoogleKey;
        }

        private static RestClient RestClient => new RestClient(ApiUrl)
        {
            CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
        };

        public async Task<GeocodeComponents> LookupGeocode(string query)
        {
            // build request
            var request = new RestRequest("geocode/json");
            request.AddQueryParameter("key", _apiKey);
            request.AddQueryParameter("address", query);

            // get results
            var response = await RestClient.ExecuteGetTaskAsync(request);
            
            using (var jsonReader = new MongoDB.Bson.IO.JsonReader(response.Content))
            {
                // parse to bson
                var serializer = new BsonDocumentSerializer();
                var bsonDocument = serializer.Deserialize(BsonDeserializationContext.CreateRoot(jsonReader));

                // cast to geocode components
                var geocode = new GeocodeComponents(bsonDocument);
                return geocode;
            }
        }
    }
}
