using Xunit;
using Moq;
using ChessAIWebAPI.Controllers;
using ChessAIWebAPI.Services;
using ChessAIWebAPI.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class GameControllerTests
{
    private readonly Mock<MongoService> _mongoMock;
    private readonly GameController _controller;

    public GameControllerTests()
    {
        var mockUsers = new Mock<IMongoCollection<User>>();
        var mockGames = new Mock<IMongoCollection<Game>>();

        _mongoMock = new MongoService(mockUsers.Object, mockGames.Object);
        _controller = new AuthController(mongo);
    }

    [Fact]
    public async Task SaveGame_ReturnsOk()
    {
        var game = new Game
        {
            Username = "test",
            ClientId = "1"
        };

        var mockCollection = new Mock<IMongoCollection<Game>>();
        _mongoMock.Setup(x => x.Games).Returns(mockCollection.Object);

        var result = await _controller.SaveGame(game);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult()
    {
        var mockCollection = new Mock<IMongoCollection<Game>>();

        mockCollection.Setup(x =>
            x.Find(It.IsAny<FilterDefinition<Game>>(), null))
            .Returns(Mock.Of<IFindFluent<Game, Game>>());

        _mongoMock.Setup(x => x.Games).Returns(mockCollection.Object);

        var result = await _controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ClearUserGames_ReturnsOk()
    {
        var mockCollection = new Mock<IMongoCollection<Game>>();

        _mongoMock.Setup(x => x.Games).Returns(mockCollection.Object);

        var result = await _controller.ClearUserGames("test");

        Assert.IsType<OkObjectResult>(result);
    }
}