using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChessDotNet;

namespace ChessAIApp
{
  public class Board
  {
    private readonly Panel boardGrid, dragLayer, arrowLayer;
    private Player? currentView = Player.White;
    private string[,] board, premoveBoard;
    private string[,]? viewBoard;
    private const int BoardSize = 8;
    private readonly bool showCoords = true;
    private ChessGame game;
    private Image? _draggedPiece;
    private Point _mouseOffset;
    private bool _isDragging;
    private Point _originalPosition;
    private Panel? _originalParent;
    private Point? _originalParentPoint;
    private static Color darkColor => (Color)Application.Current.FindResource("DarkSquareColor");
    private static Color lightColor => (Color)Application.Current.FindResource("LightSquareColor");
    private static Color highlightColor => (Color)Application.Current.FindResource("EngineHighlightColor");
    private static Color userHighlightColor => (Color)Application.Current.FindResource("UserHighlightRed");
    private static Color userHighlightColorOrange => (Color)Application.Current.FindResource("UserHighlightOrange");
    private static Color userHighlightColorBlue => (Color)Application.Current.FindResource("UserHighlightBlue");
    private static Color userHighlightColorGreen => (Color)Application.Current.FindResource("UserHighlightGreen");
    private static Color arrowColor => (Color)Application.Current.FindResource("ArrowColor");
    private static Color hintArrowColor => (Color)Application.Current.FindResource("HintArrowColor");
    private Bot? botOne, botTwo;
    Player? RandomTurn, InvertedRandomTurn;
    private readonly Canvas promotionLayer;
    private Move? pendingPromotionMove = null;
    private string setName = "alpha";
    private Point? toHighlightSquare, fromHighlightSquare;
    private readonly Action<string, string> EndScreen;
    public string? currentGamePath, selectedBot, selectedColor, selectedDifficulty;
    private const string highlightTxt = "Highlight";
    private const string premoveTxt = "Premove";

    public bool Loaded { get; set; }
    public Board(
      Panel BoardGrid,
      Canvas arrowLayer,
      Canvas dragLayer,
      DataGrid movesTable,
      Canvas promotionLayer,
      Action<string, string> EndScreen
    )
    {
      board = new string[8, 8]
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
      premoveBoard = new string[8, 8]
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
      viewBoard = null;
      boardGrid = BoardGrid;
      this.dragLayer = dragLayer;
      this.arrowLayer = arrowLayer;
      this.promotionLayer = promotionLayer;
      this.EndScreen = EndScreen;
      game = new ChessGame();
    }

