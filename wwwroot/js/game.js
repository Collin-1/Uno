// Game functionality with SignalR
const urlParams = new URLSearchParams(window.location.search);
const roomId = urlParams.get("room");
const playerName = sessionStorage.getItem("playerName") || "Player";

let connection;
let currentHand = [];
let isMyTurn = false;
let pendingWildCard = null;
let voiceChat = null;

// Initialize SignalR connection
async function initializeGame() {
  connection = new signalR.HubConnectionBuilder()
    .withUrl("/gameHub")
    .withAutomaticReconnect()
    .build();

  // Set up event handlers
  setupSignalRHandlers();

  try {
    await connection.start();
    console.log("Connected to game hub");

    // Join the room
    await connection.invoke("JoinRoom", roomId, playerName);
  } catch (error) {
    console.error("Error connecting:", error);
    alert("Failed to connect to game server");
  }
}

function setupSignalRHandlers() {
  // Successfully joined room
  connection.on("JoinedRoom", (roomId, playerName) => {
    console.log(`Joined room ${roomId} as ${playerName}`);
    document.getElementById("yourName").textContent = playerName;
  });

  // Game state updated
  connection.on("GameStateUpdated", (gameState) => {
    updateGameState(gameState);
    // Voice chat initialization is manual via button click
  });

  // Player's hand updated
  connection.on("UpdateHand", (playerInfo) => {
    updatePlayerHand(playerInfo.hand);
  });

  // Card was played
  connection.on("CardPlayed", (playerName, card) => {
    console.log(`${playerName} played a card`);
    updateTopCard(card);
  });

  // Game started
  connection.on("GameStarted", (gameState) => {
    console.log("Game started!", gameState);
    showNotification("Game started!");
    document.getElementById("startGameSection").style.display = "none";
    updateGameState(gameState);
    // Don't auto-initialize voice chat - let user click the button
  });

  // Cards were drawn
  connection.on("CardsDrawn", (cards) => {
    console.log("Drew cards:", cards);
    // Cards will be added via UpdateHand event
  });

  // Player drew cards
  connection.on("PlayerDrew", (playerName, count) => {
    showNotification(`${playerName} drew ${count} card(s)`);
  });

  // Player joined
  connection.on("PlayerJoined", (playerName, playerCount) => {
    showNotification(`${playerName} joined the game`);
  });

  // Player left
  connection.on("PlayerLeft", (playerName) => {
    showNotification(`${playerName} left the game`);
  });

  // Game over
  connection.on("GameOver", (winnerName) => {
    showGameOver(winnerName);
  });

  // Error messages
  connection.on("Error", (message) => {
    showNotification(message, true);
  });

  // Wild color was chosen
  connection.on("WildColorChosen", (playerName, color) => {
    showWildColorNotification(playerName, color);
  });
}

function updateGameState(gameState) {
  console.log("Game state:", gameState);
  console.log("Game Status:", gameState.status);
  console.log("Players:", gameState.players);

  // Store globally for voice chat
  window.currentGameState = gameState;

  // Update room name
  document.getElementById("roomName").textContent = gameState.roomName;

  // Update game status
  const statusText =
    gameState.status === "InProgress"
      ? `Playing - ${gameState.direction}`
      : gameState.status;
  document.getElementById("gameStatus").textContent = statusText;

  // Update top card
  if (gameState.topCard) {
    updateTopCard(gameState.topCard);
  }

  // Update deck count
  document.getElementById("deckCount").textContent = gameState.deckCount;

  // Update current turn indicator
  const currentPlayerName = gameState.currentPlayerName || "";
  isMyTurn = currentPlayerName === playerName;

  const turnIndicator = document.getElementById("currentTurnIndicator");
  if (gameState.status === "InProgress") {
    turnIndicator.textContent = isMyTurn
      ? "Your Turn!"
      : `${currentPlayerName}'s Turn`;
    turnIndicator.className = isMyTurn
      ? "turn-indicator my-turn"
      : "turn-indicator";
  } else {
    turnIndicator.textContent = "Waiting for game to start...";
    turnIndicator.className = "turn-indicator";
  }

  // Update other players
  updateOtherPlayers(gameState.players);

  // Show/hide start button based on game status
  const startGameSection = document.getElementById("startGameSection");
  console.log("Start game section element:", startGameSection);
  console.log("Should show start button?", gameState.status === "Waiting");

  if (gameState.status === "Waiting") {
    startGameSection.style.display = "block";
    console.log("Showing start button");
  } else {
    startGameSection.style.display = "none";
    console.log("Hiding start button");
  }

  // Enable/disable draw pile based on turn
  updateDrawPileState();
}

