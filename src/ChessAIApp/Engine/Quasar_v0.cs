using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChessDotNet;

namespace ChessAIApp
{
  //Quasar chess Bot v0
  //Evaluates positions only based on material gain
  //Depth 3
  public class Quasarv0 : RandomBot
  {
    public Quasarv0(Board board) : base(board){}

    //Rng and Piece Values
    private static Random rng = new Random();
    private static readonly Dictionary<char, int> PieceValue = new()
    {
        { 'P', 1 },
        { 'N', 3 },
        { 'B', 3 },
        { 'R', 5 },
        { 'Q', 9 },
        { 'K', 0 }
    };

    //Function to request a move from the bot
    public override async Task MakeMove()
    {
      if (board != null)
      {
        Move bestMove = await Task.Run(() => Evaluate(game, 3));
        board.MakeMove(bestMove);
      }
    }

    //Evaluate function
    public Move Evaluate(ChessGame game, int depth)
    {
        ChessGame currentGame = new ChessGame(game.GetFen());
        List<Move> legalMoves = GetAllLegalMoves(currentGame);

        //Score starts at -Infinity
        float bestScore = float.NegativeInfinity;
        Move bestMove = legalMoves[0];

        foreach (Move move in legalMoves)
        {
            //Foreach move score is calculated and the best move is stored

            float score = EvaluatePositionAfterMove(currentGame, move, depth);

            if (score > bestScore)
            {
              bestScore = score;
              bestMove = move;
            }
        }
        
        //Return the best move at the end
        return bestMove;
    }

    //Evaluate Position 
    private float EvaluatePositionAfterMove(ChessGame game, Move move, int depth)
    {
        //Make a clone and make the proposed move there
        ChessGame clone = new ChessGame(game.GetFen());

        //Calcluate material gain
        float materialGain = EvaluateMove(game, move);

        clone.MakeMove(move, true);

        //If last depth return the material gain
        if (depth <= 1)
            return materialGain;

        //If checkmate return very high score
        if (clone.IsCheckmated(clone.WhoseTurn))
            return 9999;

        //If check add 0.5 to score
        if (clone.IsInCheck(clone.WhoseTurn))
            materialGain+=0.5f;

        //If stalemate score is 0
        if (clone.IsStalemated(clone.WhoseTurn))
            return 0;

        // Opponent moves next, calculate to minimize their best reply
        List<Move> replies = GetAllLegalMoves(clone);
        if (replies.Count == 0)
            return materialGain;

        //Find opponents best move
        float worstReply = float.NegativeInfinity;

        foreach (Move reply in replies)
        {
            float replyScore = EvaluatePositionAfterMove(clone, reply, depth - 1);

            if (replyScore > worstReply)
                worstReply = replyScore;
        }
        // Return negated value
        return materialGain - worstReply + (rng.Next(5) / 10);
    }

    //Evaluate material gain of the move
    public static float EvaluateMove(ChessGame game, Move move)
    {
        //Find and return the captured piece value
        var board = game.GetBoard();

        int row = 8 - move.NewPosition.Rank;
        int col = (int)move.NewPosition.File;

        var piece = board[row][col];

        if (piece == null)
            return 0;

        char fen = char.ToUpper(piece.GetFenCharacter());

        return PieceValue[fen];
    }
  }
}