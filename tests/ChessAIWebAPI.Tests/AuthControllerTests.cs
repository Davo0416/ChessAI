using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using ChessAIWebAPI.Controllers;
using ChessAIWebAPI.Models;
using ChessAIWebAPI.Services;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AuthControllerTests
{
    private readonly Mock<IMongoService> _mongoMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mongoMock = new Mock<IMongoService>();
        _controller = new AuthController(_mongoMock.Object);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
    {
        var mockUsers = new Mock<IMongoCollection<User>>();
        var mockCursor = new Mock<IAsyncCursor<User>>();

        mockCursor.Setup(x => x.Current).Returns(new List<User>());
        mockCursor.SetupSequence(x => x.MoveNextAsync(default))
                  .ReturnsAsync(false);

        mockUsers.Setup(x =>
            x.FindAsync(It.IsAny<FilterDefinition<User>>(),
                        It.IsAny<FindOptions<User>>(),
                        default))
                 .ReturnsAsync(mockCursor.Object);

        _mongoMock.Setup(x => x.Users).Returns(mockUsers.Object);

        var result = await _controller.Login(new User
        {
            Username = "ghost",
            PasswordHash = "123"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}