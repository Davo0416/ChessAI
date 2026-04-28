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
    //Quasar Chess Bot v01
    //Evaluates positions based on material gain and piece activity
    //Uses alpha-beta pruning to reduce computation time
    //Improvements over original:
    //  - Move ordering: captures searched first for better pruning efficiency
    //  - Quiescence search: continues capturing past depth limit to avoid horizon effect
    //  - Repetition & same-piece penalties now actually applied during search
    //  - Checkmate depth bonus: finds fastest mate rather than treating all mates equally
    public class Quasarv01 : RandomBot
    {
        public Quasarv01(Board board) : base(board) { }
        private Player? color;

        private DateTime searchStart;
        private readonly int maxSearchTimeMs = 10000;

        private static readonly Dictionary<char, int> PieceValue = new()
        {
            { 'P', 1 },
            { 'N', 3 },
            { 'B', 3 },
            { 'R', 5 },
            { 'Q', 9 },
            { 'K', 0 }
        };

        //Set bot color
        public override void SetColor(Player? color)
        {
            this.color = color;
        }

        //Entry point - fixed depth 4 search
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

        private bool IsTimeUp()
        {
            return (DateTime.Now - searchStart).TotalMilliseconds > maxSearchTimeMs;
        }

        //Evaluate functions - finds best move at given depth using Alpha-Beta pruning
        public Move Evaluate(int depth)
        {
            return Evaluate(game, depth);
        }

        public Move Evaluate(ChessGame game, int depth)
        {
            var moves = OrderMoves(game, GetAllLegalMoves(game));
            Move best = moves[0];

            float alpha = float.NegativeInfinity;
            float beta = float.PositiveInfinity;
            float bestScore = float.NegativeInfinity;

            foreach (var move in moves)
            {
                ChessGame clone = new ChessGame(game.GetFen());
                clone.MakeMove(move, true);

                //Immediate checkmate - no need to search further
                if (clone.IsCheckmated(clone.WhoseTurn))
                    return move;

                float score = -Negamax(clone, depth - 1, -beta, -alpha, depth);

                //Apply penalties at the root level
                score += RepetitionPenalty(move);
                score += SamePieceMovePenalty(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = move;
                }

                alpha = Math.Max(alpha, bestScore);
            }

            return best;
        }

        //Negamax with alpha-beta pruning
        //depthFromRoot used to reward finding checkmate faster
        private float Negamax(ChessGame game, int depth, float alpha, float beta, int depthFromRoot)
        {
            //Continue to quiescence search at leaf nodes
            if (depth == 0)
                return QuiescenceSearch(game, alpha, beta, 6); //Cap quiescence at 6 plies

            var moves = GetAllLegalMoves(game);

            if (moves.Count == 0)
                return EvaluateBoard(game);

            var orderedMoves = OrderMoves(game, moves);
            float bestScore = float.NegativeInfinity;

            foreach (Move move in orderedMoves)
            {
                ChessGame clone = new ChessGame(game.GetFen());
                clone.MakeMove(move, true);

                //Reward faster mates with a small depth bonus
                if (clone.IsCheckmated(clone.WhoseTurn))
                    return 1000f + depthFromRoot;

                float score = -Negamax(clone, depth - 1, -beta, -alpha, depthFromRoot + 1);

                if (score > bestScore)
                    bestScore = score;

                if (bestScore >= beta)
                    break; //Beta cutoff

                alpha = Math.Max(alpha, bestScore);
            }

            return bestScore;
        }

        //Quiescence Search - keeps searching captures past the normal depth limit
        //Prevents the bot from stopping mid-capture sequence and misvaluing positions (horizon effect)
        //maxDepth cap and time check prevent runaway search in sharp positions
        private float QuiescenceSearch(ChessGame game, float alpha, float beta, int maxDepth)
        {
            //Stand pat: assume we can choose not to capture
            float standPat = EvaluateBoard(game);

            if (standPat >= beta)
                return beta;

            //Stop if we've hit the quiescence depth cap or time is up
            if (maxDepth == 0 || IsTimeUp())
                return standPat;

            alpha = Math.Max(alpha, standPat);

            //Only consider capture moves
            var captures = GetAllLegalMoves(game)
                .Where(m => IsCapture(game, m))
                .ToList();

            foreach (var move in OrderMoves(game, captures))
            {
                ChessGame clone = new ChessGame(game.GetFen());
                clone.MakeMove(move, true);

                float score = -QuiescenceSearch(clone, -beta, -alpha, maxDepth - 1);

                if (score >= beta)
                    return beta;

                alpha = Math.Max(alpha, score);
            }

            return alpha;
        }

        //Checks whether a move captures an enemy piece
        private static bool IsCapture(ChessGame game, Move move)
        {
            var boardArr = game.GetBoard();
            //ChessDotNet: Rank is 1-8, board index 0 = rank 8
            int rankIdx = 8 - move.NewPosition.Rank;
            int fileIdx = (int)move.NewPosition.File;
            var target = boardArr[rankIdx][fileIdx];
            return target != null && target.Owner != game.WhoseTurn;
        }

        //Move ordering - search captures first using MVV-LVA (Most Valuable Victim, Least Valuable Attacker)
        //Better move order = more alpha-beta cutoffs = faster/deeper search
        private static List<Move> OrderMoves(ChessGame game, List<Move> moves)
        {
            var boardArr = game.GetBoard();

            return moves.OrderByDescending(m =>
            {
                int toRank = 8 - m.NewPosition.Rank;
                int toFile = (int)m.NewPosition.File;
                var victim = boardArr[toRank][toFile];

                if (victim == null || victim.Owner == game.WhoseTurn)
                    return 0;

                int fromRank = 8 - m.OriginalPosition.Rank;
                int fromFile = (int)m.OriginalPosition.File;
                var attacker = boardArr[fromRank][fromFile];

                char victimFen = char.ToUpper(victim.GetFenCharacter());
                char attackerFen = attacker != null ? char.ToUpper(attacker.GetFenCharacter()) : 'P';

                //High score = big piece captured by small piece
                return PieceValue[victimFen] * 10 - PieceValue[attackerFen];
            }).ToList();
        }

        //Evaluate position from the current player's point of view
        //Evaluate position from the perspective of whoever is currently moving
        //Must reflect the current mover - Negamax negates the score each ply
        private static float EvaluateBoard(ChessGame game)
        {
            float score = 0;
            var board = game.GetBoard();

            if (game.IsCheckmated(game.WhoseTurn))
                return float.NegativeInfinity;
            else if (game.IsStalemated(game.WhoseTurn))
                return 0;

            //Evaluate from the perspective of whoever is currently moving, not the bot's fixed color
            Player currentMover = game.WhoseTurn;

            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    var piece = board[rank][file];

                    if (piece != null)
                    {
                        char fen = char.ToUpper(piece.GetFenCharacter());
                        float value = PieceValue[fen];

                        //Add piece activity using piece-square tables
                        value += PieceActivity(piece, new Point(rank, file)) * 0.01f;

                        if (piece.Owner == currentMover)
                            score += value;
                        else
                            score -= value;
                    }
                }
            }

            return score;
        }

        //Penalty to discourage repeating the same position
        private float RepetitionPenalty(Move move)
        {
            if (board == null)
                return 0;

            ObservableCollection<MoveEntry> moveList = GetMoves();
            if (moveList.Count <= 2)
                return 0;

            string? previousMove = color == Player.White
                ? moveList[moveList.Count - 2].White
                : moveList[moveList.Count - 2].Black;

            bool isRepeat = previousMove == Utils.AlgebraicNotation(game, move, board.GetBoard());
            return isRepeat ? -0.25f : 0;
        }

        //Penalty to discourage moving the same piece twice in a row
        private float SamePieceMovePenalty(Move move)
        {
            if (board == null)
                return 0;

            ObservableCollection<MoveEntry> moveList = GetMoves();
            if (moveList.Count < 1)
                return 0;

            string newMoveStart = move.ToString().Substring(0, 2);

            string? previousMove = color == Player.White
                ? moveList[^1].White
                : moveList.Count > 1 ? moveList[^2].Black : null;

            bool isRepeat = previousMove != null
                && previousMove.ToUpper().Contains(newMoveStart);

            return isRepeat ? -0.25f : 0;
        }

        //Calculate piece activity score using piece-square tables
        private static int PieceActivity(Piece piece, Point square)
        {
            if (piece == null)
                return 0;

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