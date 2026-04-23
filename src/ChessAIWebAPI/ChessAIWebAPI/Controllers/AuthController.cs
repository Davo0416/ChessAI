using Microsoft.AspNetCore.Mvc;

namespace ChessAIWebAPI.Controllers
{
    using ChessAIWebAPI.Models;
    using ChessAIWebAPI.Services;
    using Microsoft.AspNetCore.Mvc;
    using MongoDB.Driver;

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MongoService _mongo;

        public AuthController(MongoService mongo)
        {
            _mongo = mongo;
        }

        // SIGNUP
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] User user)
        {
            var existingUser = await _mongo.Users
                .Find(x => x.Username == user.Username)
                .FirstOrDefaultAsync();

            if (existingUser != null)
                return BadRequest("Username already exists");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            await _mongo.Users.InsertOneAsync(user);

            return Ok("User created");
        }

        // LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User login)
        {
            var user = await _mongo.Users
                .Find(x => x.Username == login.Username)
                .FirstOrDefaultAsync();

            if (user == null)
                return Unauthorized("User not found");

            bool valid = BCrypt.Net.BCrypt.Verify(login.PasswordHash, user.PasswordHash);

            if (!valid)
                return Unauthorized("Invalid password");

            return Ok(user);
        }

        // GET ALL USERS
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _mongo.Users
                .Find(_ => true)
                .ToListAsync();

            return Ok(users);
        }

        // DELETE USER BY ID
        [HttpDelete("{username}")]
        public async Task<IActionResult> DeleteUser(string username)
        {
            var result = await _mongo.Users.DeleteOneAsync(u => u.Username == username);

            if (result.DeletedCount == 0)
                return NotFound("User not found");

            return Ok("User deleted");
        }

        // UPDATE USER SETTINGS
        [HttpPut("{username}/preferences")]
        public async Task<IActionResult> UpdatePreferences(string username, [FromBody] User updatedUser)
        {
            var update = Builders<User>.Update
                .Set(u => u.PieceSet, updatedUser.PieceSet)
                .Set(u => u.Theme, updatedUser.Theme);

            var result = await _mongo.Users.UpdateOneAsync(
                u => u.Username == username,
                update
            );

            if (result.MatchedCount == 0)
                return NotFound("User not found");

            return Ok("Preferences updated");
        }
    }
}
