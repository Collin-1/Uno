# 🎮 UNO Online - Multiplayer Card Game

A fully functional multiplayer UNO card game built with ASP.NET Core, SignalR, and Razor Pages. Play UNO with your friends online in real-time!

## 🎯 Features

- **Real-time Multiplayer**: Play with 2-6 players using SignalR for instant updates
- **Full UNO Rules**: Complete implementation of standard UNO gameplay
  - Number cards (0-9) in four colors
  - Skip, Reverse, and Draw Two cards
  - Wild and Wild Draw Four cards
  - Proper card validation and game flow
- **Lobby System**: Create and join game rooms
- **Clean UI**: Modern, responsive design with smooth animations
- **Server-Authoritative**: All game logic validated server-side to prevent cheating

## 🧱 Tech Stack

- **ASP.NET Core 8.0** - Web framework
- **SignalR** - Real-time communication
- **Razor Pages** - Server-side rendering
- **Vanilla JavaScript** - Client-side interactivity
- **CSS3** - Modern styling with gradients and animations

## 📁 Project Structure

```
/UnoGame
├── Controllers/
│   └── RoomsController.cs      # API endpoints for lobby
├── Hubs/
│   └── GameHub.cs              # SignalR hub for real-time game events
├── Models/
│   ├── Card.cs                 # Card model with validation logic
│   ├── Player.cs               # Player model
│   └── GameRoom.cs             # Game room and state management
├── Services/
│   └── GameService.cs          # Core game logic (deck, validation, etc.)
├── Pages/
│   ├── Index.cshtml            # Lobby page
│   ├── Game.cshtml             # Game table page
│   └── Shared/
│       └── _Layout.cshtml      # Layout template
├── wwwroot/
│   ├── cards/                  # UNO card image assets (53 files)
│   ├── css/
│   │   └── site.css            # Game styling
│   └── js/
│       ├── lobby.js            # Lobby functionality
│       └── game.js             # Game client with SignalR
└── Program.cs                  # Application entry point
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

### Running the Game

1. **Clone or navigate to the project directory**

2. **Build the project**

   ```bash
   dotnet build
   ```

3. **Run the application**

   ```bash
   dotnet run
   ```

4. **Open in browser**
   - Navigate to `https://localhost:5001` (or the URL shown in the console)
   - Create a new game or join an existing one
   - Share the game URL with friends to play together

## 🎮 How to Play

### Creating a Game

1. On the lobby page, enter:
   - Room name
   - Your player name
   - Max players (2-6)
2. Click "Create Room"
3. Share the URL with friends
4. Wait for players to join, then click "Start Game"

### Playing

- **Your Turn**:

  - Click a card to play it (must match color or number/symbol)
  - Click the deck to draw a card if you can't play
  - Wild cards will prompt you to choose a color

- **Card Rules**:

  - **Number cards**: Match color or number
  - **Skip**: Skips the next player's turn
  - **Reverse**: Changes direction of play
  - **Draw Two**: Next player draws 2 cards and loses their turn
  - **Wild**: Can be played anytime, choose a color
  - **Wild Draw Four**: Can only be played when you have no matching color, next player draws 4

- **Winning**: First player to play all their cards wins!

## 🔌 SignalR Events

### Client → Server

- `JoinRoom(roomId, playerName)` - Join a game room
- `StartGame(roomId)` - Start the game
- `PlayCard(roomId, cardId, wildColor)` - Play a card
- `DrawCard(roomId)` - Draw a card from the deck
- `LeaveRoom(roomId)` - Leave the game

### Server → Client

- `GameStateUpdated(gameState)` - Full game state sync
- `UpdateHand(playerInfo)` - Player's private hand
- `CardPlayed(playerName, card)` - Notification of card played
- `PlayerJoined/PlayerLeft` - Player status updates
- `GameStarted` - Game has begun
- `GameOver(winnerName)` - Game ended
- `Error(message)` - Error notifications

## 🏗️ Architecture Highlights

### Server-Authoritative Design

- All game logic runs server-side in `GameService`
- Card plays are validated before being applied
- Players only receive their own cards
- Prevents cheating and ensures fair play

### State Management

- In-memory storage using `ConcurrentDictionary`
- Thread-safe operations for concurrent players
- Automatic cleanup when players disconnect

### Game Logic

- **Deck Creation**: Standard 108-card UNO deck
- **Shuffling**: Fisher-Yates algorithm
- **Validation**: Server validates every move
- **Card Effects**: Automatic handling of Skip, Reverse, Draw cards
- **Reshuffle**: Discard pile reshuffled when deck runs out

## 🎨 Card Assets

The game includes 53 professionally designed card images:

- 40 number cards (4 colors × 10 numbers)
- 8 Skip cards (4 colors × 2)
- 8 Reverse cards (4 colors × 2)
- 8 Draw Two cards (4 colors × 2)
- 4 Wild cards
- 4 Wild Draw Four cards
- 1 Card back (deck)

## 🔒 Security Features

- Server-side validation of all moves
- Connection-based player identification
- No client-side game state manipulation
- Proper turn enforcement
- Wild Draw Four restriction (can't play if you have matching color)

## 📱 Responsive Design

- Desktop-optimized layout
- Mobile-friendly card display
- Touch-friendly buttons and cards
- Adaptive grid layouts

## 🧪 Testing the Game

1. Open two browser windows (or use different browsers)
2. Create a room in the first window
3. Copy the URL and open it in the second window
4. Join with a different player name
5. Start the game and play!

## 🎯 Future Enhancements (Optional)

- [ ] Persistent storage (database)
- [ ] User accounts and authentication
- [ ] Game history and statistics
- [ ] Sound effects and music
- [ ] Custom game rules
- [ ] Tournament mode
- [ ] Chat system
- [ ] Spectator mode

## 📝 License

This project is for educational and portfolio purposes.

## 🙏 Acknowledgments

- UNO® is a registered trademark of Mattel, Inc.
- Card assets provided for educational use
- Built as a portfolio project to demonstrate ASP.NET Core and real-time web development skills

---

**Made with ❤️ using ASP.NET Core and SignalR**
