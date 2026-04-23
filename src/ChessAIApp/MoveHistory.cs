using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using ChessDotNet;
using System.Diagnostics.CodeAnalysis;

namespace ChessAIApp
{
  public class MoveHistory
  {
    private static readonly ObservableCollection<MoveEntry> _moveList = new();

    public static ReadOnlyObservableCollection<MoveEntry> MoveList { get; }
        = new(_moveList);
    private static int currentSelectedRowIndex = 0, currentSelectedColumnIndex = 0;

    public const string startingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public static void Add(MoveEntry move)
    {
      _moveList.Add(move);
    }
    public static void Add(Player player, string notation, string positionFen, Point fromSquare, Point toSquare)
    {
      var moveSquares = new SquareMove { Start = new SerializablePoint(fromSquare), End = new SerializablePoint(toSquare) };

      if (player == Player.White)
      {
        _moveList.Add(new MoveEntry
        {
          Number = _moveList.Count + 1,
          White = notation,
          WhiteFen = positionFen,
          WhiteSquares = moveSquares
        });
      }
      else
      {
        if (_moveList.Count == 0)
          return;

        MoveEntry last = _moveList[_moveList.Count - 1];

        last.Black = notation;
        last.BlackFen = positionFen;
        last.BlackSquares = moveSquares;

        _moveList.RemoveAt(_moveList.Count - 1);
        _moveList.Add(last);
      }

      SelectLast();
    }

    public static void RemoveLast()
    {
      if (_moveList.Count == 0)
        return;

      MoveEntry last = _moveList[^1];
      _moveList.RemoveAt(_moveList.Count - 1);

      if (last.BlackFen != null && last.WhiteFen != null)
      {
        Add(Player.White, last.White!, last.WhiteFen, last.WhiteSquares.Start.ToPoint(), last.WhiteSquares.End.ToPoint());
      }
    }

    public static void RemoveAt(int id)
    {
      if (id >= 0 && id < _moveList.Count)
      {
        if (_moveList[id].BlackFen != null)
        {
          MoveEntry modifiedMove = _moveList[id];
          modifiedMove.BlackFen = null;
          modifiedMove.Black = "";
          modifiedMove.BlackSquares = new SquareMove
          {
            Start = new SerializablePoint(),
            End = new SerializablePoint()
          };

          _moveList[id] = modifiedMove;
        }
        else
        {
          _moveList.RemoveAt(id);
        }

        SelectLast();
      }
    }

    public static void Clear() => _moveList.Clear();

    public static ObservableCollection<MoveEntry> GetMoves()
    {
      return _moveList;
    }
    public static MoveEntry? GetMoveEntry(int id)
    {
      return (id >= 0 && id < _moveList.Count) ? _moveList[id] : null;
    }

    public static int GetLength() => _moveList.Count;

    public static MoveEntry? GetSelected() => GetMoveEntry(currentSelectedRowIndex);

    public static string GetSelectedFen()
    {
      if (_moveList.Count == 0) return "";

      MoveEntry selectedMove = _moveList[currentSelectedRowIndex];

      if (currentSelectedColumnIndex == 0 && selectedMove.WhiteFen != null)
        return selectedMove.WhiteFen;

      if (selectedMove.BlackFen != null)
        return selectedMove.BlackFen;

      return "";
    }

    public static (Point start, Point end) GetSelectedHighlights()
    {
      if (_moveList.Count == 0)
        return (new Point(0, 0), new Point(0, 0));

      MoveEntry selectedMove = _moveList[currentSelectedRowIndex];

      if (currentSelectedColumnIndex == 0 && selectedMove.WhiteFen != null)
        return (selectedMove.WhiteSquares.Start.ToPoint(), selectedMove.WhiteSquares.End.ToPoint());

      if (selectedMove.BlackFen != null)
        return (selectedMove.BlackSquares.Start.ToPoint(), selectedMove.BlackSquares.End.ToPoint());

      return (new Point(0, 0), new Point(0, 0));
    }

