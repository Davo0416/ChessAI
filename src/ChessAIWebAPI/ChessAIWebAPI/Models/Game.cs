namespace ChessAIWebAPI.Models
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    public class Game
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Username { get; set; }

        public string JsonData { get; set; }

        public string ClientId { get; set; } = null!;

        public DateTime LastModified { get; set; }
    }
}
