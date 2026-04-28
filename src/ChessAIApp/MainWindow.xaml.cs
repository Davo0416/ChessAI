using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChessDotNet;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace ChessAIApp
{
    public partial class MainWindow : Window
    {
        //Window management variables
        Board? chessBoard;
        private string? selectedBot = null;
        private string? selectedColor = "Random";
        private string? selectedDifficulty = "Easy";

        private string? loadedGamePath;
        private MoveHistory.SavedGame? loadedGame;

        private ProfileStats profileStats = new ProfileStats();

        private string? currentUser;

        //DB access API
        private readonly HttpClient http = new HttpClient
        {
            BaseAddress = new Uri("https://chessai-84pl.onrender.com")
        };
        private const string whiteTxt = "White";
        private const string blackTxt = "Black";
        public MainWindow()
        {
            InitializeComponent();

            //Available Piece Sets
            PieceSetCombo.ItemsSource = new[]
            {
                "alpha",
                "caliente",
                "california",
                "cardinal",
                "cburnett",
                "celtic",
                "chess7",
                "chessnut",
                "cooke",
                "dubrovny",
                "fantasy",
                "firi",
                "fresca",
                "gioco",
                "governor",
                "icpieces",
                "kiwen-suwi",
                "kosal",
                "leipzig",
                "maestro",
                "merida",
                "monarchy",
                "mpchess",
                "pixel",
                "rhosgfx",
                "riohacha",
                "shahi-ivory-brown",
                "staunty",
                "tatiana",
                "xkcd"
            };

            //Available Themes
            ThemeCombo.ItemsSource = new[]
            {
                "DefaultTheme",
                "DarkTheme",
                "BlueTheme",
                "DarkBlueTheme",
                "GreenTheme",
                "OrangeTheme",
                "PinkTheme",
                "PurpleTheme",
                "RedTheme",
                "TanTheme"
            };

            PieceSetCombo.SelectedIndex = 1;
            ThemeCombo.SelectedIndex = 4;

        }

        //User class - synced with API user class
        public class User
        {
            public string? Id { get; set; }
            public string? Username { get; set; }
            public string? PieceSet { get; set; }
            public string? Theme { get; set; }
            public string? PasswordHash { get; set; }
        }

        //Game class - synced with API game class
        public class ServerGame
        {
            public string? Id { get; set; }
            public string? Username { get; set; }
            public string? PieceSet { get; set; }
            public string? Theme { get; set; }
            public string? JsonData { get; set; }
            public string? ClientId { get; set; }
            public DateTime LastModified { get; set; }
        }

        //When window is closed - save everything then exit
        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            try
            {
                await SyncGames();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during exit sync: " + ex.Message);
            }
        } 

        //Button & other UI handlers

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                Undo_Click(this, null);
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                Redo_Click(this, null);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                FullyRedo_Click(this, null);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                FullyUndo_Click(this, null);
                e.Handled = true;
            }
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = true;
        }

        private async void PieceSetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PieceSetCombo.SelectedItem is string pieceSet)
            {
                chessBoard?.SetPieceSet(pieceSet);
                await UpdatePreferences();
            }
        }

        private async void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeCombo.SelectedItem is string theme)
            {
                ThemeManager.LoadTheme(theme);
                chessBoard?.UpdateBoard(chessBoard.GetGame());
                await UpdatePreferences();
            }
        }
        
        //Handler for the move selection from the table
        private void MoveCell_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                int rowIndex = MovesTable.Items.IndexOf(btn.DataContext);

                DependencyObject parent = btn;
                while (parent != null && parent is not DataGridCell)
                    parent = VisualTreeHelper.GetParent(parent);

                if (parent is DataGridCell cell)
                {
                    int columnIndex = cell.Column.DisplayIndex;

                    MoveHistory.Select(rowIndex, columnIndex);
                    chessBoard?.ShowPosition(MoveHistory.GetSelectedFen());
                    (Point start, Point end) = MoveHistory.GetSelectedHighlights();
                    chessBoard?.HighlightSquare(start);
                    chessBoard?.HighlightSquare(end);
                }
            }
        }
        
        //Undo/Redo handlers
        private void FullyUndo_Click(object sender, RoutedEventArgs e)
        {
            if (MoveHistory.GetLength() > 0)
            {
                MoveHistory.SelectFirst();
                chessBoard?.ShowPosition(MoveHistory.GetSelectedFen());
                (Point start, Point end) = MoveHistory.GetSelectedHighlights();
                chessBoard?.HighlightSquare(start);
                chessBoard?.HighlightSquare(end);
            }
        }
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (MoveHistory.GetLength() > 0)
            {
                MoveHistory.SelectPrevious();
                chessBoard?.ShowPosition(MoveHistory.GetSelectedFen());
                (Point start, Point end) = MoveHistory.GetSelectedHighlights();
                chessBoard?.HighlightSquare(start);
                chessBoard?.HighlightSquare(end);
            }
        }

        private void FullyRedo_Click(object sender, RoutedEventArgs e)
        {
            if (MoveHistory.GetLength() > 0)
            {
                MoveHistory.SelectLast();
                chessBoard?.ShowPosition(MoveHistory.GetSelectedFen());
                (Point start, Point end) = MoveHistory.GetSelectedHighlights();
                chessBoard?.HighlightSquare(start);
                chessBoard?.HighlightSquare(end);
            }
        }
        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (MoveHistory.GetLength() > 0)
            {
                MoveHistory.SelectNext();
                chessBoard?.ShowPosition(MoveHistory.GetSelectedFen());
                (Point start, Point end) = MoveHistory.GetSelectedHighlights();
                
                chessBoard?.HighlightSquare(start);
                chessBoard?.HighlightSquare(end);
            }
        }

        //Menu button handlers
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            MenuView.Visibility = Visibility.Collapsed;
            BotSelectView.Visibility = Visibility.Visible;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        //Game start menu handlers
        private void SelectBot_Click(object sender, RoutedEventArgs e)
        {
            // Reset all bot buttons
            EasyBotBtn.ClearValue(Button.BackgroundProperty);
            MediumBotBtn.ClearValue(Button.BackgroundProperty);
            HardBotBtn.ClearValue(Button.BackgroundProperty);

            EasyBotBtn.Tag = null;
            MediumBotBtn.Tag = null;
            HardBotBtn.Tag = null;

            // Highlight selected
            Button? clicked = sender as Button;
            clicked?.Tag = "Selected";


            if (clicked != null)
            {
                StackPanel? panel = clicked.Content as StackPanel;

                if (panel != null)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is TextBlock textBlock)
                        {
                            selectedBot = textBlock.Text;
                            break;
                        }
                    }
                }
            }
        }

        private void Color_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton? rb = sender as RadioButton;
            selectedColor = rb?.Content.ToString();
        }

        private void Difficulty_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton? rb = sender as RadioButton;
            selectedDifficulty = rb?.Content.ToString();
        }

        //Start game button
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            //Warn if bot not selected
            if (selectedBot == null)
            {
                MessageBox.Show("Please select a bot first.");
                return;
            }

            //Change app view to game mode
            BotSelectView.Visibility = Visibility.Collapsed;
            GameView.Visibility = Visibility.Visible;

            ResignControl.Visibility = Visibility.Visible;
            BottomControls.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Collapsed;

            //Apply selections and start game
            if (chessBoard != null && currentUser != null)
            {
                chessBoard.ResetGame(currentUser, true);

                if (selectedBot == "Random Bot")
                {
                    RandomBot randomBot = new RandomBot(chessBoard);
                    chessBoard.SetBotOne(randomBot);
                }
                else if (selectedBot == "Easy Bot")
                {
                    Quasarv0 Quasarv0 = new Quasarv0(chessBoard);
                    chessBoard.SetBotOne(Quasarv0);
                }
                else if (selectedBot == "Hard Bot")
                {
                    Quasarv01 Quasarv01 = new Quasarv01(chessBoard);
                    chessBoard.SetBotOne(Quasarv01);
                }

                chessBoard.selectedBot = selectedBot;

                if (selectedColor == whiteTxt)
                    chessBoard.SetTurn(Player.White);
                else if (selectedColor == blackTxt)
                    chessBoard.SetTurn(Player.Black);
                else if (selectedColor == "Random")
                {
                    Random rand = new Random();
                    if (rand.Next(2) > 0)
                    {
                        selectedColor = whiteTxt;
                        chessBoard.SetTurn(Player.White);
                    }
                    else
                    {
                        selectedColor = blackTxt;
                        chessBoard.SetTurn(Player.Black);
                    }
                }

                chessBoard.selectedColor = selectedColor;

                if (selectedDifficulty == "Easy")
                    BottomControls.Visibility = Visibility.Visible;
                else if (selectedDifficulty == "Hard")
                    BottomControls.Visibility = Visibility.Collapsed;

                chessBoard.selectedDifficulty = selectedDifficulty;

                //Create new json file and save the game to there
                string folder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"ChessGames/{currentUser}"
                );

                System.IO.Directory.CreateDirectory(folder);

                chessBoard.currentGamePath = loadedGamePath ?? System.IO.Path.Combine(
                    folder,
                    $"game_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json"
                    );

                MoveHistory.SaveToJson(
                chessBoard.currentGamePath,
                selectedBot,
                selectedDifficulty,
                selectedColor
                );

                chessBoard.Loaded = false;

                _ = chessBoard.Play();
            }
        }

        //Other button handlers
        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            BotSelectView.Visibility = Visibility.Collapsed;
            MenuView.Visibility = Visibility.Visible;
        }

        private void Hint_Click(object sender, RoutedEventArgs e)
        {
            if (chessBoard != null)
            {
                Quasarv01 Quasarv01 = new Quasarv01(chessBoard);
                Move? hint = Quasarv01.Evaluate(4);
                (Point fromSquare, Point toSquare) = Utils.MoveToSquares(hint);
                chessBoard.DrawHintArrow(fromSquare, toSquare);
            }
        }

        private void Resign_Click(object sender, RoutedEventArgs e)
        {
            ResignDialog.Visibility = Visibility.Visible;
        }

        private void CancelResign_Click(object sender, RoutedEventArgs e)
        {
            ResignDialog.Visibility = Visibility.Collapsed;
        }

        private void ConfirmResign_Click(object sender, RoutedEventArgs e)
        {
            ResignDialog.Visibility = Visibility.Collapsed;

            if (chessBoard != null)
            {
                Player? turn = chessBoard.GetRandomTurn();
                if (chessBoard.currentGamePath != null)
                    MoveHistory.SaveToJson(chessBoard.currentGamePath, selectedBot, selectedDifficulty, selectedColor, true, $"{turn} Wins by resignation");
                EndScreen($"{turn} Wins", "by resignation");
                chessBoard.currentGamePath = null;
            }
        }
        
        //Game end screen handlers
        private async void EndScreen(string text, string reasonText)
        {
            WinnerText.Text = text;
            ResultReasonText.Text = reasonText;
            ResultScreen.Visibility = Visibility.Visible;
            loadedGamePath = null;
            await SyncGames();
        }

        private void Rematch_Click(object sender, RoutedEventArgs e)
        {
            ResultScreen.Visibility = Visibility.Collapsed;

            if (chessBoard != null && currentUser != null)
            {
                chessBoard.ResetGame(currentUser, true, true);
                chessBoard.ShowPosition(MoveHistory.startingFen);
                _ = chessBoard.Play();
            }
        }

        private void MenuFromResult_Click(object sender, RoutedEventArgs e)
        {
            ResultScreen.Visibility = Visibility.Collapsed;

            GameView.Visibility = Visibility.Collapsed;
            MenuView.Visibility = Visibility.Visible;
        }

        //Undo last played move handler
        private void UndoMove_Click(object sender, RoutedEventArgs e)
        {
            if (chessBoard != null)
            {
                chessBoard.UndoMove();
                FullyRedo_Click(sender, e);
            }
        }

        //Return to profile from viewing a game
        private void BackToProfile_Click(object sender, RoutedEventArgs e)
        {
            GameView.Visibility = Visibility.Collapsed;
            ProfileView.Visibility = Visibility.Visible;
        }

        //Load last unfinished game
        private void LoadLastUnfinishedGame()
        {
            //Get user folder
            string folder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"ChessGames/{currentUser}"
            );

            if (!System.IO.Directory.Exists(folder))
                return;

            //Check for an unfinished game in the folder
            var files = System.IO.Directory.GetFiles(folder, "*.json");

            foreach (var file in files)
            {
                var game = MoveHistory.LoadGame(file);
                
                //Load game if is unfinished
                if (game != null && !game.IsFinished)
                {
                    if (game.Moves.Count == 0) continue;

                    loadedGamePath = file;
                    loadedGame = game;

                    MoveHistory.Clear();
                    foreach (var move in game.Moves)
                        MoveHistory.Add(move);

                    //Apply the game bot, difficulty and player color
                    selectedBot = game.BotName;
                    selectedDifficulty = game.Difficulty;
                    selectedColor = game.PlayerColor;

                    //Apply the game
                    ApplyLoadedGame();

                    return;
                }
            }
        }

        //Load game from a given filepath
        private void LoadGameFromFile(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var json = System.IO.File.ReadAllText(path);
            var game = JsonConvert.DeserializeObject<MoveHistory.SavedGame>(json);

            if (game == null) return;

            // restore move list
            MoveHistory.Clear();
            if (game.Moves.Count > 0)
                foreach (var move in game.Moves)
                    MoveHistory.Add(move);

            // restore UI state
            selectedBot = game.BotName;
            selectedDifficulty = game.Difficulty;
            selectedColor = game.PlayerColor;

            // switch view
            MenuView.Visibility = Visibility.Collapsed;
            ProfileView.Visibility = Visibility.Collapsed;
            GameView.Visibility = Visibility.Visible;
            ResultScreen.Visibility = Visibility.Collapsed;

            // restore board
            if (chessBoard != null)
            {
                if (game.Moves.Count > 0)
                {
                    chessBoard.SetGame(
                        new ChessGame(game.Moves[game.Moves.Count - 1].BlackFen ?? game.Moves[game.Moves.Count - 1].WhiteFen)
                    );

                    MoveHistory.SelectLast();
                    chessBoard.ShowPosition(MoveHistory.GetSelectedFen());
                    (Point start, Point end) = MoveHistory.GetSelectedHighlights();
                    chessBoard.HighlightSquare(start);
                    chessBoard.HighlightSquare(end);
                }
                else
                {
                    chessBoard.SetGame(
                        new ChessGame(MoveHistory.startingFen)
                    );
                    chessBoard.ShowPosition(MoveHistory.startingFen);
                }

                chessBoard.Loaded = true;

                BackButton.Visibility = Visibility.Visible;
                BottomControls.Visibility = Visibility.Collapsed;
                ResignControl.Visibility = Visibility.Collapsed;

                chessBoard.SetTurn(
                    game.PlayerColor == whiteTxt ? Player.White : Player.Black
                );
            }
        }

        //Apply the loaded game
        private void ApplyLoadedGame()
        {
            //Change UI to game mode
            MenuView.Visibility = Visibility.Collapsed;
            BotSelectView.Visibility = Visibility.Collapsed;
            GameView.Visibility = Visibility.Visible;
            ResignControl.Visibility = Visibility.Visible;


            //Apply the game parameters and restore move history
            if (chessBoard == null || loadedGame == null)
                return;

            chessBoard.SetGame(new ChessGame(loadedGame.Moves[loadedGame.Moves.Count - 1].BlackFen ?? loadedGame.Moves[loadedGame.Moves.Count - 1].WhiteFen));
            
            if (loadedGame.BotName == "Random Bot")
                chessBoard.SetBotOne(new RandomBot(chessBoard));
            else if (loadedGame.BotName == "Easy Bot")
                chessBoard.SetBotOne(new Quasarv0(chessBoard));
            else if (loadedGame.BotName == "Hard Bot")
                chessBoard.SetBotOne(new Quasarv01(chessBoard));

            if (loadedGame.Difficulty == "Easy")
                BottomControls.Visibility = Visibility.Visible;
            else
                BottomControls.Visibility = Visibility.Collapsed;

            if (loadedGame.PlayerColor == whiteTxt)
                chessBoard.SetTurn(Player.White);
            else if (loadedGame.PlayerColor == blackTxt)
                chessBoard.SetTurn(Player.Black);

            chessBoard.currentGamePath = loadedGamePath;
            chessBoard.selectedBot = loadedGame.BotName;
            chessBoard.selectedDifficulty = loadedGame.Difficulty;
            chessBoard.selectedColor = loadedGame.PlayerColor;

            _ = chessBoard.Play();
        }

        //Game Summary class used for game info display
        public class GameSummary
        {
            public string? FilePath { get; set; }
            public bool? Won { get; set; }
            public string? Result { get; set; }
            public string? PlayerScore { get; set; }
            public string? Opponent { get; set; }
            public string? OpponentScore { get; set; }
            public string? ResultSymbol { get; set; }
            public string? Difficulty { get; set; }
            public int MoveCount { get; set; }
            public string? Date { get; set; }

            public Brush BorderColor
            {
                get
                {
                    return Won switch
                    {
                        true => Brushes.LimeGreen,
                        false => Brushes.IndianRed,
                        null => Brushes.DarkGray,
                    };
                }
            }
        }


        //Profile Stats class used for profile info display
        public class ProfileStats : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            private string? _username = "Player1";
            public string? Username
            {
                get => _username;
                set { _username = value; OnPropertyChanged(); }
            }

            private string? _profilePicture = "https://upload.wikimedia.org/wikipedia/commons/0/03/Twitter_default_profile_400x400.png";

            public string? ProfilePicture
            {
                get => _profilePicture;
                set { _profilePicture = value; OnPropertyChanged(); }
            }

            private string? _country;
            public string? Country
            {
                get => _country;
                set { _country = value; OnPropertyChanged(); }
            }

            private string _flagImage = "https://flagcdn.com/w40/ie.png";
            public string FlagImage
            {
                get => _flagImage;
                set { _flagImage = value; OnPropertyChanged(); }
            }

            private int _movesPlayedCount;
            public int MovesPlayedCount
            {
                get => _movesPlayedCount;
                set { _movesPlayedCount = value; OnPropertyChanged(); }
            }

            private int _captureCount;
            public int CaptureCount
            {
                get => _captureCount;
                set { _captureCount = value; OnPropertyChanged(); }
            }

            private int _checkCount;
            public int CheckCount
            {
                get => _checkCount;
                set { _checkCount = value; OnPropertyChanged(); }
            }

            private int _stalemateCount;
            public int StalemateCount
            {
                get => _stalemateCount;
                set { _stalemateCount = value; OnPropertyChanged(); }
            }

            private int _castleCount;
            public int CastleCount
            {
                get => _castleCount;
                set { _castleCount = value; OnPropertyChanged(); }
            }

            private int _promotionCount;
            public int PromotionCount
            {
                get => _promotionCount;
                set { _promotionCount = value; OnPropertyChanged(); }
            }

            private string _whiteOpener = "e4";
            public string WhiteOpener
            {
                get => _whiteOpener;
                set { _whiteOpener = value; OnPropertyChanged(); }
            }

            private string _blackOpener = "e5";
            public string BlackOpener
            {
                get => _blackOpener;
                set { _blackOpener = value; OnPropertyChanged(); }
            }

            private int _age;
            public int Age
            {
                get => _age;
                set { _age = value; OnPropertyChanged(); }
            }

            //Calculatin win/loss/draw percentages for barchart display
            private int _wins;
            public int Wins
            {
                get => _wins;
                set { _wins = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalGames)); OnPropertyChanged(nameof(WinPercent)); OnPropertyChanged(nameof(DrawPercent)); OnPropertyChanged(nameof(LossPercent)); OnPropertyChanged(nameof(WinCorner)); OnPropertyChanged(nameof(DrawCorner)); OnPropertyChanged(nameof(LossCorner)); }
            }

            private int _draws;
            public int Draws
            {
                get => _draws;
                set { _draws = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalGames)); OnPropertyChanged(nameof(WinPercent)); OnPropertyChanged(nameof(DrawPercent)); OnPropertyChanged(nameof(LossPercent)); OnPropertyChanged(nameof(WinCorner)); OnPropertyChanged(nameof(DrawCorner)); OnPropertyChanged(nameof(LossCorner)); }
            }
            private int _losses;
            public int Losses
            {
                get => _losses;
                set { _losses = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalGames)); OnPropertyChanged(nameof(WinPercent)); OnPropertyChanged(nameof(LossPercent)); OnPropertyChanged(nameof(DrawPercent)); OnPropertyChanged(nameof(WinCorner)); OnPropertyChanged(nameof(DrawCorner)); OnPropertyChanged(nameof(LossCorner)); }
            }

            public int TotalGames => Wins + Draws + Losses;

            public double WinPercent => TotalGames == 0 ? 0 : (double)Wins / TotalGames * 100;
            public double DrawPercent => TotalGames == 0 ? 0 : (double)Draws / TotalGames * 100;
            public double LossPercent => TotalGames == 0 ? 0 : (double)Losses / TotalGames * 100;

            //Corner radius calculations based on win/loss/draw percentages to give the bar a rounded look
            public CornerRadius WinCorner
            {
                get
                {
                    bool hasWins = Wins > 0;
                    bool hasDraws = Draws > 0;
                    bool hasLosses = Losses > 0;

                    if (!hasWins) return new CornerRadius(0);

                    // Only wins
                    if (!hasDraws && !hasLosses)
                        return new CornerRadius(8);

                    // Wins + something else → only left side rounded
                    return new CornerRadius(8, 0, 0, 8);
                }
            }

            public CornerRadius DrawCorner
            {
                get
                {
                    bool hasWins = Wins > 0;
                    bool hasDraws = Draws > 0;
                    bool hasLosses = Losses > 0;

                    if (!hasDraws) return new CornerRadius(0);

                    // Only draws
                    if (!hasWins && !hasLosses)
                        return new CornerRadius(8);

                    // Draw is left edge
                    if (!hasWins)
                        return new CornerRadius(8, 0, 0, 8);

                    // Draw is right edge
                    if (!hasLosses)
                        return new CornerRadius(0, 8, 8, 0);

                    // Middle segment
                    return new CornerRadius(0);
                }
            }

            public CornerRadius LossCorner
            {
                get
                {
                    bool hasWins = Wins > 0;
                    bool hasDraws = Draws > 0;
                    bool hasLosses = Losses > 0;

                    if (!hasLosses) return new CornerRadius(0);

                    // Only losses
                    if (!hasWins && !hasDraws)
                        return new CornerRadius(8);

                    // Losses + something else → only right side rounded
                    return new CornerRadius(0, 8, 8, 0);
                }
            }
        }

        //Load user profile
        private void LoadProfile()
        {
            var games = new List<GameSummary>();

            string folder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"ChessGames/{currentUser}"
                );

            int wins = 0;
            int losses = 0;
            int draws = 0;
            int movesPlayed = 0;
            int captures = 0;
            int checks = 0;
            int castles = 0;
            int promotions = 0;
            int stalemates = 0;

            List<string> whiteOpenerMoves = new List<string>();
            List<string> blackOpenerMoves = new List<string>();

            //Loop though all of the games of a user and calcluate all the stats
            foreach (var file in System.IO.Directory.GetFiles(folder, "*.json"))
            {
                var json = System.IO.File.ReadAllText(file);
                var game = JsonConvert.DeserializeObject<MoveHistory.SavedGame>(json);

                if (game == null) return;


                bool? won = game.Result == null || game.Result.StartsWith("Draw") ? (bool?)null : game.Result.StartsWith(game.PlayerColor ?? "NULL");

                var summary = new GameSummary
                {
                    FilePath = file,
                    Won = won,
                    Result = game.Result,
                    PlayerScore = won == true ? "1" : won == false ? "0" : "1/2",
                    ResultSymbol = won == true ? "M17,13H13V17H11V13H7V11H11V7H13V11H17M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3Z" : won == false ? "M17,13H7V11H17M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3Z" : "M17,16V14H7V16H17M19,3A2,2 0 0,1 21,5V19A2,2 0 0,1 19,21H5C3.89,21 3,20.1 3,19V5C3,3.89 3.89,3 5,3H19M17,10V8H7V10H17Z",
                    Opponent = game.BotName,
                    OpponentScore = won == true ? "0" : won == false ? "1" : "1/2",
                    Difficulty = game.Difficulty,
                    MoveCount = game.Moves.Count,
                    Date = System.IO.Path.GetFileName(file).Substring(5, 10)
                };

                movesPlayed += game.Moves.Count;

                games.Add(summary);
                if (game.Result != null)
                {
                    if (game.Result[..4] == "Draw") draws++;
                    else if (game.Result[..5] == game.PlayerColor) wins++;
                    else losses++;

                    if (game.Result.Contains("Stalemate")) stalemates++;
                }

                if (game.Moves.Count > 0)
                {
                    var firstMove = game.Moves[0];

                    if (game.PlayerColor == blackTxt && firstMove?.Black != null)
                        blackOpenerMoves?.Add(firstMove.Black);

                    if (game.PlayerColor == whiteTxt && firstMove?.White != null)
                        whiteOpenerMoves?.Add(firstMove.White);
                }

                foreach (var move in game.Moves)
                {
                    if (move.Black != null)
                    {
                        if (move.Black.Contains("x") && game.PlayerColor == blackTxt) captures++;
                        if (move.Black.Contains("+") && game.PlayerColor == blackTxt) checks++;
                        if (move.Black.Contains("O-O") && game.PlayerColor == blackTxt) castles++;
                        if (move.Black.Contains("=") && game.PlayerColor == blackTxt) promotions++;
                    }
                    if (move.White != null)
                    {
                        if (move.White.Contains("x") && game.PlayerColor == whiteTxt) captures++;
                        if (move.White.Contains("+") && game.PlayerColor == whiteTxt) checks++;
                        if (move.White.Contains("O-O") && game.PlayerColor == whiteTxt) castles++;
                        if (move.White.Contains("=") && game.PlayerColor == whiteTxt) promotions++;
                    }
                }
            }

            GamesList.ItemsSource = games;
            profileStats.Wins = wins;
            profileStats.Draws = draws;
            profileStats.Losses = losses;
            profileStats.MovesPlayedCount = movesPlayed;
            profileStats.CaptureCount = captures;
            profileStats.CheckCount = checks;
            profileStats.CastleCount = castles;
            profileStats.PromotionCount = promotions;
            profileStats.StalemateCount = stalemates;
            profileStats.BlackOpener = Mode(blackOpenerMoves) ?? "e5";
            profileStats.WhiteOpener = Mode(whiteOpenerMoves) ?? "e4";

            ProfileView.DataContext = profileStats;
        }

        //Mode function to find the favourite opener moves
        static string? Mode(IEnumerable<string>? values)
        {
            if (values == null) return null;

            var counts = new Dictionary<string, int>();

            foreach (var v in values)
            {
                if (v == null) continue;

                counts[v] = counts.TryGetValue(v, out var c) ? c + 1 : 1;
            }

            return counts.Count == 0
                ? null
                : counts.MaxBy(kv => kv.Value).Key;
        }

        //Profile click handlers
        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            MenuView.Visibility = Visibility.Collapsed;
            ProfileView.Visibility = Visibility.Visible;

            LoadProfile();
        }

        private void BackFromProfile_Click(object sender, RoutedEventArgs e)
        {
            ProfileView.Visibility = Visibility.Collapsed;
            MenuView.Visibility = Visibility.Visible;
        }

        //Game click handler to load the game
        private void Game_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GameSummary game)
            {
                LoadGameFromFile(game.FilePath);
            }
        }

        //Login/Logout/Signup click handlers
        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text;
            var password = PasswordBox.Password;

            var response = await http.PostAsJsonAsync("api/auth/login", new
            {
                Username = username,
                Theme = "GreenTheme",
                PieceSet = "caliente",
                PasswordHash = password
            });

            if (response.IsSuccessStatusCode)
            {
                var userData = await response.Content.ReadFromJsonAsync<User>();

                currentUser = username;

                //Change UI view to menu
                AuthView.Visibility = Visibility.Collapsed;
                MenuView.Visibility = Visibility.Visible;

                profileStats.Username = username;

                chessBoard = new Board(ChessBoardGrid, ArrowLayer, DragLayer, MovesTable, PromotionLayer, EndScreen);

                //Load users last unfinished game
                LoadLastUnfinishedGame();

                MovesTable.ItemsSource = MoveHistory.MoveList;
                chessBoard.BuildBoard();

                //Apply user preferrences
                if (userData != null)
                {
                    PieceSetCombo.SelectedItem = userData.PieceSet;
                    if (userData.PieceSet != null)
                        chessBoard?.SetPieceSet(userData.PieceSet);
                    if (userData.Theme != null)
                    {
                        ThemeCombo.SelectedItem = userData.Theme;
                        ThemeManager.LoadTheme(userData.Theme);
                    }
                }
                //Update board and sync games
                chessBoard?.UpdateBoard(chessBoard.GetGame());

                await SyncGames();
            }
            else
            {
                AuthStatus.Text = "Username or Password is Incorrect";
            }
        }
    
        private async void Logout_Click(object sender, RoutedEventArgs e)
        {
            //Reset UI and credentials
            PasswordBox.Password = "";
            UsernameBox.Text = "";
            AuthView.Visibility = Visibility.Visible;
            MenuView.Visibility = Visibility.Collapsed;
        }

        private async void Signup_Click(object sender, RoutedEventArgs e)
        {
            //Post the new user - with default preferences
            var response = await http.PostAsJsonAsync("api/auth/signup", new
            {
                Username = UsernameBox.Text,
                PieceSet = "caliente",
                Theme = "GreenTheme",
                PasswordHash = PasswordBox.Password
            });

            AuthStatus.Text = response.IsSuccessStatusCode
                ? "User created! You can log in."
                : "Signup failed";
        }

        //Function to update user preferences
        private async Task UpdatePreferences()
        {
            var user = currentUser;

            //Post the new preferences
            await http.PutAsJsonAsync(
                $"api/auth/{user}/preferences",
                new
                {
                    Username = user,
                    PieceSet = PieceSetCombo.SelectedItem?.ToString(),
                    Theme = ThemeCombo.SelectedItem?.ToString(),
                    PasswordHash = "POTATO"
                });
        }

        //Function to sync users games folder with the DB
        private async Task SyncGames()
        {
            if (currentUser == null) return;

            //Create user folder if not present
            string folder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"ChessGames/{currentUser}"
            );

            System.IO.Directory.CreateDirectory(folder);

            //Upload local games
            foreach (var file in System.IO.Directory.GetFiles(folder, "*.json"))
            {
                var json = await System.IO.File.ReadAllTextAsync(file);

                var game = new
                {
                    Username = currentUser,
                    JsonData = json,
                    ClientId = System.IO.Path.GetFileName(file),
                    LastModified = System.IO.File.GetLastWriteTimeUtc(file)
                };

                await http.PostAsJsonAsync("api/game/save", game);
            }

            //Download games from DB
            var response = await http.GetAsync($"api/game/sync/{currentUser}");

            if (!response.IsSuccessStatusCode) return;

            var serverGames = await response.Content.ReadFromJsonAsync<List<ServerGame>>();

            if (serverGames != null)
                foreach (var g in serverGames)
                {
                    if (g.ClientId == null) return;

                    string path = System.IO.Path.Combine(folder, g.ClientId);

                    var serverTime = g.LastModified;

                    if (!System.IO.File.Exists(path))
                    {
                        await System.IO.File.WriteAllTextAsync(path, g.JsonData);
                    }
                    else
                    {
                        var localTime = System.IO.File.GetLastWriteTimeUtc(path);

                        if (serverTime > localTime)
                        {
                            await System.IO.File.WriteAllTextAsync(path, g.JsonData);
                        }
                    }
                }
        }
    }
    

    //Percent to Grid length Conerter for win% barchart
    public class PercentToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = (double)value;
            return new GridLength(percent, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

}