function updateTopCard(card) {
  const topCardImg = document.getElementById("topCard");
  if (card && card.imageFile) {
    topCardImg.src = `/cards/${card.imageFile}`;
    topCardImg.alt = `${card.color} ${card.type}`;
  }
}

function updatePlayerHand(hand) {
  currentHand = hand;
  const handContainer = document.getElementById("playerHand");
  handContainer.innerHTML = "";

  document.getElementById("yourCardCount").textContent =
    `${hand.length} card(s)`;

  hand.forEach((card) => {
    const cardElement = createCardElement(card);
    handContainer.appendChild(cardElement);
  });

  updateDrawPileState();
}

function createCardElement(card) {
  const cardDiv = document.createElement("div");
  cardDiv.className = "card";
  cardDiv.dataset.cardId = card.id;

  const cardImg = document.createElement("img");
  cardImg.src = `/cards/${card.imageFile}`;
  cardImg.alt = `${card.color} ${card.type}`;

  cardDiv.appendChild(cardImg);

  // Add click handler to play card
  cardDiv.addEventListener("click", () => playCard(card));

  return cardDiv;
}

function updateOtherPlayers(players) {
  const container = document.getElementById("otherPlayers");
  container.innerHTML = "";

  players.forEach((player) => {
    if (player.name !== playerName) {
      const playerDiv = document.createElement("div");
      playerDiv.className = "other-player";
      if (player.isCurrentPlayer) {
        playerDiv.classList.add("active");
      }

      playerDiv.innerHTML = `
        <div class="player-name">${escapeHtml(player.name)}</div>
        <div class="player-cards">${player.cardCount} 🃏</div>
            `;

      container.appendChild(playerDiv);
    }
  });
}

async function playCard(card) {
  if (!isMyTurn) {
    showNotification("It's not your turn!", true);
    return;
  }

  // If it's a wild card, show color picker.
  // Some card objects may carry type as string or may use imageFile naming — check both robustly.
  const isWildByType = typeof card.type === "string" && /wild/i.test(card.type);
  const isWildByImage =
    typeof card.imageFile === "string" && /wild/i.test(card.imageFile);
  if (isWildByType || isWildByImage) {
    pendingWildCard = card;
    showWildColorModal();
    return;
  }

  // Play regular card
  try {
    await connection.invoke("PlayCard", roomId, card.id, null);
  } catch (error) {
    console.error("Error playing card:", error);
    showNotification("Failed to play card", true);
  }
}

async function playWildCard(color) {
  if (!pendingWildCard) return;

  try {
    await connection.invoke("PlayCard", roomId, pendingWildCard.id, color);
    pendingWildCard = null;
    hideWildColorModal();
  } catch (error) {
    console.error("Error playing wild card:", error);
    showNotification("Failed to play card", true);
  }
}

async function drawCard() {
  if (!isMyTurn) {
    showNotification("It's not your turn!", true);
    return;
  }

  try {
    await connection.invoke("DrawCard", roomId);
  } catch (error) {
    console.error("Error drawing card:", error);
    showNotification("Failed to draw card", true);
  }
}

async function startGame() {
  try {
    await connection.invoke("StartGame", roomId);
  } catch (error) {
    console.error("Error starting game:", error);
    showNotification("Failed to start game", true);
  }
}

async function leaveGame() {
  try {
    await connection.invoke("LeaveRoom", roomId);
    window.location.href = "/";
  } catch (error) {
    console.error("Error leaving game:", error);
    window.location.href = "/";
  }
}

function updateDrawPileState() {
  const drawPile = document.getElementById("drawPile");
  if (isMyTurn) {
    drawPile.classList.add("clickable");
    drawPile.onclick = drawCard;
  } else {
    drawPile.classList.remove("clickable");
    drawPile.onclick = null;
  }
}

function showWildColorModal() {
  const modal = document.getElementById("wildColorModal");
  modal.style.display = "flex";
}

function hideWildColorModal() {
  const modal = document.getElementById("wildColorModal");
  modal.style.display = "none";
  pendingWildCard = null;
}

function showGameOver(winnerName) {
  const modal = document.getElementById("gameOverModal");
  const message = document.getElementById("winnerMessage");
  message.textContent = winnerName.includes("wins")
    ? winnerName
    : `${winnerName} wins!`;
  modal.style.display = "flex";
}

