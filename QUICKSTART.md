# UNO Game - Quick Start Guide

## ✅ Project Complete!

Your multiplayer UNO game is fully functional and ready to play!

## 🚀 Running the Game

The server is currently running at: **http://localhost:5000**

### To start the game:

1. Open your browser and go to http://localhost:5000
2. Create a new game room
3. Open another browser window (or incognito mode)
4. Join the same room using the game URL
5. Start playing!

### To stop the server:

Press `Ctrl+C` in the terminal

### To restart the server:

```bash
dotnet run
```

## 📂 Project Files Overview

### Backend (C#)

- **Program.cs** - Main application configuration with SignalR setup
- **Models/** - Card, Player, GameRoom classes with game logic
- **Services/GameService.cs** - Core game engine (deck, shuffle, validation)
- **Hubs/GameHub.cs** - SignalR hub for real-time multiplayer
- **Controllers/RoomsController.cs** - REST API for lobby

### Frontend

- **Pages/Index.cshtml** - Lobby page (create/join games)
- **Pages/Game.cshtml** - Game table with player hands and cards
- **wwwroot/js/lobby.js** - Lobby functionality
- **wwwroot/js/game.js** - Game client with SignalR integration
- **wwwroot/css/site.css** - Complete styling for the game

### Assets

- **wwwroot/cards/** - 53 UNO card images (all colors, special cards, wild cards)

## 🎮 Game Features Implemented

### ✅ Full UNO Rules

- [x] 108-card standard deck
- [x] Number cards (0-9) in all colors
- [x] Skip, Reverse, Draw Two special cards
- [x] Wild and Wild Draw Four cards
- [x] Color matching and number matching
- [x] Wild Draw Four restriction (can't play if you have matching color)
- [x] Automatic turn rotation
- [x] Direction reversal
- [x] Card drawing from deck
- [x] Deck reshuffling when empty
- [x] Win condition detection

### ✅ Multiplayer Features

- [x] 2-6 players per game
- [x] Real-time synchronization with SignalR
- [x] Server-authoritative game logic (no cheating!)
- [x] Player join/leave handling
- [x] Graceful disconnect handling
- [x] Private card hands (players only see their own cards)
- [x] Public game state (everyone sees top card, player counts)

### ✅ UI/UX

- [x] Modern, colorful design matching UNO's theme
- [x] Responsive layout (works on mobile and desktop)
- [x] Card hover animations
- [x] Turn indicator highlighting
- [x] Wild color selection modal
- [x] Game over modal
- [x] Real-time notifications
- [x] Visual feedback for current player

### ✅ Technical Excellence

- [x] Clean architecture (Models, Services, Hubs, Controllers)
- [x] Async/await throughout
- [x] Thread-safe game state management
- [x] Comprehensive error handling
- [x] Well-commented code
- [x] No external UI frameworks (vanilla JS)
- [x] In-memory state (perfect for portfolio demo)

## 🎯 Testing Checklist

Try these scenarios:

- [ ] Create a room and join it from another browser
- [ ] Play number cards matching color and number
- [ ] Play Skip card (next player should be skipped)
- [ ] Play Reverse card (direction should change)
- [ ] Play Draw Two (next player draws 2 cards)
- [ ] Play Wild card and select a color
- [ ] Play Wild Draw Four (ensure it validates no matching color)
- [ ] Draw a card when you can't play
- [ ] Win by playing all your cards
- [ ] Player disconnection (close a browser tab)

## 🏆 What Makes This Portfolio-Ready

1. **Professional Code Structure** - Clear separation of concerns
2. **Real-Time Technology** - Shows mastery of SignalR
3. **Game Logic** - Complex state management and validation
4. **Security** - Server-authoritative design prevents cheating
5. **UX Design** - Polished, intuitive interface
6. **Documentation** - Comprehensive README and code comments
7. **Best Practices** - Async patterns, SOLID principles
8. **No Databases** - Easy to demo without setup

## 📝 Code Highlights to Mention in Interviews

- **SignalR Hub Pattern**: Real-time bidirectional communication
- **Fisher-Yates Shuffle**: Proper random card shuffling
- **Server Validation**: All moves validated server-side
- **State Management**: ConcurrentDictionary for thread-safety
- **Card Validation Logic**: CanPlayOn() method with complex rules
- **Event-Driven Architecture**: SignalR events for game state updates
- **Clean Code**: Well-organized, maintainable, commented

## 🎨 Customization Ideas

Want to add your personal touch?

- Change color scheme in site.css
- Add sound effects
- Add game chat
- Add player avatars
- Track game statistics
- Add game rules page

## 🐛 Known Limitations (by design for simplicity)

- In-memory storage (games lost on server restart)
- No authentication/user accounts
- No persistent game history
- No AI players
- Single game server (not distributed)

These are intentional for a portfolio project - easy to demo and explain!

---

**Congratulations! You now have a complete, portfolio-ready multiplayer UNO game! 🎉**

Enjoy playing and showcasing your work!
