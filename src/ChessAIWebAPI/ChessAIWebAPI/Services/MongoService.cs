using ChessAIWebAPI.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;

namespace ChessAIWebAPI.Services
{
    public class MongoService
    {
        private readonly IMongoDatabase _database;

        public MongoService(IConfiguration config)
        {
            var connectionString = config["MongoDb:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("MongoDB connection string is missing!");

            var client = new MongoClient(connectionString);

            _database = client.GetDatabase("ChessApp");
        }

        public IMongoCollection<User> Users =>
            _database.GetCollection<User>("Users");

        public IMongoCollection<Game> Games =>
            _database.GetCollection<Game>("Games");
    }
}