using System.Linq;
using MongoDB.Bson;

namespace DeveloperNetwork.Models
{
    public class GeocodeComponents
    {
        public GeocodeComponents(BsonDocument document)
        {
            // get components and set props
            var results = document["results"].AsBsonArray.First();
            var components = results["address_components"].AsBsonArray;

            Locality = FindComponentTypeValue(components, "locality");
            AdminAreaLevel1 = FindComponentTypeValue(components, "administrative_area_level_1");
            AdminAreaLevel2 = FindComponentTypeValue(components, "administrative_area_level_2");
            Country = FindComponentTypeValue(components, "country");
        }

        private string FindComponentTypeValue(BsonArray components, string type)
        {
            // TODO: Make this a Bson extension for easy access

            // try to find the right component type
            var component = components.FirstOrDefault(o =>
            {
                return o["types"].AsBsonArray.Any(t => t.AsString == type);
            });

            // make sure we found it and it's not null or empty
            var value = component == null ||
                        component == BsonNull.Value
                ? null
                : component["short_name"].AsString;

            return value;
        }

        public string Locality { get; }

        public string AdminAreaLevel1 { get; }

        public string AdminAreaLevel2 { get; }

        public string Country { get; }
    }
}