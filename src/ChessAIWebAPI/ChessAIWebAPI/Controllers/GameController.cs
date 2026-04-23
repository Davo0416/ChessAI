namespace ChessAIWebAPI.Controllers
{
    using ChessAIWebAPI.Models;
    using ChessAIWebAPI.Services;
    using Microsoft.AspNetCore.Mvc;
    using MongoDB.Driver;

    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly MongoService _mongo;

        public GameController(MongoService mongo)
        {
            _mongo = mongo;
        }

        // SAVE GAME
        [HttpPost("save")]
        public async Task<IActionResult> SaveGame([FromBody] Game game)
        {
            var exists = await _mongo.Games.Find(x =>
                x.Username == game.Username &&
                x.ClientId == game.ClientId
            ).AnyAsync();

            if (!exists)
            {
                await _mongo.Games.InsertOneAsync(game);
            }

            return Ok();
        }

        [HttpGet("sync/{username}")]
        public async Task<IActionResult> Sync(string username, DateTime? lastSync)
        {
            var filter = Builders<Game>.Filter.Eq(x => x.Username, username);

            if (lastSync.HasValue)
            {
                filter &= Builders<Game>.Filter.Gt(x => x.LastModified, lastSync.Value);
            }

            var games = await _mongo.Games.Find(filter).ToListAsync();

            return Ok(games);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var games = await _mongo.Games.Find(_ => true).ToListAsync();
            return Ok(games);
        }

        [HttpDelete("clear/{username}")]
        public async Task<IActionResult> ClearUserGames(string username)
        {
            var filter = Builders<Game>.Filter.Eq(x => x.Username, username);

            await _mongo.Games.DeleteManyAsync(filter);

            return Ok($"Deleted all games for {username}");
        }
    }
}
