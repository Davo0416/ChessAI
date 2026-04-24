using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessDotNet;

//RandomBot - plays random moves
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

    //Rng for the random move generation
    private static Random rng = new Random();

    //Functions to request a random move from the bot
    public override async Task MakeMove()
    {
      if (board != null)
      {
        List<Move> legalMoves = GetAllLegalMoves(game);
        await Task.Delay(10);
        MakeRandomMove(legalMoves);
      }
    }
    public void MakeRandomMove(List<Move> legalMoves)
    {
      Move move = legalMoves[rng.Next(legalMoves.Count)];
      board?.MakeMove(move);
    }

    public static List<Move> GetAllLegalMoves(ChessGame game)
    {
      return game.GetValidMoves(game.WhoseTurn).ToList();
    }
    
    //Reset bot for a new game
    public override void Reset(){
      if(board != null)
        game = board.GetGame();
    }

    //Set bots color
    public override void SetColor (Player? color) {}
  }
}