    public static void Select(int row, int col)
    {
      currentSelectedRowIndex = row;
      currentSelectedColumnIndex = col;
    }

    public static void SelectPrevious()
    {
      if (currentSelectedColumnIndex == 1)
      {
        currentSelectedColumnIndex = 0;
      }
      else if (currentSelectedRowIndex > 0)
      {
        currentSelectedRowIndex--;
        currentSelectedColumnIndex = 1;
      }

      currentSelectedRowIndex = Math.Clamp(currentSelectedRowIndex, 0, GetLength() - 1);
    }

    public static void SelectFirst()
    {
      currentSelectedRowIndex = 0;
      currentSelectedColumnIndex = 0;
    }

    public static void SelectLast()
    {
      if (_moveList.Count == 0) return;

      currentSelectedRowIndex = GetLength() - 1;
      currentSelectedColumnIndex =
          _moveList[currentSelectedRowIndex].BlackFen != null ? 1 : 0;
    }

    public static void SelectNext()
    {
      if (currentSelectedColumnIndex == 0)
      {
        currentSelectedColumnIndex = 1;
      }
      else if (currentSelectedRowIndex < _moveList.Count - 1)
      {
        currentSelectedRowIndex++;
        currentSelectedColumnIndex = 0;
      }

      currentSelectedRowIndex = Math.Clamp(currentSelectedRowIndex, 0, GetLength() - 1);
    }

    public static void SaveToJson(string filePath, string? botName, string? difficulty, string? color, bool isFinished = false, string? result = null)
    {
      var options = new JsonSerializerOptions { WriteIndented = true };

      var savedGame = new SavedGame
      {
        IsFinished = isFinished,
        Result = result,
        BotName = botName,
        Difficulty = difficulty,
        PlayerColor = color,
        Moves = _moveList.ToList()
      };

      System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(savedGame, options));
    }

    public static SavedGame? LoadGame(string filePath)
    {
      if (!System.IO.File.Exists(filePath))
        return null;

      return JsonSerializer.Deserialize<SavedGame>(System.IO.File.ReadAllText(filePath));
    }

    public static string? GetLatestUnfinishedGame(string folder)
    {
      if (!Directory.Exists(folder))
        return null;

      var files = Directory.GetFiles(folder, "*.json")
                           .OrderByDescending(f => f);

      foreach (var file in files)
      {
        string json = System.IO.File.ReadAllText(file);
        var savedGame = JsonSerializer.Deserialize<SavedGame>(json);

        if (savedGame != null && !savedGame.IsFinished)
          return file;
      }

      return null;
    }

    public class SavedGame
    {
      public bool IsFinished { get; set; }
      public string? Result { get; set; }
      public string? BotName { get; set; }
      public string? Difficulty { get; set; }
      public string? PlayerColor { get; set; }

      public List<MoveEntry> Moves { get; set; } = new();
    }

    public class MoveEntry
    {
      public int Number { get; set; }
      public string? White { get; set; }
      public string? Black { get; set; }
      public string? WhiteFen { get; set; }
      public string? BlackFen { get; set; }

      public SquareMove WhiteSquares { get; set; } = new();
      public SquareMove BlackSquares { get; set; } = new();
    }

    public class SquareMove
    {
      [SetsRequiredMembers]
      public SquareMove()
      {
        Start = new SerializablePoint();
        End = new SerializablePoint();
      }
      public required SerializablePoint Start { get; set; }
      public required SerializablePoint End { get; set; }
    }

    public class SerializablePoint
    {
      public double X { get; set; }
      public double Y { get; set; }
      public SerializablePoint()
      {
        X = 0;
        Y = 0;
      }

      public SerializablePoint(System.Windows.Point p)
      {
        X = p.X;
        Y = p.Y;
      }

      public Point ToPoint()
      {
        return new System.Windows.Point(X, Y);
      }
    }
  }
}