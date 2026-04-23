namespace ChessAIWebAPI.Models
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string Username { get; set; }
        public string PieceSet { get; set; }
        public string Theme { get; set; }
        public string PasswordHash { get; set; }
    }
}
