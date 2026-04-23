using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using ChessAIWebAPI.Controllers;
using ChessAIWebAPI.Controllers;
using ChessAIWebAPI.Models;
using ChessAIWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class AuthControllerTests
{
    // Helper to create a mock IAsyncCursor<T>
    private static Mock<IAsyncCursor<T>> CreateMockCursor<T>(IEnumerable<T> items)
    {
        var cursor = new Mock<IAsyncCursor<T>>();
        var list = items.ToList();
        var call = 0;

        cursor.Setup(c => c.Current).Returns(list);
        cursor.Setup(c => c.MoveNext(It.IsAny<CancellationToken>()))
              .Returns(() => call++ == 0 && list.Any());
        cursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(() => call++ == 0 && list.Any());

        return cursor;
    }

    // Helper to mock FindAsync and return a cursor
    private static void SetupFindAsync<T>(Mock<IMongoCollection<T>> collection, 
                                          Expression<Func<T, bool>> filter, 
                                          IEnumerable<T> results)
    {
        var cursor = CreateMockCursor(results);
        collection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<T>>(),
                It.IsAny<FindOptions<T, T>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor.Object);
    }

    [Fact]
    public async Task Signup_NewUser_ReturnsOk()
    {
        // Arrange
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockMongo = new Mock<MongoService>();
        mockMongo.Setup(m => m.Users).Returns(mockCollection.Object);

        // No existing user
        SetupFindAsync(mockCollection, u => u.Username == "newuser", Enumerable.Empty<User>());

        var user = new User { Username = "newuser", PasswordHash = "plainPassword" };
        var controller = new AuthController(mockMongo.Object);

        // Act
        var result = await controller.Signup(user);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User created", okResult.Value);
        mockCollection.Verify(x => x.InsertOneAsync(user, null, default), Times.Once);
        // Password should be hashed
        Assert.True(BCrypt.Net.BCrypt.Verify("plainPassword", user.PasswordHash));
    }

    [Fact]
    public async Task Signup_UsernameAlreadyExists_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new User { Username = "existing", PasswordHash = "hash" };
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockMongo = new Mock<MongoService>();
        mockMongo.Setup(m => m.Users).Returns(mockCollection.Object);

        SetupFindAsync(mockCollection, u => u.Username == "existing", new[] { existingUser });

        var user = new User { Username = "existing", PasswordHash = "any" };
        var controller = new AuthController(mockMongo.Object);

        // Act
        var result = await controller.Signup(user);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Username already exists", badRequest.Value);
        mockCollection.Verify(x => x.InsertOneAsync(It.IsAny<User>(), null, default), Times.Never);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithUser()
    {
        // Arrange
        var plainPassword = "secret";
        var hashed = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        var dbUser = new User { Username = "john", PasswordHash = hashed };

        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockMongo = new Mock<MongoService>();
        mockMongo.Setup(m => m.Users).Returns(mockCollection.Object);

        SetupFindAsync(mockCollection, u => u.Username == "john", new[] { dbUser });

        var login = new User { Username = "john", PasswordHash = plainPassword };
        var controller = new AuthController(mockMongo.Object);

        // Act
        var result = await controller.Login(login);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dbUser, okResult.Value);
    }

    [Fact]
    public async Task Login_UserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockMongo = new Mock<MongoService>();
        mockMongo.Setup(m => m.Users).Returns(mockCollection.Object);

        SetupFindAsync(mockCollection, u => u.Username == "unknown", Enumerable.Empty<User>());

        var login = new User { Username = "unknown", PasswordHash = "anything" };
        var controller = new AuthController(mockMongo.Object);

        // Act
        var result = await controller.Login(login);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User not found", unauthorized.Value);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var dbUser = new User { Username = "john", PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct") };
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockMongo = new Mock<MongoService>();
        mockMongo.Setup(m => m.Users).Returns(mockCollection.Object);

        SetupFindAsync(mockCollection, u => u.Username == "john", new[] { dbUser });

        var login = new User { Username = "john", PasswordHash = "wrong" };
        var controller = new AuthController(mockMongo.Object);

        // Act
        var result = await controller.Login(login);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid password", unauthorized.Value);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Username = "user1" },
            new User { Username = "user2" }
        };
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockMongo = new Mock<MongoService>();
        mockMongo.Setup(m => m.Users).Returns(mockCollection.Object);

        // Mock Find(_ => true).ToListAsync()
        var cursor = CreateMockCursor(users);
        mockCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor.Object);

        var controller = new AuthController(mockMongo.Object);

        // Act
        var result = await controller.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(users, okResult.Value);
    }

    [Fact]
    public async Task DeleteUser_UserExists_ReturnsOk()
    {
        // Arrange
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockMongo = new Mock<MongoService>();
        mockMongo.Setup(m => m.Users).Returns(mockCollection.Object);

        mockCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        var controller = new AuthController(mockMongo.Object);

        // Act
        var result = await controller.DeleteUser("existingUser");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User deleted", okResult.Value);
    }

    [Fact]
    public async Task DeleteUser_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockMongo = new Mock<MongoService>();
        mockMongo.Setup(m => m.Users).Returns(mockCollection.Object);

        mockCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(0));

        var controller = new AuthController(mockMongo.Object);

        // Act
        var result = await controller.DeleteUser("missing");

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found", notFound.Value);
    }

    [Fact]
    public async Task UpdatePreferences_UserExists_ReturnsOk()
    {
        // Arrange
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockMongo = new Mock<MongoService>();
        mockMongo.Setup(m => m.Users).Returns(mockCollection.Object);

        mockCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var updatedUser = new User { PieceSet = "newPieces", Theme = "dark" };
        var controller = new AuthController(mockMongo.Object);

        // Act
        var result = await controller.UpdatePreferences("existingUser", updatedUser);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Preferences updated", okResult.Value);
    }

    [Fact]
    public async Task UpdatePreferences_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockCollection = new Mock<IMongoCollection<User>>();
        var mockMongo = new Mock<MongoService>();
        mockMongo.Setup(m => m.Users).Returns(mockCollection.Object);

        mockCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        var updatedUser = new User { PieceSet = "any", Theme = "any" };
        var controller = new AuthController(mockMongo.Object);

        // Act
        var result = await controller.UpdatePreferences("missing", updatedUser);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found", notFound.Value);
    }
}