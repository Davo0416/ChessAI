using Xunit;
using Moq;
using ChessAIWebAPI.Controllers;
using ChessAIWebAPI.Services;
using ChessAIWebAPI.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class AuthControllerTests
{
    private readonly Mock<MongoService> _mongoMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mongoMock = new Mock<MongoService>();
        _controller = new AuthController(_mongoMock.Object);
    }

    [Fact]
    public async Task Signup_ReturnsBadRequest_WhenUserExists()
    {
        // Arrange
        var user = new User { Username = "test", PasswordHash = "pass" };

        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockFind = new Mock<IAsyncCursor<User>>();

        mockFind.Setup(x => x.Current)
            .Returns(new List<User> { user });

        mockFind.SetupSequence(x => x.MoveNextAsync(default))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        mockCollection.Setup(x =>
            x.FindAsync(It.IsAny<FilterDefinition<User>>(),
            It.IsAny<FindOptions<User, User>>(),
            default))
            .ReturnsAsync(mockFind.Object);

        _mongoMock.Setup(x => x.Users).Returns(mockCollection.Object);

        // Act
        var result = await _controller.Signup(user);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Signup_ReturnsOk_WhenNewUser()
    {
        var user = new User { Username = "newuser", PasswordHash = "pass" };

        var mockCollection = new Mock<IMongoCollection<User>>();

        mockCollection.Setup(x =>
            x.Find(It.IsAny<FilterDefinition<User>>(), null))
            .Returns(Mock.Of<IFindFluent<User, User>>());

        _mongoMock.Setup(x => x.Users).Returns(mockCollection.Object);

        var result = await _controller.Signup(user);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
    {
        var login = new User { Username = "ghost", PasswordHash = "123" };

        var mockCollection = new Mock<IMongoCollection<User>>();

        mockCollection.Setup(x =>
            x.Find(It.IsAny<FilterDefinition<User>>(), null))
            .Returns(Mock.Of<IFindFluent<User, User>>());

        _mongoMock.Setup(x => x.Users).Returns(mockCollection.Object);

        var result = await _controller.Login(login);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}