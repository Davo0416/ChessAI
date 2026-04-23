using ChessAIWebAPI.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;

namespace ChessAIWebAPI.Services
{
    public class MongoService
    {
        public IMongoCollection<User> Users { get; }
        public IMongoCollection<Game> Games { get; }


        public MongoService(IConfiguration config)
        {
            var connectionString = config["MongoDb:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("MongoDB connection string is missing!");

            var client = new MongoClient(connectionString);

            _database = client.GetDatabase("ChessApp");
        }

        public MongoService(IMongoCollection<User> users, IMongoCollection<Game> games)
        {
            Users = users;
            Games = games;
        }

        public IMongoCollection<User> Users =>
            _database.GetCollection<User>("Users");

        public IMongoCollection<Game> Games =>
            _database.GetCollection<Game>("Games");
    }
}