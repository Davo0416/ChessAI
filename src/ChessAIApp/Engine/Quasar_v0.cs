using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChessDotNet;

namespace ChessAIApp
{
  //Quasar chess Bot v0 =======================================
  //Evaluates positions only based on material gain
  //Depth 3
  public class Quasarv0 : RandomBot
  {
    public Quasarv0(Board board) : base(board){}
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
    public override async Task MakeMove()
    {
      if (board != null)
      {
        Move bestMove = await Task.Run(() => Evaluate(game, 3));
        board.MakeMove(bestMove);
      }
    }

    public Move Evaluate(ChessGame game, int depth)
    {
        ChessGame currentGame = new ChessGame(game.GetFen());
        List<Move> legalMoves = GetAllLegalMoves(currentGame);

        float bestScore = float.NegativeInfinity;
        Move bestMove = legalMoves[0];

        foreach (Move move in legalMoves)
        {
            float score = EvaluatePositionAfterMove(currentGame, move, depth);

            if (score > bestScore)
            {
              bestScore = score;
              bestMove = move;
            }
        }
        
        return bestMove;
    }

    private float EvaluatePositionAfterMove(ChessGame game, Move move, int depth)
    {
        ChessGame clone = new ChessGame(game.GetFen());
        float materialGain = EvaluateMove(game, move);

        clone.MakeMove(move, true);

        if (depth <= 1)
            return materialGain;

        if (clone.IsCheckmated(clone.WhoseTurn))
            return 9999;

        if (clone.IsInCheck(clone.WhoseTurn))
            materialGain+=0.5f;

        if (clone.IsStalemated(clone.WhoseTurn))
            return 0;

        // Opponent moves next → minimize their best reply
        List<Move> replies = GetAllLegalMoves(clone);

        if (replies.Count == 0)
            return materialGain;

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

    public static float EvaluateMove(ChessGame game, Move move)
    {
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