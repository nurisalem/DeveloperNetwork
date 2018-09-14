using System;
using MongoDB.Bson;

namespace DeveloperNetwork.Models
{
    public class Commit
    {
        public Commit(BsonDocument document)
        {
            // save document
            BsonDocument = document;

            // set author
            var author = BsonDocument["author"];
            Author = author != BsonNull.Value ? 
                author["login"].AsString : 
                BsonDocument["commit"]["author"]["email"].AsString;

            // set commit date
            Date = DateTime.Parse(BsonDocument["commit"]["committer"]["date"].AsString);

            // set files
            Files = BsonDocument["files"].AsBsonArray;
        }

        public string Author { get; private set; }

        public DateTime Date { get; }

        public BsonArray Files { get; }

        public BsonDocument BsonDocument { get; }

        public void UpdateAuthor(string value)
        {
            Author = value;
        }
    }
}