    // Board ------------------------------------------------------------------------------------
    public void BuildBoard()
    {
      boardGrid.Children.Clear();
      Overlays.ClearArrows(arrowLayer);

      for (int row = 0; row < BoardSize; row++)
      {
        for (int col = 0; col < BoardSize; col++)
        {
          int colNum = col, rowNum = row;
          if (currentView == Player.Black)
          {
            colNum = 7 - col;
            rowNum = 7 - row;
          }

          var square = new Grid
          {
            Background = ((rowNum + colNum) % 2 == 0)
                  ? new SolidColorBrush(lightColor)
                  : new SolidColorBrush(darkColor),
            Tag = (rowNum, colNum)
          };

          square.MouseRightButtonDown += Square_MouseRightButtonDown;
          square.MouseRightButtonUp += Square_MouseRightButtonUp;

          if (showCoords)
          {
            if (col == 0)
            {
              var num = new TextBlock
              {
                Text = (8 - rowNum).ToString(),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5),
                Foreground = ((rowNum + colNum) % 2 != 0)
                      ? new SolidColorBrush(lightColor)
                      : new SolidColorBrush(darkColor),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
              };
              square.Children.Add(num);
            }

            if (row == 7)
            {
              var letter = new TextBlock
              {
                Text = ((char)('A' + colNum)).ToString(),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5),
                Foreground = ((rowNum + colNum) % 2 != 0)
                      ? new SolidColorBrush(lightColor)
                      : new SolidColorBrush(darkColor),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
              };
              square.Children.Add(letter);
            }
          }

          if (premoveBoard == null)
            premoveBoard = (string[,])board.Clone();

          string[,]? displayBoard = premoveBoard;

          if (viewBoard != null)
            displayBoard = viewBoard;

          if (!string.IsNullOrEmpty(displayBoard[rowNum, colNum]))
          {
            var piece = new Image
            {
              Source = new BitmapImage(new Uri($"Resources/Pieces/{setName}/{displayBoard[rowNum, colNum]}.png", UriKind.Relative)),
              Stretch = Stretch.Uniform,
              Tag = new Point(rowNum, colNum)
            };

            piece.MouseLeftButtonDown += Piece_MouseLeftButtonDown;
            piece.MouseMove += Piece_MouseMove;
            piece.MouseLeftButtonUp += Piece_MouseLeftButtonUp;

            Point dragOrigin = new Point(MathF.Round((float)_originalPosition.Y / 75), MathF.Round((float)_originalPosition.X / 75));
            if (!(_isDragging && row == (int)dragOrigin.X && col == (int)dragOrigin.Y))
              square.Children.Add(piece);
          }

          boardGrid.Children.Add(square);
        }
      }
      if (viewBoard == null)
      {
        if (fromHighlightSquare != null)
        {
          Grid? fromGrid = GetSquare(boardGrid, (int)fromHighlightSquare.Value.X, (int)fromHighlightSquare.Value.Y);
          if (fromGrid != null)
            Overlays.HighlightSquare(fromGrid, highlightColor, highlightTxt, false);
        }
        if (toHighlightSquare != null)
        {
          Grid? toGrid = GetSquare(boardGrid, (int)toHighlightSquare.Value.X, (int)toHighlightSquare.Value.Y);
          if (toGrid != null)
            Overlays.HighlightSquare(toGrid, highlightColor, highlightTxt, false);
        }

        foreach (Move? pendingPremove in pendingPremoves)
        {
          ShowPremove(pendingPremove);
        }
      }

      if (_originalParentPoint != null)
      {
        Grid? origParent = GetSquare(boardGrid, (int)_originalParentPoint.Value.X, (int)_originalParentPoint.Value.Y);
        _originalParent = origParent;
      }