function showNotification(message, isError = false) {
  // Simple notification system
  const notification = document.createElement("div");
  notification.className = isError ? "notification error" : "notification";
  notification.textContent = message;
  document.body.appendChild(notification);

  setTimeout(() => {
    notification.classList.add("show");
  }, 10);

  setTimeout(() => {
    notification.classList.remove("show");
    setTimeout(() => notification.remove(), 300);
  }, 5000);
}

function showWildColorNotification(playerName, color) {
  const notification = document.createElement("div");
  notification.className = "notification wild-color-notification";

  const colorIndicator = document.createElement("span");
  colorIndicator.className = `wild-color-indicator ${color.toLowerCase()}`;
  colorIndicator.textContent = "●";

  const text = document.createElement("span");
  text.textContent = ` ${playerName} chose ${color}!`;

  notification.appendChild(colorIndicator);
  notification.appendChild(text);
  document.body.appendChild(notification);

  setTimeout(() => {
    notification.classList.add("show");
  }, 10);

  setTimeout(() => {
    notification.classList.remove("show");
    setTimeout(() => notification.remove(), 300);
  }, 5000);
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

// Event listeners
document.getElementById("startGameBtn").addEventListener("click", startGame);
document.getElementById("leaveGameBtn").addEventListener("click", leaveGame);
document.getElementById("backToLobbyBtn").addEventListener("click", () => {
  window.location.href = "/";
});

// Wild color selection
document.querySelectorAll(".color-btn").forEach((btn) => {
  btn.addEventListener("click", () => {
    const color = btn.dataset.color;
    playWildCard(color);
  });
});

// Close modals when clicking outside
window.addEventListener("click", (e) => {
  const wildModal = document.getElementById("wildColorModal");
  if (e.target === wildModal) {
    hideWildColorModal();
  }
});

// Voice chat functions
async function initializeVoiceChat(gameState) {
  if (!voiceChat) {
    console.log("Initializing voice chat...", gameState);
    try {
      voiceChat = new VoiceChat(connection, roomId);
      const success = await voiceChat.initialize();
      console.log("Voice chat initialization:", success ? "success" : "failed");

      if (success && gameState.players) {
        const playerConnectionIds = gameState.players.map(
          (p) => p.connectionId,
        );
        console.log("Connecting to peers:", playerConnectionIds);
        await voiceChat.connectToAllPeers(playerConnectionIds);
      } else if (!success) {
        // If initialization failed, clear voiceChat
        voiceChat = null;
      }
    } catch (error) {
      console.error("Error initializing voice chat:", error);
      voiceChat = null;
    }
  } else {
    console.log("Voice chat already initialized");
  }
}

async function toggleMicrophone() {
  console.log("Toggle microphone clicked. VoiceChat:", voiceChat);

  if (!voiceChat) {
    // First click - initialize voice chat
    console.log("Initializing voice chat from button click...");
    const micBtn = document.getElementById("mic-toggle");
    if (micBtn) {
      micBtn.textContent = "⏳ Requesting...";
      micBtn.disabled = true;
    }

    try {
      voiceChat = new VoiceChat(connection, roomId);
      const success = await voiceChat.initialize();

      if (success) {
        // Get current game state to connect to peers
        const gameState = await getGameState();
        if (gameState && gameState.players) {
          const playerConnectionIds = gameState.players.map(
            (p) => p.connectionId,
          );
          await voiceChat.connectToAllPeers(playerConnectionIds);
        }
        showNotification("Voice chat enabled!");
      } else {
        voiceChat = null;
      }
    } catch (error) {
      console.error("Error initializing voice chat:", error);
      voiceChat = null;
    }

    if (micBtn) {
      micBtn.disabled = false;
      if (!voiceChat) {
        micBtn.textContent = "🎤 Enable Voice";
      }
    }
  } else {
    // Already initialized - just toggle mute
    voiceChat.toggleMute();
  }
}

// Helper to get current game state
function getGameState() {
  return new Promise((resolve) => {
    // Store the current game state in a global variable when updated
    resolve(window.currentGameState || null);
  });
}

// Update leave game to disconnect voice
const originalBackToLobby = document.getElementById("backToLobbyBtn")?.onclick;
if (document.getElementById("backToLobbyBtn")) {
  document.getElementById("backToLobbyBtn").onclick = function () {
    if (voiceChat) {
      voiceChat.disconnectAll();
      voiceChat = null;
    }
    if (originalBackToLobby) originalBackToLobby();
  };
}

// Initialize when page loads
initializeGame();
