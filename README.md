# ChessAI - Desktop Chess Application with AI Opponents

## Overview

ChessAI is a **desktop chess application** built with **C# WPF** that allows users to play chess against multiple AI opponents of varying strength. The application includes game analysis tools, customizable themes, cloud-based account management, and online game storage.

The project combines a modern desktop interface with custom-built chess engines and a cloud-hosted ASP.NET Web API to provide an engaging chess experience for casual players and enthusiasts alike.

---

## Features

- **Play Against Multiple Chess Bots**
  - RandomBot for beginner-level gameplay (plays random moves)
  - Quasar v0 (simple position evaluation)
  - Quasar v0.1 (position evaluation, piece activity, alpha-beta pruning)
- **Interactive Chess Board**
  - Drag-and-drop piece movement
  - Move validation
  - Draw arrows and highlight squares on the board
- **Move History**
  - Complete move list using algebraic notation
  - Navigate through previous moves
- **User Accounts**
  - Register and log in securely
  - Store user preferences
- **Cloud Save System**
  - Save games to a MongoDB database
  - Load previously saved games
- **Customisation**
  - Multiple board themes
  - Multiple chess piece sets
- **Responsive Desktop UI**
  - Clean WPF interface with intuitive controls

---

## Technology Stack

**Frontend:**

- C#
- WPF (.NET 10)
- XAML

**Backend:**

- ASP.NET Core Web API
- MongoDB

**Libraries:**

- ChessDotNet (chess rules and move generation)
- Newtonsoft.Json
- MongoDB.Driver
- xUnit (unit testing)

**Hosting:**

- **Backend API:** Render
- **Database:** MongoDB Atlas
- **Source Control & CI/CD:** GitHub Actions

---

## How It Works

1. **Game Initialization**
   - The application creates a chess game using ChessDotNet and initializes the selected AI opponent.

2. **Player Interaction**
   - Players move pieces using the graphical chess board.
   - Legal moves are validated before being executed.

3. **AI Decision Making**
   - Depending on the selected bot, the AI evaluates legal moves using different search algorithms.
   - Stronger bots use minimax with alpha-beta pruning and board evaluation heuristics.

4. **Cloud Integration**
   - User accounts and saved games are managed through an ASP.NET Core Web API.
   - Data is securely stored in MongoDB Atlas.

5. **Customization**
   - Users can personalise the application through different board themes and chess piece sets.

---

## Installation & Setup

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/ChessAI.git
cd ChessAI
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure the Backend

Create an `appsettings.json` or environment variable containing your MongoDB connection string.

```json
{
  "MongoDb": {
    "ConnectionString": "your_mongodb_connection_string"
  }
}
```

### 4. Configure the Desktop Application

Update the API URL inside the project:

```csharp
BaseAddress = new Uri("https://your-render-api.onrender.com/")
```

or

```csharp
BaseAddress = new Uri("https://localhost:7062/")
```

for local development.

### 5. Run the API

```bash
cd src/ChessAIWebAPI
dotnet run
```

### 6. Run the Desktop Application

```bash
cd src/ChessAIApp
dotnet run
```

### 7. Access the Application

Launch the WPF application and begin playing against any available AI opponent.

---

## Images

Below are screenshots of the main sections of the application.

### Home Screen
<img width="1891" height="956" alt="image" src="https://github.com/user-attachments/assets/6b1eff1c-af1c-4fb2-a080-8dcf3a863ee7" />

### AI Opponent Selection
<img width="1917" height="992" alt="image" src="https://github.com/user-attachments/assets/136acca0-1944-4159-959f-3d6d1c7bd4df" />

### Chess Board & Move History
<img width="1915" height="980" alt="image" src="https://github.com/user-attachments/assets/f84a57f0-bbf1-4d67-8ea8-aba5170cb6a7" />

### Profile Page
<img width="1917" height="977" alt="image" src="https://github.com/user-attachments/assets/c308a3d7-09f1-455d-8370-5321fbe6fd84" />

### Signup / Login Screen
<img width="1896" height="966" alt="image" src="https://github.com/user-attachments/assets/eded4044-5443-4ca9-864f-55e5138575fd" />

### Themes & Piece Sets
<img width="1902" height="983" alt="image" src="https://github.com/user-attachments/assets/7c27c01f-aafd-43dd-9933-8fcfbd0c0d4e" />
<img width="1913" height="976" alt="image" src="https://github.com/user-attachments/assets/db6c3d45-6f8a-4962-b566-087db2b4ce4c" />
<img width="1911" height="976" alt="image" src="https://github.com/user-attachments/assets/90db0f67-e68e-4927-ad34-385d8021b436" />

---

## Testing

The project includes automated unit tests covering:

- Chess AI evaluation
- Overlay rendering
- Highlight management
- Arrow drawing
- Utility functions

Run the tests using:

```bash
dotnet test
```

GitHub Actions automatically builds the application, runs all unit tests, publishes the desktop application, deploys the Web API, and creates GitHub Releases for tagged versions.

---

## Future Enhancements

- Stronger chess engine with iterative deepening and transposition tables
- Opening book support
- Endgame tablebases
- Multiplayer gameplay
- Chess clock and time controls
- PGN import/export
- Game analysis using Stockfish
- Elo rating system
- Puzzle and training mode
- Cross-platform support using .NET MAUI