      boardGrid.MouseLeftButtonDown += OnMouseLeftButtonDown;
    }
    public void UpdateBoard(ChessGame updatedGame)
    {
      game = updatedGame;
      var boardState = game.GetBoard();
      for (int row = 0; row < 8; row++)
      {
        for (int col = 0; col < 8; col++)
        {
          var piece = boardState[row][col];
          if (piece == null)
          {
            board[row, col] = "";
          }
          else
          {
            string color = piece.Owner == Player.White ? "w" : "b";
            char pieceType = piece.GetFenCharacter(); // P, R, N, B, Q, K
            board[row, col] = color + pieceType;
          }
        }
      }

      if (pendingPremoves.Count == 0)
      {
        premoveBoard = (string[,])board.Clone();
        kingMoved = false;
      }

      BuildBoard();
    }
    public string[,] GetBoard()
    {
      return board;
    }
    // Events ------------------------------------------------------------------------------------
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      HidePromotionOverlay();
      Overlays.ClearArrows(arrowLayer);
      Overlays.UnhighlightAllUserHighlights(boardGrid);
    }
    private void Piece_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      HidePromotionOverlay();
      _draggedPiece = sender as Image;
      if (_draggedPiece == null) return;

      _isDragging = true;
      _mouseOffset = e.GetPosition(_draggedPiece);
      _originalParent = VisualTreeHelper.GetParent(_draggedPiece) as Panel;

      if (_originalParent == null) return;

      Overlays.UnhighlightAllUserHighlights(boardGrid);
      Overlays.ClearArrows(arrowLayer);

      _originalPosition = _originalParent.TranslatePoint(new Point(0, 0), boardGrid);

      _originalParentPoint = new Point(MathF.Round((float)_originalPosition.Y / 75), MathF.Round((float)_originalPosition.X / 75));
      if (currentView == Player.Black)
      {
        _originalParentPoint = new Point(7 - _originalParentPoint.Value.X, 7 - _originalParentPoint.Value.Y);
      }

      _originalParent.Children.Remove(_draggedPiece);
      dragLayer.Children.Add(_draggedPiece);

      Canvas.SetZIndex(_draggedPiece, 10);

      var pos = e.GetPosition(boardGrid);
      _draggedPiece.RenderTransform = new TranslateTransform(pos.X - _mouseOffset.X - _originalPosition.X, pos.Y - _mouseOffset.Y - _originalPosition.Y);

      _draggedPiece.CaptureMouse();
    }
    private void Piece_MouseMove(object sender, MouseEventArgs e)
    {
      if (!_isDragging || _draggedPiece == null) return;
      var pos = e.GetPosition(dragLayer);

      if (_draggedPiece.RenderTransform is not TranslateTransform t)
      {
        t = new TranslateTransform();
        _draggedPiece.RenderTransform = t;
      }

      Canvas.SetLeft(_draggedPiece, pos.X - _mouseOffset.X - 12.5);
      Canvas.SetTop(_draggedPiece, pos.Y - _mouseOffset.Y - 12.5);
    }
    private void Piece_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (_draggedPiece == null) return;

      _draggedPiece.ReleaseMouseCapture();
      _isDragging = false;

      var pos = e.GetPosition(boardGrid);

      HitTestResult result = VisualTreeHelper.HitTest(boardGrid, pos);
      if (result == null)
      {
        dragLayer.Children.Remove(_draggedPiece);
        _originalParent?.Children.Add(_draggedPiece);
        _draggedPiece.RenderTransform = Transform.Identity;
        _draggedPiece = null;

        return;
      }

      Grid? targetSquare = FindParentSquare(result.VisualHit);
      string? tag = targetSquare?.Tag.ToString();
      if (tag != null && targetSquare != null)
      {
        while (tag.Contains(highlightTxt) || tag.Contains(premoveTxt))
        {
          if (targetSquare == null) break;

          targetSquare = (Grid)targetSquare.Parent;
          tag = targetSquare?.Tag.ToString();

          if (tag == null) break;
        }
      }

      if (targetSquare == null)
        return;

      dragLayer.Children.Remove(_draggedPiece);

      pos = targetSquare.TranslatePoint(new Point(0, 0), boardGrid);

      Point fromSquare = new Point(MathF.Round((float)_originalPosition.Y / 75), MathF.Round((float)_originalPosition.X / 75));
      Point toSquare = new Point(MathF.Round((float)pos.Y / 75), MathF.Round((float)pos.X / 75));

      Move move = Utils.SquaresToMove(fromSquare, toSquare, game);

      if (currentView == Player.Black)
        move = InvertMove(move);

      if (IsPromotionMove(move) && (game.IsValidMove(move) || Utils.IsPseoudoLegal(move, premoveBoard, game, kingMoved)))
      {
        ShowPromotionOverlay(move);
        return;
      }
      if (targetSquare != null && currentView == Utils.MoveToPlayer(move, premoveBoard))
      {
        if (MakeMove(move) && !Loaded)
        {
          targetSquare.Children.Add(_draggedPiece);
        }
        else if (Utils.IsPseoudoLegal(move, premoveBoard, game, kingMoved) && (Utils.MoveToPlayer(move, premoveBoard) != game.WhoseTurn) && !Loaded)
        {
          int fromRow = 8 - move.OriginalPosition.Rank;
          int fromCol = (int)move.OriginalPosition.File;

          if (Utils.GetPiece(new Point(fromRow, fromCol), premoveBoard)[1] == 'K')
          {
            kingMoved = true;
          }

          MakePremove(move);
          targetSquare.Children.Clear();
          targetSquare.Children.Add(_draggedPiece);
          UpdateBoard(game);
          ShowPremove(move);
        }
        else
        {
          if (_originalParent != null && _draggedPiece != null && !(_draggedPiece.Parent is Panel _))
          {
            _originalParent.Children.Add(_draggedPiece);
          }
        }
      }
      else
        _originalParent?.Children.Add(_draggedPiece);


      if (_draggedPiece != null)
        _draggedPiece.RenderTransform = Transform.Identity;
      _draggedPiece = null;
    }
    private Point? _arrowStart = null;
    private void Square_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (pendingPremoves.Count > 0)
      {
        pendingPremoves = new List<Move?>();
        UpdateBoard(game);
        return;
      }

      if (sender is not Grid _) return;

      _arrowStart = e.GetPosition(boardGrid);
      if (_arrowStart != null)
      {
        HitTestResult result = VisualTreeHelper.HitTest(boardGrid, _arrowStart.Value);
        Grid? parentSquare = FindParentSquare(result.VisualHit);
        if (parentSquare != null)
          _arrowStart = parentSquare.TranslatePoint(new Point(37.5, 37.5), boardGrid);
      }

      e.Handled = true;
    }
    private void Square_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (sender is not Grid square) return;

      var arrowEnd = e.GetPosition(boardGrid);
      HitTestResult result = VisualTreeHelper.HitTest(boardGrid, arrowEnd);
      Grid? parentSquare = Board.FindParentSquare(result.VisualHit);
      if (parentSquare != null)
        arrowEnd = parentSquare.TranslatePoint(new Point(37.5, 37.5), boardGrid);

      if (arrowEnd == _arrowStart)
      {
        if (Keyboard.IsKeyDown(Key.LeftShift))
          Overlays.HighlightSquare(square, userHighlightColorGreen, "UserHighlight", true);
        else if (Keyboard.IsKeyDown(Key.LeftCtrl))
          Overlays.HighlightSquare(square, userHighlightColorOrange, "UserHighlightOrange", true);
        else if (Keyboard.IsKeyDown(Key.LeftAlt))
          Overlays.HighlightSquare(square, userHighlightColorBlue, "UserHighlightBlue", true);
        else
          Overlays.HighlightSquare(square, userHighlightColor, "UserHighlightRed", true);
      }
      else if (_arrowStart != null)
        Overlays.DrawArrow(arrowLayer, _arrowStart.Value, arrowEnd, arrowColor);

      _arrowStart = null;
    }

    // Utils ------------------------------------------------------------------------------------
    public void SetBotOne(Bot bot)
    {
      botOne = bot;
    }
    public void SetBotTwo(Bot bot)
    {
      botTwo = bot;
    }
    public ChessGame GetGame()
    {
      return game;
    }

    public void SetGame(ChessGame game)
    {
      this.game = game;
    }
    public static Grid? GetSquare(Panel ChessBoardGrid, int row, int col)
    {
      foreach (var child in ChessBoardGrid.Children)
      {
        if (child is Grid square && square.Tag is ValueTuple<int, int> tag && tag.Item1 == row && tag.Item2 == col)
        {
          return square;
        }
      }
      return null;
    }
    public static Grid? FindParentSquare(DependencyObject obj)
    {
      while (obj != null && obj is not Grid)
      {
        obj = VisualTreeHelper.GetParent(obj);
      }

      return obj as Grid;
    }

    // GamePlay ------------------------------------------------------------------------------------
    public bool MakeMove(Move move)
    {
      viewBoard = null;
      (Point fromSquare, Point toSquare) = Utils.MoveToSquares(move);

      if (game.IsValidMove(move))
      {
        var notation = Utils.AlgebraicNotation(game, move, board);

        game.MakeMove(move, true);
        MoveHistory.Add(InvertPlayer(game.WhoseTurn), notation, game.GetFen(), fromSquare, toSquare);

        if (!string.IsNullOrEmpty(currentGamePath))
        {
          MoveHistory.SaveToJson(currentGamePath, selectedBot, selectedDifficulty, selectedColor);
        }

        if (pendingPremoves.Count > 0 && move.Player == pendingPremoves[0]?.Player)
        {
          string piece = premoveBoard[(int)fromSquare.X, (int)fromSquare.Y];
          premoveBoard[(int)fromSquare.X, (int)fromSquare.Y] = "";
          premoveBoard[(int)toSquare.X, (int)toSquare.Y] = piece;
        }

        Grid? fromGrid = GetSquare(boardGrid, (int)fromSquare.X, (int)fromSquare.Y);
        Grid? toGrid = GetSquare(boardGrid, (int)toSquare.X, (int)toSquare.Y);

        if (fromGrid != null)
          fromHighlightSquare = new Point((int)fromSquare.X, (int)fromSquare.Y);
        if (toGrid != null)
          toHighlightSquare = new Point((int)toSquare.X, (int)toSquare.Y);

        if (_originalParent == toGrid)
        {
          _draggedPiece?.ReleaseMouseCapture();
          dragLayer.Children.Remove(_draggedPiece);
          _isDragging = false;
          _draggedPiece = null;
        }

        UpdateBoard(game);


        if (fromGrid != null)
        {
          Overlays.UnhighlightSquare(fromGrid, premoveTxt);
          Overlays.HighlightSquare(fromGrid, highlightColor, highlightTxt, false);
        }
        if (toGrid != null)
        {
          Overlays.UnhighlightSquare(toGrid, premoveTxt);
          Overlays.HighlightSquare(toGrid, highlightColor, highlightTxt, false);
        }

        _ = Play();
        return true;
      }
      else
      {
        return false;
      }
    }
    public async Task Play()
    {
      if (RandomTurn == null)
      {
        Random rand = new Random();
        var random = rand.Next(2);
        RandomTurn = random > 0 ? Player.White : Player.Black;
        InvertedRandomTurn = random > 0 ? Player.Black : Player.White;

        if ((botOne != null && botTwo == null && RandomTurn == Player.White) || (botOne == null && botTwo != null && InvertedRandomTurn == Player.White))
        {
          SetBoardDisplay(Player.Black);
        }
        else SetBoardDisplay(Player.White);

      }
      if (botOne != null)
        botOne.SetColor(RandomTurn);
      if (botTwo != null)
        botTwo.SetColor(InvertedRandomTurn);

      if (!game.IsCheckmated(game.WhoseTurn) && !game.IsStalemated(game.WhoseTurn))
      {
        if (game.WhoseTurn == RandomTurn && botOne != null)
        {
          await botOne.MakeMove();
        }
        else if (botTwo != null)
        {
          await botTwo.MakeMove();
        }
        TryPlayPremove();
      }
      else
      {
        if (game.IsStalemated(game.WhoseTurn))
        {
          if (currentGamePath != null)
            MoveHistory.SaveToJson(currentGamePath, selectedBot, selectedDifficulty, selectedColor, true, "Draw by Stalemate");
          if (!Loaded)
            EndScreen("Draw", "by Stalemate");
        }
        else
        {
          if (game.WhoseTurn == Player.Black)
          {
            if (currentGamePath != null)
              MoveHistory.SaveToJson(currentGamePath, selectedBot, selectedDifficulty, selectedColor, true, "White Wins by checkmate");
            if (!Loaded)
              EndScreen("White Wins", "by checkmate");
          }
          else
          {
            if (currentGamePath != null)
              MoveHistory.SaveToJson(currentGamePath, selectedBot, selectedDifficulty, selectedColor, true, "Black  Wins by checkmate");
            if (!Loaded)
              EndScreen("Black Wins", "by checkmate");
          }

        }

        currentGamePath = null;
      }
    }
    public void ResetGame(string user, bool newPath = false, bool invertColors = false)
    {
      game = new ChessGame();

      fromHighlightSquare = null;
      toHighlightSquare = null;

      if (invertColors)
      {
        if (selectedColor == "White")
          selectedColor = "Black";
        else selectedColor = "White";
      }

      if (newPath)
      {
        string folder = System.IO.Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
          $"ChessGames/{user}"
        );

        System.IO.Directory.CreateDirectory(folder);

        currentGamePath = System.IO.Path.Combine(
            folder,
            $"game_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json"
        );


        MoveHistory.SaveToJson(
                currentGamePath,
                selectedBot,
                selectedDifficulty,
                selectedColor
                );
      }
      board = new string[8, 8]
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

      Overlays.ClearArrows(arrowLayer);
      Overlays.UnhighlightAllUserHighlights(boardGrid);
      MoveHistory.Clear();

      InvertBoardDisplay();
      BuildBoard();

      if (invertColors)
      {
        if (RandomTurn == Player.Black)
        {
          RandomTurn = Player.White;
          InvertedRandomTurn = Player.Black;
        }
        else
        {
          RandomTurn = Player.Black;
          InvertedRandomTurn = Player.White;
        }
      }

      if (botOne != null)
        botOne.Reset();
      if (botTwo != null)
        botTwo.Reset();
      _ = Play();
    }

    // Promotion ------------------------------------------------------------------------------------
    void ShowPromotionOverlay(Move move)
    {
      promotionLayer.Children.Clear();

      if (currentView == Player.Black)
        move = InvertMove(move);

      pendingPromotionMove = move;
      var (_, to) = Utils.MoveToSquares(move);

      var panel = new StackPanel
      {
        Orientation = Orientation.Vertical,
        Background = Brushes.White,
        Width = 75,
        Height = 300
      };

      string piece = Utils.GetPiece(new Point(
          8 - move.OriginalPosition.Rank,
          (int)move.OriginalPosition.File), premoveBoard);

      char[] promotionPieces = new[] { 'Q', 'R', 'B', 'N' };

      if ((int)to.X == 7)
        Array.Reverse(promotionPieces);

      foreach (char p in promotionPieces)
      {
        var img = new Image
        {
          Source = new BitmapImage(
            new Uri($"/Resources/Pieces/{setName}/{piece[0]}{p}.png",
                UriKind.Relative)),
          Width = 75,
          Height = 75,
          Tag = p
        };

        img.MouseLeftButtonDown += PromotionClicked;
        panel.Children.Add(img);
      }

      if ((int)to.X == 0)
      {
        Canvas.SetLeft(panel, to.Y * 75);
        Canvas.SetTop(panel, to.X * 75);
      }
      else if ((int)to.X == 7)
      {
        Canvas.SetLeft(panel, to.Y * 75);
        Canvas.SetTop(panel, 4 * 75);
      }

      promotionLayer.Children.Add(panel);
    }

    public void HidePromotionOverlay()
    {
      promotionLayer.Children.Clear();
      if (pendingPromotionMove != null)
        CancelMove();
      pendingPromotionMove = null;
    }

    void CancelMove()
    {
      if (_originalParent != null && _draggedPiece != null)
        _originalParent?.Children.Add(_draggedPiece);
    }

    void PromotionClicked(object sender, MouseButtonEventArgs e)
    {
      if (pendingPromotionMove == null) return;

      char pieceChar = (char)((Image)sender).Tag;

      var finalMove = new Move(
          pendingPromotionMove.OriginalPosition,
          pendingPromotionMove.NewPosition,
          game.WhoseTurn,
          pieceChar);

      promotionLayer.Children.Clear();
      pendingPromotionMove = null;

      if (currentView == Player.Black)
        finalMove = InvertMove(finalMove);

      if (!MakeMove(finalMove) && Utils.IsPseoudoLegal(finalMove, premoveBoard, game, kingMoved))
      {
        MakePremove(finalMove);
        _originalParent?.Children.Clear();
        int rowNum = 8 - finalMove.NewPosition.Rank;
        int colNum = (int)finalMove.NewPosition.File;
        Grid? targetSquare = GetSquare(boardGrid, rowNum, colNum);
        targetSquare?.Children.Clear();
        if (_draggedPiece != null)
        {
          targetSquare?.Children.Clear();
          targetSquare?.Children.Add(_draggedPiece);

          string piece = premoveBoard[rowNum, colNum];

          if (!string.IsNullOrEmpty(piece))
          {
            _draggedPiece.Source = new BitmapImage(
              new Uri($"Resources/Pieces/{setName}/{piece}.png", UriKind.Relative));
          }

        }
        ShowPremove(finalMove);
      }
    }


    public Player? GetRandomTurn()
    {
      return RandomTurn;
    }
    bool IsPromotionMove(Move move)
    {
      int toRank = move.NewPosition.Rank;

      string piece = Utils.GetPiece(new Point(
          8 - move.OriginalPosition.Rank,
          (int)move.OriginalPosition.File), premoveBoard);

      if (string.IsNullOrEmpty(piece))
        return false;

      char type = char.ToLower(piece[1]);

      if (type != 'p')
        return false;

      return (piece[0] == 'w' && toRank == 8) ||
            (piece[0] == 'b' && toRank == 1);
    }

    // Premoves -------------------------------------------------------------------------------------
    private List<Move?> pendingPremoves = new List<Move?>();
    private bool kingMoved = false;

    void MakePremove(Move move)
    {
      pendingPremoves.Add(move);

      (Point fromSquare, Point toSquare) = Utils.MoveToSquares(move);

      string piece = premoveBoard[(int)fromSquare.X, (int)fromSquare.Y];

      premoveBoard[(int)fromSquare.X, (int)fromSquare.Y] = "";
      if (move.Promotion == null)
        premoveBoard[(int)toSquare.X, (int)toSquare.Y] = piece;
      else
        premoveBoard[(int)toSquare.X, (int)toSquare.Y] = piece[0].ToString() + move.Promotion;

      int dist = (int)(fromSquare.Y - toSquare.Y);

      if (piece[1] == 'K' && Math.Abs(dist) == 2)
      {
        premoveBoard[(int)toSquare.X, (int)toSquare.Y + (dist / 2)] = piece[0] + "R";

        if (dist < 0)
          premoveBoard[7, 7] = "";
        else
          premoveBoard[7, 0] = "";
      }
    }
    void ShowPremove(Move? move)
    {
      if (move == null)
        return;
      var (from, to) = Utils.MoveToSquares(move);

      Grid? fromGrid = GetSquare(boardGrid, (int)from.X, (int)from.Y);
      Grid? toGrid = GetSquare(boardGrid, (int)to.X, (int)to.Y);

      if (fromGrid != null)
      {
        Overlays.UnhighlightSquare(fromGrid, highlightTxt);
        Overlays.HighlightSquare(fromGrid, userHighlightColor, premoveTxt, false);
      }

      if (toGrid != null)
      {
        Overlays.UnhighlightSquare(toGrid, highlightTxt);
        Overlays.HighlightSquare(toGrid, userHighlightColor, premoveTxt, false);
      }

      int dist = (int)(from.Y - to.Y);
      string piece = Utils.GetPiece(to, premoveBoard);
      if (piece != "" && piece[1] == 'K' && Math.Abs(dist) == 2)
      {
        Grid? fromRook;
        if (dist < 0)
        {
          fromRook = GetSquare(boardGrid, 7, 7);
          if (fromRook != null)
            Overlays.HighlightSquare(fromRook, userHighlightColor, premoveTxt, false);
        }
        else
        {
          fromRook = GetSquare(boardGrid, 7, 0);
          if (fromRook != null)
            Overlays.HighlightSquare(fromRook, userHighlightColor, premoveTxt, false);
        }

        Grid? toRook = GetSquare(boardGrid, 7, (int)(from.Y - dist / 2));
        if (toRook != null)
          Overlays.HighlightSquare(toRook, userHighlightColor, premoveTxt, false);
      }
    }
    void TryPlayPremove()
    {
      if (pendingPremoves.Count == 0)
        return;
      Move? pendingPremove = pendingPremoves[0];
      if (game.WhoseTurn == pendingPremove?.Player)
        return;

      (Point fromSquare, Point toSquare) = Utils.MoveToSquares(pendingPremove);
      Move premove = Utils.SquaresToMove(fromSquare, toSquare, game);
      premove.Promotion = pendingPremove?.Promotion;

      if (game.IsValidMove(premove))
      {
        pendingPremoves.RemoveAt(0);
        MakeMove(premove);
      }
      else
      {
        pendingPremoves = new List<Move?>();
        UpdateBoard(game);
      }
    }

    // Display --------------------------------------------------------------------------------------
    public void SetPieceSet(string setName)
    {
      this.setName = setName;
      UpdateBoard(game);
    }
    public void SetBoardDisplay(Player? player)
    {
      currentView = player;
      UpdateBoard(game);
    }
    public void InvertBoardDisplay()
    {
      if (currentView == Player.Black)
        currentView = Player.White;
      else
        currentView = Player.Black;
    }
    static Move InvertMove(Move move)
    {
      Position from = move.OriginalPosition;
      Position to = move.NewPosition;

      var invertedFrom = new Position(
          (File)(7 - (int)from.File),
          9 - from.Rank
      );

      var invertedTo = new Position(
          (File)(7 - (int)to.File),
          9 - to.Rank
      );

      return new Move(
          invertedFrom,
          invertedTo,
          move.Player,
          move.Promotion
      );
    }

    static Player InvertPlayer(Player player)
    {
      if (player == Player.White)
        return Player.Black;
      else if (player == Player.Black)
        return Player.White;
      else return Player.None;
    }
    public void SetTurn(Player player)
    {
      RandomTurn = InvertPlayer(player);
      SetBoardDisplay(player);
    }
    public void ShowPosition(string fen)
    {
      if (fen != null && fen != "")
        ShowPosition(Utils.FenToBoard(fen));
    }
    void ShowPosition(string[,] position)
    {
      if (position == null)
        return;

      viewBoard = position;
      pendingPremoves = new List<Move?>();
      UpdateBoard(game);
    }

    public string GetPiece(Point square, Player? color)
    {
      if (color == Player.White)
        return Utils.GetPiece(square, board);
      else
        return Utils.GetPiece(new Point(7 - square.X, 7 - square.Y), board);
    }

    public void DrawHintArrow(Point start, Point end)
    {
      Grid? startSquare = GetSquare(boardGrid, (int)start.X, (int)start.Y);
      if (startSquare != null)
        start = startSquare.TranslatePoint(new Point(37.5, 37.5), boardGrid);

      Grid? endSquare = GetSquare(boardGrid, (int)end.X, (int)end.Y);
      if (endSquare != null)
        end = endSquare.TranslatePoint(new Point(37.5, 37.5), boardGrid);

      Overlays.DrawArrow(arrowLayer, start, end, hintArrowColor);
    }

    public void HighlightSquare(Point square)
    {
      Grid? squareToHighlight = GetSquare(boardGrid, (int)square.X, (int)square.Y);
      if (squareToHighlight != null)
        Overlays.HighlightSquare(squareToHighlight, highlightColor, highlightTxt, false);
    }

    public void UndoMove()
    {

      if (currentView == game.WhoseTurn)
      {
        game.Undo();
        MoveHistory.RemoveLast();
      }
      game.Undo();
      MoveHistory.RemoveLast();
      ShowPosition(game.GetFen());
      UpdateBoard(game);
    }
  }
}
