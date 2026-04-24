using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChessDotNet;


namespace ChessAIApp
{
  public static class Utils
  {
    //Get the algebraic notation of a given move like - e4, Bxc4, Qb2#
    public static string AlgebraicNotation(ChessGame game, Move move, string[,] board)
    {
      string piece = GetPiece(new Point(8 - move.OriginalPosition.Rank, (int)move.OriginalPosition.File), board);
      string result = piece.ToUpper()[1] + "";
      string moveString = move.ToString().ToLower();

      ChessGame clone = new ChessGame(game.GetFen());
      var moveResult = clone.MakeMove(move, true);

      //Handle Castling
      if (moveResult.HasFlag(MoveType.Castling))
      {
        if (moveString[3] == 'g')
          return "O-O";
        else
          return "O-O-O";
      }

      //Handle Captures
      if (result.ToLower() == "p")
      {
        result = "";
        if (moveResult.HasFlag(MoveType.Capture))
          result += moveString[0];
      }
      if (moveResult.HasFlag(MoveType.Capture))
        result += 'x';
      result += moveString.Substring(3);

      //Handle Checkmates
      if (clone.IsCheckmated(clone.WhoseTurn))
        result += '#';
      else if (clone.IsInCheck(clone.WhoseTurn))
        result += '+';

      //Handle Promotions
      if (move.Promotion != null)
        result += "=" + move.Promotion;

      return result.Replace(" ", "");
    }

    //Get piece occupying a given square on a given board
    public static string GetPiece(Point square, string[,] board)
    {
      return board[(int)square.X, (int)square.Y];
    }

    //Check if move is pseoudo legal
    public static bool IsPseoudoLegal(Move move, string[,] board, ChessGame game, bool kingMoved)
    {
      
      //Preprocess the move
      if (move.OriginalPosition == move.NewPosition)
        return false;
      int fromRow = 8 - move.OriginalPosition.Rank;
      int fromCol = (int)move.OriginalPosition.File;

      int toRow = 8 - move.NewPosition.Rank;
      int toCol = (int)move.NewPosition.File;

      string piece = Utils.GetPiece(new Point(fromRow, fromCol), board);

      if (string.IsNullOrEmpty(piece))
        return false;

      char color = piece[0];
      char pieceType = Char.ToLower(piece[1]);


      switch (pieceType)
      {
        //Handle Pawn
        case 'p':
          if (color == 'w')
            return (toRow - fromRow == -1 && Math.Abs(fromCol - toCol) <= 1) || (toRow - fromRow == -2 && fromCol - toCol == 0 && toRow == 4);
          else
            return (toRow - fromRow == 1 && Math.Abs(fromCol - toCol) <= 1) || (toRow - fromRow == 2 && fromCol - toCol == 0 && toRow == 5);
        //Handle Knight
        case 'n':
          return Math.Abs(fromRow - toRow) + Math.Abs(fromCol - toCol) == 3 && fromRow != toRow && fromCol != toCol;
        //Handle Bishop
        case 'b':
          return Math.Abs(fromRow - toRow) == Math.Abs(fromCol - toCol);
        //Handle Rook
        case 'r':
          return fromRow == toRow || fromCol == toCol;
        //Handle Queen
        case 'q':
          return fromRow == toRow || fromCol == toCol || Math.Abs(fromRow - toRow) == Math.Abs(fromCol - toCol);
        //Handle King + Castling
        case 'k':
          {
            bool regularKingMove = Math.Abs(fromRow - toRow) <= 1 && Math.Abs(fromCol - toCol) <= 1;

            if (regularKingMove)
              return true;

            bool kingSideCastle, queenSideCastle;
            if (move.Player != Player.White)
            {
              kingSideCastle = game.CanWhiteCastleKingSide;
              queenSideCastle = game.CanWhiteCastleQueenSide;
            }
            else
            {
              kingSideCastle = game.CanBlackCastleKingSide;
              queenSideCastle = game.CanBlackCastleQueenSide;
            }
            return ((((fromCol - toCol) == 2 && kingSideCastle) || ((fromCol - toCol) == -2 && queenSideCastle)) && !kingMoved);
          }
        default:
          // Error Piece
          break;
      }

      return false;
    }

    //Function to convert 2 squares to a move
    public static Move SquaresToMove(Point fromSquare, Point toSquare, ChessGame game)
    {
      string moveFrom = ToSquareName(fromSquare);
      string moveTo = ToSquareName(toSquare);

      Move move = new Move(moveFrom, moveTo, game.WhoseTurn, null);
      return move;
    }

    //Function to convert a square to a name - a2, b3, f8
    public static string ToSquareName(Point square)
    {
      char file = (char)('a' + square.Y);  // 0 -> a, 1 -> b,
      char rank = (char)('8' - square.X);  // 0 (top) -> 8, 7 (bottom) -> 1
      return $"{file}{rank}";
    }

    //Function to convert a move to its start & end squares
    public static (Point, Point) MoveToSquares(Move? move)
    {
      if (move == null)
        return (new Point(-1, -1), new Point(-1, -1));
      int fromRow = 8 - move.OriginalPosition.Rank;
      int fromCol = (int)move.OriginalPosition.File;

      int toRow = 8 - move.NewPosition.Rank;
      int toCol = (int)move.NewPosition.File;

      return (new Point(fromRow, fromCol), new Point(toRow, toCol));
    }

    //Function to return player that made the move
    public static Player MoveToPlayer(Move? move, string[,] board)
    {
      (Point fromSquare, Point _) = Utils.MoveToSquares(move);
      string piece = GetPiece(fromSquare, board);
      if (piece[0] == 'w') return Player.White;
      else return Player.Black;
    }

    //Function to convert fen to a 8x8 string board
    public static string[,] FenToBoard(string? fenNotation)
    {
      if (fenNotation == null)
        return new string[8, 8]
        {
          { "bR","bN","bB","bQ","bK","bB","bN","bR" },
          { "bP","bP","bP","bP","bP","bP","bP","bP" },
          { "","","","","","","","" },
          { "","","","","","","","" },
          { "","","","","","","","" },
          { "","","","","","","","" },
          { "wP","wP","wP","wP","wP","wP","wP","wP" },
          { "wR","wN","wB","wQ","wK","wB","wN","wR" },
        };
      else
      {
        string fen = fenNotation.Split()[0];

        string[,] board = new string[8, 8];

        string[] rows = fen.Split('/');

        for (int row = 0; row < 8; row++)
        {
          int col = 0;

          foreach (char c in rows[row])
          {
            if (char.IsDigit(c))
            {
              int empty = c - '0';
              for (int i = 0; i < empty; i++)
              {
                board[row, col] = "";
                col++;
              }
            }
            else
            {
              string color = char.IsUpper(c) ? "w" : "b";
              char piece = char.ToUpper(c);

              board[row, col] = color + piece;
              col++;
            }
          }
        }

        return board;

      }
    }
  }
}
