using Xunit;
using System;
using System.Linq;
using System.IO;
using System.Windows;
using ChessDotNet;
using ChessAIApp;

public class MoveHistoryTests
{
    private static void Reset()
    {
        MoveHistory.Clear();
        MoveHistory.SelectFirst();
    }

    [Fact]
    public void Add_WhiteMove_ShouldCreateNewEntry()
    {
        Reset();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(6, 4), new Point(4, 4));

        Assert.Equal(1, MoveHistory.GetLength());
        var move = MoveHistory.GetMoveEntry(0);

        Assert.Equal("e4", move.White);
        Assert.Null(move.Black);
    }

    [Fact]
    public void Add_BlackMove_ShouldAttachToLastEntry()
    {
        Reset();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(), new Point());
        MoveHistory.Add(Player.Black, "e5", "fen2", new Point(), new Point());

        Assert.Equal(1, MoveHistory.GetLength());
        var move = MoveHistory.GetMoveEntry(0);

        Assert.Equal("e4", move.White);
        Assert.Equal("e5", move.Black);
    }

    [Fact]
    public void Add_BlackWithoutWhite_ShouldDoNothing()
    {
        Reset();

        MoveHistory.Add(Player.Black, "e5", "fen", new Point(), new Point());

        Assert.Equal(0, MoveHistory.GetLength());
    }

    // ---------------------------
    // REMOVE
    // ---------------------------

    [Fact]
    public void RemoveLast_WithFullMove_ShouldKeepWhite()
    {
        Reset();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(), new Point());
        MoveHistory.Add(Player.Black, "e5", "fen2", new Point(), new Point());

        MoveHistory.RemoveLast();

        Assert.Equal(1, MoveHistory.GetLength());
        var move = MoveHistory.GetMoveEntry(0);

        Assert.Equal("e4", move.White);
        Assert.Null(move.Black);
    }

    [Fact]
    public void RemoveAt_WithBlackMove_ShouldClearBlackOnly()
    {
        Reset();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(), new Point());
        MoveHistory.Add(Player.Black, "e5", "fen2", new Point(), new Point());

        MoveHistory.RemoveAt(0);

        var move = MoveHistory.GetMoveEntry(0);

        Assert.Equal("e4", move.White);
        Assert.True(string.IsNullOrEmpty(move.Black));
    }

    [Fact]
    public void RemoveAt_WhiteOnly_ShouldRemoveEntry()
    {
        Reset();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(), new Point());

        MoveHistory.RemoveAt(0);

        Assert.Equal(0, MoveHistory.GetLength());
    }

    // ---------------------------
    // SELECTION
    // ---------------------------

    [Fact]
    public void SelectLast_ShouldSelectLastMove()
    {
        Reset();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(), new Point());
        MoveHistory.SelectLast();

        var selected = MoveHistory.GetSelected();

        Assert.Equal("e4", selected.White);
    }

    [Fact]
    public void SelectNext_ShouldMoveForward()
    {
        Reset();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(), new Point());
        MoveHistory.Add(Player.Black, "e5", "fen2", new Point(), new Point());

        MoveHistory.SelectFirst();
        MoveHistory.SelectNext();

        string fen = MoveHistory.GetSelectedFen();

        Assert.Equal("fen2", fen);
    }

    [Fact]
    public void SelectPrevious_ShouldMoveBack()
    {
        Reset();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(), new Point());
        MoveHistory.Add(Player.Black, "e5", "fen2", new Point(), new Point());

        MoveHistory.SelectLast();
        MoveHistory.SelectPrevious();

        string fen = MoveHistory.GetSelectedFen();

        Assert.Equal("fen1", fen);
    }

    // ---------------------------
    // GETTERS
    // ---------------------------

    [Fact]
    public void GetSelectedFen_ShouldReturnCorrectFen()
    {
        Reset();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(), new Point());

        var fen = MoveHistory.GetSelectedFen();

        Assert.Equal("fen1", fen);
    }

    [Fact]
    public void GetSelectedHighlights_ShouldReturnCorrectSquares()
    {
        Reset();

        var from = new Point(6, 4);
        var to = new Point(4, 4);

        MoveHistory.Add(Player.White, "e4", "fen1", from, to);

        var (start, end) = MoveHistory.GetSelectedHighlights();

        Assert.Equal(from, start);
        Assert.Equal(to, end);
    }

    // ---------------------------
    // CLEAR
    // ---------------------------

    [Fact]
    public void Clear_ShouldRemoveAllMoves()
    {
        Reset();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(), new Point());

        MoveHistory.Clear();

        Assert.Equal(0, MoveHistory.GetLength());
    }

    // ---------------------------
    // JSON SAVE / LOAD
    // ---------------------------

    [Fact]
    public void SaveAndLoadGame_ShouldPersistData()
    {
        Reset();

        string file = Path.GetTempFileName();

        MoveHistory.Add(Player.White, "e4", "fen1", new Point(), new Point());

        MoveHistory.SaveToJson(file, "Bot", "Hard", "White");

        var loaded = MoveHistory.LoadGame(file);

        Assert.NotNull(loaded);
        Assert.Single(loaded.Moves);
        Assert.Equal("Bot", loaded.BotName);

        System.IO.File.Delete(file);
    }

    // ---------------------------
    // LATEST UNFINISHED GAME
    // ---------------------------

    [Fact]
    public void GetLatestUnfinishedGame_ShouldReturnCorrectFile()
    {
        Reset();

        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        string file1 = Path.Combine(folder, "game1.json");
        string file2 = Path.Combine(folder, "game2.json");

        // Finished game
        System.IO.File.WriteAllText(file1, System.Text.Json.JsonSerializer.Serialize(new MoveHistory.SavedGame
        {
            IsFinished = true
        }));

        // Unfinished game
        System.IO.File.WriteAllText(file2, System.Text.Json.JsonSerializer.Serialize(new MoveHistory.SavedGame
        {
            IsFinished = false
        }));

        var result = MoveHistory.GetLatestUnfinishedGame(folder);

        Assert.Equal(file2, result);

        Directory.Delete(folder, true);
    }
}