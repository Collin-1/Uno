// Lobby functionality - creating and joining games
(async function () {
    // Create room
    document.getElementById('createRoomBtn').addEventListener('click', async () => {
        const roomName = document.getElementById('roomName').value.trim();
        const playerName = document.getElementById('playerName').value.trim();
        const maxPlayers = document.getElementById('maxPlayers').value;

        if (!roomName || !playerName) {
            alert('Please enter both room name and your name');
            return;
        }

        try {
            const response = await fetch('/api/rooms/create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ roomName, maxPlayers: parseInt(maxPlayers) })
            });

            const data = await response.json();

            if (data.roomId) {
                // Store player name and redirect to game
                sessionStorage.setItem('playerName', playerName);
                window.location.href = `/Game?room=${data.roomId}`;
            }
        } catch (error) {
            console.error('Error creating room:', error);
            alert('Failed to create room');
        }
    });

    // Refresh available rooms
    document.getElementById('refreshRoomsBtn').addEventListener('click', loadAvailableRooms);

    // Load rooms on page load
    await loadAvailableRooms();

    async function loadAvailableRooms() {
        const roomsList = document.getElementById('availableRooms');
        roomsList.innerHTML = '<p class="loading">Loading...</p>';

        try {
            const response = await fetch('/api/rooms');
            const rooms = await response.json();

            if (rooms.length === 0) {
                roomsList.innerHTML = '<p class="no-rooms">No available rooms. Create one!</p>';
                return;
            }

            roomsList.innerHTML = '';
            rooms.forEach(room => {
                const roomElement = document.createElement('div');
                roomElement.className = 'room-item';
                roomElement.innerHTML = `
                    <div class="room-info">
                        <h3>${escapeHtml(room.roomName)}</h3>
                        <p>${room.playerCount} / ${room.maxPlayers} players</p>
                    </div>
                    <button class="btn btn-primary join-btn" data-room-id="${room.roomId}">Join</button>
                `;

                const joinBtn = roomElement.querySelector('.join-btn');
                joinBtn.addEventListener('click', () => joinRoom(room.roomId));

                roomsList.appendChild(roomElement);
            });
        } catch (error) {
            console.error('Error loading rooms:', error);
            roomsList.innerHTML = '<p class="error">Failed to load rooms</p>';
        }
    }

    function joinRoom(roomId) {
        const playerName = document.getElementById('joinPlayerName').value.trim();

        if (!playerName) {
            alert('Please enter your name');
            return;
        }

        sessionStorage.setItem('playerName', playerName);
        window.location.href = `/Game?room=${roomId}`;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
})();
