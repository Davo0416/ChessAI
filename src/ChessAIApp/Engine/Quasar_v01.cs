using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ChessDotNet;
using static ChessAIApp.MoveHistory;
using System.Linq;

namespace ChessAIApp
{
    //Quasar chess Bot v01 =======================================
    //Evaluates positions based on material gain and piece activity
    //Uses alpha-beta pruning to reduce computation time
    //Depth 4
    public class Quasarv01 : RandomBot
    {
        public Quasarv01(Board board) : base(board) { }
        private Player? color;

        private DateTime searchStart;
        private readonly int maxSearchTimeMs = 5000;

        private static readonly Dictionary<char, int> PieceValue = new()
        {
            { 'P', 1 },
            { 'N', 3 },
            { 'B', 3 },
            { 'R', 5 },
            { 'Q', 9 },
            { 'K', 0 }
        };

        public override void SetColor(Player? color)
        {
            this.color = color;
        }

        //Move Evaluation
        public override async Task MakeMove()
        {
            try
            {
                if (board == null)
                    return;

                searchStart = DateTime.Now;

                Move bestMove = await Task.Run(() => Evaluate(4));

                board.MakeMove(bestMove);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private Move IterativeDeepeningSearch()
        {
            Move bestMove = GetAllLegalMoves(game)[0];

            for (int depth = 1; depth <= 100; depth++)
            {
                if (IsTimeUp())
                    break;

                Move move = Evaluate(game, depth);
                bestMove = move;
            }

            return bestMove;
        }

        private bool IsTimeUp()
        {
            return (DateTime.Now - searchStart).TotalMilliseconds > maxSearchTimeMs;
        }

        public Move Evaluate(int depth)
        {
            return Evaluate(game, depth);
        }
        public Move Evaluate(ChessGame game, int depth)
        {
            var moves = GetAllLegalMoves(game);
            Move best = moves[0];

            float alpha = float.NegativeInfinity;
            float beta = float.PositiveInfinity;

            float bestScore = float.NegativeInfinity;

            foreach (var move in moves)
            {
                ChessGame clone = new ChessGame(game.GetFen());
                clone.MakeMove(move, true);

                if (clone.IsCheckmated(clone.WhoseTurn))
                    return move;

                float score = -Negamax(clone, depth - 1, -beta, -alpha);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = move;
                }

                alpha = Math.Max(alpha, bestScore);
            }

            return best;
        }

        private float Negamax(ChessGame game, int depth, float alpha, float beta)
        {
            if (depth == 0)
                return EvaluateBoard(game);

            var moves = GetAllLegalMoves(game);

            if (moves.Count == 0)
                return EvaluateBoard(game);

            float bestScore = float.NegativeInfinity;

            foreach (Move move in moves)
            {
                ChessGame clone = new ChessGame(game.GetFen());
                clone.MakeMove(move, true);

                float score = -Negamax(clone, depth - 1, -beta, -alpha);

                if (score > bestScore)
                    bestScore = score;

                if (bestScore >= beta)
                    break;

                alpha = Math.Max(alpha, bestScore);
            }

            return bestScore;
        }

        private float EvaluateBoard(ChessGame game)
        {
            float score = 0;
            var board = game.GetBoard();

            if (game.IsCheckmated(game.WhoseTurn))
                return float.NegativeInfinity;
            else if (game.IsStalemated(game.WhoseTurn))
                return 0;

            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    var piece = board[rank][file];
                    var value = 0;

                    if (piece != null)
                    {
                        char fen = char.ToUpper(piece.GetFenCharacter());
                        value += PieceValue[fen];

                        if (piece.Owner == color)
                            score += value;
                        else
                            score -= value;

                        // Add piece activity
                        score += PieceActivity(piece, new Point(rank, file)) * 0.01f;
                    }
                }
            }

            return score;
        }

        private float RepetitionPenalty(Move move)
        {
            if (board == null)
                return 0;
            ObservableCollection<MoveEntry> moveList = GetMoves();
            string? previousMove;
            if (moveList.Count <= 2)
                return 0;

            if (color == Player.White)
                previousMove = moveList[moveList.Count - 2].White;
            else
                previousMove = moveList[moveList.Count - 2].Black;

            bool isRepeat = previousMove == Utils.AlgebraicNotation(game, move, board.GetBoard());

            return isRepeat ? -0.25f : 0;
        }

        private float SamePieceMovePenalty(Move move)
        {
            if (board == null)
                return 0;
            ObservableCollection<MoveEntry> moveList = GetMoves();
            string? previousMove = "";
            if (moveList.Count < 1)
                return 0;

            string newMoveStart = move.ToString().Substring(0, 2);

            if (color == Player.White)
                previousMove = moveList[^1].White;
            else if (moveList.Count > 1)
                previousMove = moveList[^2].Black;

            bool isRepeat;
            if (previousMove != null)
            {
                previousMove = previousMove.ToUpper();
                isRepeat = previousMove.Contains(newMoveStart);
            }
            else isRepeat = false;

            return isRepeat ? -0.25f : 0;
        }
        private static int PieceActivity(Piece piece, Point square)
        {
            if (piece == null)
                return 0;

            // FEN character: white uppercase, black lowercase
            char fen = piece.GetFenCharacter();
            bool isWhite = char.IsUpper(fen);
            fen = char.ToUpper(fen);

            int x = (int)square.X;
            int y = (int)square.Y;

            if (!isWhite)
                x = 7 - x;

            switch (fen)
            {
                case 'P': return Bitmaps.PawnTable[x, y];
                case 'N': return Bitmaps.KnightTable[x, y];
                case 'B': return Bitmaps.BishopTable[x, y];
                case 'R': return Bitmaps.RookTable[x, y];
                case 'Q': return Bitmaps.QueenTable[x, y];
                case 'K': return Bitmaps.KingMiddleGameTable[x, y];
            }

            return 0;
        }

    }
}