using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessDotNet;

namespace ChessAIApp
{
  public class RandomBot : Bot
  {
    public ChessGame game { get; set; }
    public RandomBot(Board board)
    {
      this.board = board;
      game = board.GetGame();
    }
    private static Random rng = new Random();

    public override async Task MakeMove()
    {
      if (board != null)
      {
        List<Move> legalMoves = GetAllLegalMoves(game);
        await Task.Delay(10);
        MakeRandomMove(legalMoves);
      }
    }

    public static List<Move> GetAllLegalMoves(ChessGame game)
    {
      return game.GetValidMoves(game.WhoseTurn).ToList();
    }
    public void MakeRandomMove(List<Move> legalMoves)
    {
      Move move = legalMoves[rng.Next(legalMoves.Count)];
      board?.MakeMove(move);
    }

    public override void Reset(){
      if(board != null)
        game = board.GetGame();
    }
    public override void SetColor (Player? color) {}
  }
}