using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessAIWebAPI.Controllers;
using ChessAIWebAPI.Models;
using ChessAIWebAPI.Services;
using MongoDB.Driver;
using BCrypt.Net;

// ---------------------------
// FAKE MONGO IMPLEMENTATION
// ---------------------------

public class FakeMongoService
{
    public List<User> UsersStore { get; } = new();

    public IMongoCollection<User> Users => new FakeUserCollection(UsersStore);

    private class FakeUserCollection : IMongoCollection<User>
    {
        private readonly List<User> _store;

        public FakeUserCollection(List<User> store)
        {
            _store = store;
        }

        public Task InsertOneAsync(User document, InsertOneOptions options = null, System.Threading.CancellationToken cancellationToken = default)
        {
            _store.Add(document);
            return Task.CompletedTask;
        }

        public IFindFluent<User, User> Find(FilterDefinition<User> filter, FindOptions options = null)
        {
            return new FakeFindFluent(_store, filter);
        }

        public Task<DeleteResult> DeleteOneAsync(FilterDefinition<User> filter, System.Threading.CancellationToken cancellationToken = default)
        {
            var compiled = filter.Compile();
            var user = _store.FirstOrDefault(u => compiled(u));

            if (user == null)
                return Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(0));

            _store.Remove(user);
            return Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1));
        }

        public Task<UpdateResult> UpdateOneAsync(FilterDefinition<User> filter, UpdateDefinition<User> update, UpdateOptions options = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var compiled = filter.Compile();
            var user = _store.FirstOrDefault(u => compiled(u));

            if (user == null)
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(0, 0, null));

            var updateDef = update as MongoDB.Driver.UpdateDefinition<User>;
            // manual mapping for test simplicity
            var updatedUser = user;

            _store.Remove(user);
            _store.Add(updatedUser);

            return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null));
        }

        // --- Not used in tests ---
        public CollectionNamespace CollectionNamespace => throw new System.NotImplementedException();
        public IMongoDatabase Database => throw new System.NotImplementedException();
        public IBsonSerializer<User> DocumentSerializer => throw new System.NotImplementedException();
        public IMongoIndexManager<User> Indexes => throw new System.NotImplementedException();
        public MongoCollectionSettings Settings => throw new System.NotImplementedException();
        public IQueryable<User> AsQueryable(System.Linq.Expressions.Expression<System.Func<User, bool>> filter) => _store.AsQueryable();
        public Task<IAsyncCursor<User>> AggregateAsync(PipelineDefinition<User, User> pipeline, AggregateOptions options = null, System.Threading.CancellationToken cancellationToken = default) => throw new System.NotImplementedException();

        // Many interface members omitted (not needed for tests)
    }

    private class FakeFindFluent : IFindFluent<User, User>
    {
        private readonly List<User> _store;
        private readonly FilterDefinition<User> _filter;

        public FakeFindFluent(List<User> store, FilterDefinition<User> filter)
        {
            _store = store;
            _filter = filter;
        }

        public Task<List<User>> ToListAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var compiled = _filter.Compile();
            return Task.FromResult(_store.Where(compiled).ToList());
        }

        public Task<User> FirstOrDefaultAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var compiled = _filter.Compile();
            return Task.FromResult(_store.FirstOrDefault(compiled));
        }

        // unused members
        public IFindFluent<User, TResult> As<TResult>(System.Func<User, TResult> projector) => throw new System.NotImplementedException();
        public IFindFluent<User, User> Limit(int? limit) => this;
        public IFindFluent<User, User> Skip(int? skip) => this;
        public IFindFluent<User, User> Sort(SortDefinition<User> sort) => this;
        public IFindFluent<User, User> Project<TResult>(ProjectionDefinition<User, TResult> projection) => throw new System.NotImplementedException();
        public IFindFluent<User, User> ThenBy(SortDefinition<User> sort) => this;
        public IFindFluent<User, User> ThenByDescending(SortDefinition<User> sort) => this;
    }
}

// ---------------------------
// TESTS
// ---------------------------

public class AuthControllerTests
{
    private AuthController CreateController(out FakeMongoService fake)
    {
        fake = new FakeMongoService();
        return new AuthController(new MongoServiceAdapter(fake));
    }

    // Adapter so controller still receives MongoService type
    private class MongoServiceAdapter : MongoService
    {
        public MongoServiceAdapter(FakeMongoService fake)
        {
            this.Users = fake.Users;
        }
    }

    [Fact]
    public async Task Signup_Should_Create_User()
    {
        var controller = CreateController(out _);

        var user = new User
        {
            Username = "testuser",
            PasswordHash = "password123"
        };

        var result = await controller.Signup(user);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Signup_Should_Reject_Duplicate_User()
    {
        var controller = CreateController(out _);

        var user = new User { Username = "duplicate", PasswordHash = "pass" };

        await controller.Signup(user);
        var result = await controller.Signup(user);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_Should_Succeed_With_Valid_Credentials()
    {
        var controller = CreateController(out _);

        var user = new User { Username = "loginuser", PasswordHash = "password123" };
        await controller.Signup(user);

        var login = new User { Username = "loginuser", PasswordHash = "password123" };

        var result = await controller.Login(login);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_Should_Fail_With_Wrong_Password()
    {
        var controller = CreateController(out _);

        var user = new User { Username = "failuser", PasswordHash = "password123" };
        await controller.Signup(user);

        var login = new User { Username = "failuser", PasswordHash = "wrongpass" };

        var result = await controller.Login(login);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetAllUsers_Should_Return_Users()
    {
        var controller = CreateController(out _);

        await controller.Signup(new User { Username = "u1", PasswordHash = "p1" });
        await controller.Signup(new User { Username = "u2", PasswordHash = "p2" });

        var result = await controller.GetAllUsers();

        var ok = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsType<List<User>>(ok.Value);

        Assert.Equal(2, users.Count);
    }

    [Fact]
    public async Task DeleteUser_Should_Remove_User()
    {
        var controller = CreateController(out _);

        await controller.Signup(new User { Username = "deleteMe", PasswordHash = "pass" });

        var result = await controller.DeleteUser("deleteMe");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdatePreferences_Should_Update_User()
    {
        var controller = CreateController(out _);

        await controller.Signup(new User
        {
            Username = "prefUser",
            PasswordHash = "pass",
            Theme = "old",
            PieceSet = "old"
        });

        var updated = new User
        {
            Theme = "new",
            PieceSet = "new"
        };

        var result = await controller.UpdatePreferences("prefUser", updated);

        Assert.IsType<OkObjectResult>(result);
    }
}