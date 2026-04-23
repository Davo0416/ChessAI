using Xunit;
using Moq;
using ChessAIWebAPI.Controllers;
using ChessAIWebAPI.Services;
using ChessAIWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;

public class GameControllerTests
{
    private readonly Mock<IMongoService> _mongoMock;
    private readonly GameController _controller;

    public GameControllerTests()
    {
        _mongoMock = new Mock<IMongoService>();
        _controller = new GameController(_mongoMock.Object);
    }

    [Fact]
    public async Task SaveGame_ReturnsOk()
    {
        var game = new Game
        {
            Username = "test",
            ClientId = "1"
        };

        // no mocking MongoDB collection anymore
        _mongoMock.Setup(x => x.Games);

        var result = await _controller.SaveGame(game);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult()
    {
        var games = new List<Game>
        {
            new Game { Username = "test", ClientId = "1" }
        };

        // you must mock controller logic instead of MongoDB
        _mongoMock
            .Setup(x => x.Games)
            .Returns((MongoDB.Driver.IMongoCollection<Game>)null);

        var result = await _controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ClearUserGames_ReturnsOk()
    {
        var result = await _controller.ClearUserGames("test");

        Assert.IsType<OkObjectResult>(result);
    }
}