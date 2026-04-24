using Xunit;
using System.Windows;
using ChessDotNet;
using ChessAIApp;

public class UtilsTests
{
    [Fact]
    public void FenToBoard_Null_ReturnsDefaultBoard()
    {
        var board = Utils.FenToBoard(null);

        Assert.Equal("bR", board[0, 0]);
        Assert.Equal("bK", board[0, 4]);
        Assert.Equal("wP", board[6, 0]);
        Assert.Equal("wR", board[7, 0]);
    }

    [Fact]
    public void FenToBoard_CustomPosition_ParsesCorrectly()
    {
        string fen = "8/8/8/3k4/8/8/4P3/4K3";

        var board = Utils.FenToBoard(fen);

        Assert.Equal("bK", board[3, 3]);
        Assert.Equal("wP", board[6, 4]);
        Assert.Equal("wK", board[7, 4]);
    }

    // ---------------------------
    // SQUARE CONVERSION
    // ---------------------------

    [Theory]
    [InlineData(7, 0, "a1")]
    [InlineData(7, 7, "h1")]
    [InlineData(0, 0, "a8")]
    [InlineData(6, 4, "e2")]
    public void ToSquareName_ConvertsCorrectly(int row, int col, string expected)
    {
        var result = Utils.ToSquareName(new Point(row, col));
        Assert.Equal(expected, result);
    }

    // ---------------------------
    // MOVE CONVERSION
    // ---------------------------

    [Fact]
    public void SquaresToMove_CreatesCorrectMove()
    {
        var game = new ChessGame();

        var move = Utils.SquaresToMove(new Point(6, 4), new Point(4, 4), game);

        Assert.Equal("e2", move.OriginalPosition.ToString().ToLower());
        Assert.Equal("e4", move.NewPosition.ToString().ToLower());
    }

    [Fact]
    public void MoveToSquares_ConvertsCorrectly()
    {
        var move = new Move("e2", "e4", Player.White);

        var (from, to) = Utils.MoveToSquares(move);

        Assert.Equal(new Point(6, 4), from);
        Assert.Equal(new Point(4, 4), to);
    }

    // ---------------------------
    // GET PIECE
    // ---------------------------

    [Fact]
    public void GetPiece_ReturnsCorrectPiece()
    {
        var board = Utils.FenToBoard(null);

        var piece = Utils.GetPiece(new Point(6, 0), board);

        Assert.Equal("wP", piece);
    }

    // ---------------------------
    // MOVE TO PLAYER
    // ---------------------------

    [Fact]
    public void MoveToPlayer_WhitePiece_ReturnsWhite()
    {
        var board = Utils.FenToBoard(null);
        var move = new Move("e2", "e4", Player.White);

        var player = Utils.MoveToPlayer(move, board);

        Assert.Equal(Player.White, player);
    }

    // ---------------------------
    // PSEUDO LEGAL MOVES
    // ---------------------------

    [Fact]
    public void PawnMove_ForwardTwo_IsPseudoLegal()
    {
        var board = Utils.FenToBoard(null);
        var game = new ChessGame();

        var move = new Move("e2", "e4", Player.White);

        Assert.True(Utils.IsPseoudoLegal(move, board, game, false));
    }

    [Fact]
    public void KnightMove_LShape_IsPseudoLegal()
    {
        var board = Utils.FenToBoard(null);
        var game = new ChessGame();

        var move = new Move("g1", "f3", Player.White);

        Assert.True(Utils.IsPseoudoLegal(move, board, game, false));
    }

    [Fact]
    public void BishopMove_Diagonal_IsPseudoLegal()
    {
        string fen = "8/8/8/8/3B4/8/8/8";
        var board = Utils.FenToBoard(fen);
        var game = new ChessGame();

        var move = new Move("d4", "g7", Player.White);

        Assert.True(Utils.IsPseoudoLegal(move, board, game, false));
    }

    [Fact]
    public void RookMove_Straight_IsPseudoLegal()
    {
        string fen = "8/8/8/8/3R4/8/8/8";
        var board = Utils.FenToBoard(fen);
        var game = new ChessGame();

        var move = new Move("d4", "d8", Player.White);

        Assert.True(Utils.IsPseoudoLegal(move, board, game, false));
    }

    [Fact]
    public void QueenMove_Diagonal_IsPseudoLegal()
    {
        string fen = "8/8/8/8/3Q4/8/8/8";
        var board = Utils.FenToBoard(fen);
        var game = new ChessGame();

        var move = new Move("d4", "h8", Player.White);

        Assert.True(Utils.IsPseoudoLegal(move, board, game, false));
    }

    [Fact]
    public void KingMove_OneSquare_IsPseudoLegal()
    {
        string fen = "8/8/8/8/3K4/8/8/8";
        var board = Utils.FenToBoard(fen);
        var game = new ChessGame();

        var move = new Move("d4", "d5", Player.White);

        Assert.True(Utils.IsPseoudoLegal(move, board, game, false));
    }

    [Fact]
    public void InvalidMove_SameSquare_ReturnsFalse()
    {
        var board = Utils.FenToBoard(null);
        var game = new ChessGame();

        var move = new Move("e2", "e2", Player.White);

        Assert.False(Utils.IsPseoudoLegal(move, board, game, false));
    }

    // ---------------------------
    // ALGEBRAIC NOTATION
    // ---------------------------

    [Fact]
    public void AlgebraicNotation_PawnMove()
    {
        var game = new ChessGame();
        var board = Utils.FenToBoard(null);

        var move = new Move("e2", "e4", Player.White);

        var result = Utils.AlgebraicNotation(game, move, board);

        Assert.Equal("e4", result);
    }

    [Fact]
    public void AlgebraicNotation_KnightMove()
    {
        var game = new ChessGame();
        var board = Utils.FenToBoard(null);

        var move = new Move("g1", "f3", Player.White);

        var result = Utils.AlgebraicNotation(game, move, board);

        Assert.Equal("Nf3", result);
    }

    [Fact]
    public void AlgebraicNotation_Capture()
    {
        string fen = "8/8/8/4p3/4P3/8/8/8";
        var board = Utils.FenToBoard(fen);
        var game = new ChessGame(fen + " w - - 0 1");

        var move = new Move("e4", "e5", Player.White);

        var result = Utils.AlgebraicNotation(game, move, board);

        Assert.Contains("x", result);
    }

    [Fact]
    public void AlgebraicNotation_Castling_KingSide()
    {
        var game = new ChessGame();

        var move = new Move("e1", "g1", Player.White);

        var result = Utils.AlgebraicNotation(game, move, Utils.FenToBoard(null));

        Assert.Equal("O-O", result);
    }

    [Fact]
    public void AlgebraicNotation_Promotion()
    {
        string fen = "8/P7/8/8/8/8/8/8";
        var board = Utils.FenToBoard(fen);
        var game = new ChessGame(fen + " w - - 0 1");

        var move = new Move("a7", "a8", Player.White, 'Q');

        var result = Utils.AlgebraicNotation(game, move, board);

        Assert.Contains("=Q", result);
    }
}