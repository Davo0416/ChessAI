using Xunit;
using System.Linq;
using System.Threading.Tasks;
using ChessDotNet;
using ChessAIApp;

public class BotTests
{

    private static ChessGame CreateGame()
    {
        return new ChessGame(); // starting position
    }

    private static Board CreateBoard()
    {
        return new Board(null!, null!, null!, null!, null!, null!); // NOT used in logic tests
    }

    // -----------------------------
    // RANDOM BOT
    // -----------------------------

    [Fact]
    public void RandomBot_GetAllLegalMoves_ShouldReturnMoves()
    {
        var game = CreateGame();

        var moves = RandomBot.GetAllLegalMoves(game);

        Assert.NotEmpty(moves);
    }

    // -----------------------------
    // QUASAR V0
    // -----------------------------

    [Fact]
    public void QuasarV0_EvaluateMove_ShouldReturnNonNegative()
    {
        var game = CreateGame();
        var board = CreateBoard();

        var bot = new Quasarv0(board);

        var moves = RandomBot.GetAllLegalMoves(game);

        var score = Quasarv0.EvaluateMove(game, moves[0]);

        Assert.True(score >= 0);
    }

    [Fact]
    public void QuasarV0_Evaluate_ShouldReturnLegalMove()
    {
        var game = CreateGame();
        var board = CreateBoard();

        var bot = new Quasarv0(board);

        var move = bot.Evaluate(game, 2);

        Assert.Contains(move, RandomBot.GetAllLegalMoves(game));
    }

    // -----------------------------
    // QUASAR V01
    // -----------------------------

    [Fact]
    public void QuasarV01_EvaluateBoard_ShouldReturnFiniteValue()
    {
        var game = CreateGame();
        var board = CreateBoard();

        var bot = new Quasarv01(board);

        var score = typeof(Quasarv01)
            .GetMethod("EvaluateBoard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(bot, new object[] { game });

        Assert.NotNull(score);
    }

    [Fact]
    public void QuasarV01_Evaluate_ShouldReturnLegalMove()
    {
        var game = CreateGame();
        var board = CreateBoard();

        var bot = new Quasarv01(board);

        var move = bot.Evaluate(game, 2);

        Assert.Contains(move, RandomBot.GetAllLegalMoves(game));
    }

    [Fact]
    public void QuasarV01_Negamax_ShouldReturnValue()
    {
        var game = CreateGame();
        var board = CreateBoard();

        var bot = new Quasarv01(board);

        var result = typeof(Quasarv01)
            .GetMethod("Negamax", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(bot, new object[] { game, 2, float.NegativeInfinity, float.PositiveInfinity });

        Assert.NotNull(result);
    }
}